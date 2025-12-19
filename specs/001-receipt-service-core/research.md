# Phase 0: Research & Architectural Decisions

**Feature**: Receipt Service Core
**Date**: 2025-12-05
**Status**: Complete

## Overview

This document consolidates research findings and architectural decisions for implementing the Receipt Service. All technical unknowns have been resolved using the Maliev Service Implementation Guidelines and industry best practices for financial microservices.

---

## 1. Receipt Numbering Strategy

### Decision
Sequential numbering per legal entity with prefix format: `ENTITY-YYYY-NNNNNN` (e.g., `MALIEV-2025-000001`)

### Rationale
- **Tax Compliance**: Most tax authorities require sequential receipt numbers for audit trails
- **Uniqueness**: Legal entity prefix prevents collisions in multi-tenant scenarios
- **Traceability**: Year component aids in archival and lookup
- **Gap Prevention**: Sequential numbering detects missing receipts (fraud indicator)

### Implementation Approach
```csharp
// ReceiptNumberGenerator service
// 1. Query MAX receipt number for (entity + year) from database
// 2. Increment atomically using PostgreSQL sequence or row-level locking
// 3. Format as ENTITY-YYYY-NNNNNN with zero-padding
// 4. Handle year rollover (reset sequence on Jan 1)
```

### Alternatives Considered
- **UUID**: Rejected - not tax compliant, no sequential audit trail
- **Global sequential**: Rejected - doesn't scale across entities, single point of contention
- **Timestamp-based**: Rejected - not guaranteed sequential, clock skew issues

---

## 2. Receipt Immutability Pattern

### Decision
Receipts are immutable after creation. Corrections require void + recreate workflow.

### Rationale
- **Audit Integrity**: Prevents tampering with financial records
- **Tax Compliance**: Matches physical receipt behavior (can't erase ink)
- **Simplicity**: No complex versioning or change tracking
- **Legal**: Creates clear paper trail for audits

### Implementation Approach
```csharp
// Entity-level enforcement
public class Receipt
{
    // No setters after initial creation
    public string ReceiptNumber { get; init; }
    public decimal TotalAmount { get; init; }

    // Only status can change (for voiding)
    public ReceiptStatus Status { get; private set; }

    public void Void(string reason, string staffMemberId)
    {
        if (Status == ReceiptStatus.Void)
            throw new InvalidOperationException("Receipt already void");

        Status = ReceiptStatus.Void;
        // Trigger audit event
    }
}
```

### Alternatives Considered
- **Editable with versioning**: Rejected - complex, harder to audit, doesn't match physical receipt model
- **Time-limited editing (24h)**: Rejected - arbitrary window, doesn't prevent fraud after window closes
- **Admin override**: Rejected - defeats purpose of immutability, creates compliance risk

---

## 3. Invoice Service Integration Pattern

### Decision
HTTP client with 5-second timeout, 2 retries, exponential backoff (1s, 2s delays)

### Rationale
- **Performance Target**: Aligns with SC-001 (5s total receipt creation time)
- **Resilience**: Handles transient network issues without excessive delay
- **User Experience**: Max 9s total wait (5s + 1s + 5s + 2s + 5s = 18s theoretical, but typically fails faster)
- **Circuit Breaking**: Prevents cascade failures

### Implementation Approach
```csharp
// Use Polly retry policy (included via ServiceDefaults)
builder.Services.AddHttpClient<IInvoiceServiceClient, InvoiceServiceClient>()
    .AddStandardResilienceHandler(options =>
    {
        options.Retry = new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential
        };
        options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    });
```

### Alternatives Considered
- **No retries (fail fast)**: Rejected - too aggressive, many transient issues are retryable
- **3+ retries**: Rejected - exceeds performance budget, poor UX
- **Circuit breaker only**: Rejected - doesn't handle single transient failures well

---

## 4. Balance Tracking & Concurrency Control

### Decision
Optimistic locking using EF Core's `ConcurrencyToken` on `InvoiceBalanceTracker` entity

### Rationale
- **Correctness**: Prevents race conditions when two staff create receipts for same invoice simultaneously
- **Performance**: Optimistic locking is faster than pessimistic (no lock holding)
- **Scalability**: Doesn't block other invoices' receipt creation
- **EF Core Native**: Built-in support via `[Timestamp]` or `RowVersion`

### Implementation Approach
```csharp
public class InvoiceBalanceTracker
{
    public Guid InvoiceId { get; set; }
    public decimal TotalInvoiceAmount { get; set; }
    public decimal TotalReceiptedAmount { get; set; }
    public decimal RemainingBalance { get; set; }

    [Timestamp]  // EF Core concurrency token
    public byte[] RowVersion { get; set; }
}

// Service layer
try
{
    tracker.TotalReceiptedAmount += request.Amount;
    tracker.RemainingBalance -= request.Amount;
    await _context.SaveChangesAsync();  // Will throw DbUpdateConcurrencyException if modified
}
catch (DbUpdateConcurrencyException)
{
    return BadRequest("Invoice balance was modified by another operation. Please retry.");
}
```

### Alternatives Considered
- **Pessimistic locking**: Rejected - requires holding database locks, hurts throughput
- **Distributed lock (Redis)**: Rejected - adds complexity, network dependency for every receipt
- **No locking**: Rejected - allows duplicate/over-receipting

---

## 5. Audit Trail Storage Strategy

### Decision
Separate `ReceiptAuditEvent` table with 7-year retention policy

### Rationale
- **Compliance**: FR-016a requires 7-year retention
- **Immutability**: Append-only table, never UPDATE/DELETE
- **Query Performance**: Doesn't bloat main `Receipts` table
- **Archival**: Can move old events to cold storage independently

### Implementation Approach
```csharp
public class ReceiptAuditEvent
{
    public Guid Id { get; set; }
    public Guid ReceiptId { get; set; }
    public AuditEventType EventType { get; set; }  // Created, Voided, etc.
    public DateTime Timestamp { get; set; }
    public string StaffMemberId { get; set; }
    public string Reason { get; set; }
    public string PreviousState { get; set; }  // JSON snapshot
    public string NewState { get; set; }  // JSON snapshot
    public string CorrelationId { get; set; }
    public DateTime RetainUntil { get; set; }  // Timestamp + 7 years
}

// Cleanup job (separate scheduled task)
// DELETE FROM ReceiptAuditEvents WHERE RetainUntil < NOW()
```

### Alternatives Considered
- **Event sourcing**: Rejected - overkill for this use case, requires rebuilding state from events
- **Same table as Receipt**: Rejected - pollutes receipt queries, harder to manage retention
- **NoSQL/document store**: Rejected - adds infrastructure complexity, SQL is sufficient

---

## 6. PDF Generation Event Pattern

### Decision
Fire-and-forget MassTransit publish to RabbitMQ after receipt creation

### Rationale
- **Async Workflow**: PDF generation is slow (1-5s), shouldn't block receipt creation
- **Retry Handling**: MassTransit handles retries if PDF Service is down
- **Idempotency**: PDF Service can deduplicate based on ReceiptId
- **Separation of Concerns**: Receipt Service doesn't know about PDF rendering

### Implementation Approach
```csharp
// After saving receipt
var pdfEvent = new PdfGenerationRequestedEvent
{
    ReceiptId = receipt.Id,
    ReceiptNumber = receipt.ReceiptNumber,
    CustomerDetails = receipt.CustomerDetails,
    LineItems = receipt.LineItems.Select(x => x.ToDto()).ToList(),
    TaxFields = receipt.GetTaxFields(),  // Government-required fields
    CorrelationId = correlationId,
    TemplateId = "receipt-v1"
};

await _publishEndpoint.Publish(pdfEvent);  // Fire and forget

// Mark receipt as "PendingPdf" status
// PDF Service will callback with Upload Service reference
```

### Routing Key
`maliev.receipt.v1.pdf.requested`

### Alternatives Considered
- **Synchronous call to PDF Service**: Rejected - blocks receipt creation, violates performance targets
- **Direct RabbitMQ client**: Rejected - MassTransit provides better retry, DLQ, monitoring
- **Saga pattern**: Rejected - overkill, no compensation logic needed (PDFs can be regenerated)

---

## 7. Tax Validation Strategy

### Decision
Pluggable `ITaxValidator` interface with configurable validation rules

### Rationale
- **Flexibility**: Tax rules vary by jurisdiction (Thailand, Singapore, etc.)
- **Testability**: Can mock validator in tests
- **Maintainability**: Tax rules change; isolate in one component
- **Compliance**: FR-009 requires government-required fields

### Implementation Approach
```csharp
public interface ITaxValidator
{
    ValidationResult ValidateReceipt(InvoiceDto invoice, CreateReceiptRequest request);
}

public class ThailandTaxValidator : ITaxValidator
{
    public ValidationResult ValidateReceipt(InvoiceDto invoice, CreateReceiptRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(invoice.TaxId))
            errors.Add("Tax ID (เลขประจำตัวผู้เสียภาษี) is required");

        if (invoice.WithholdingTaxRate.HasValue && invoice.WithholdingTaxRate < 0 || invoice.WithholdingTaxRate > 100)
            errors.Add("Withholding tax rate must be between 0-100%");

        if (invoice.VatRate != 7.0m)  // Thailand VAT is 7%
            errors.Add($"VAT rate must be 7% (got {invoice.VatRate}%)");

        return errors.Any()
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }
}

// Register in Program.cs
builder.Services.AddScoped<ITaxValidator, ThailandTaxValidator>();
```

### Alternatives Considered
- **Hardcoded validation**: Rejected - not flexible, hard to extend
- **FluentValidation**: Rejected - violates Principle XIV
- **External tax service**: Rejected - adds latency, most rules are static

---

## 8. Analytics & Caching Strategy

### Decision
Redis caching for aggregated analytics queries with 5-minute TTL

### Rationale
- **Performance**: SC-007 requires <2s for 100k receipts
- **Read-Heavy**: Analytics queries are frequent, data changes slowly
- **Freshness**: 5-minute staleness acceptable for business metrics
- **Cost**: Reduces PostgreSQL load

### Implementation Approach
```csharp
public class AnalyticsService
{
    private readonly IDistributedCache _cache;
    private readonly ReceiptDbContext _context;

    public async Task<PaymentCompletionRate> GetPaymentCompletionRate(DateTime month)
    {
        var cacheKey = $"analytics:completion-rate:{month:yyyy-MM}";
        var cached = await _cache.GetStringAsync(cacheKey);

        if (cached != null)
            return JsonSerializer.Deserialize<PaymentCompletionRate>(cached);

        // Query database
        var result = await _context.Receipts
            .Where(r => r.IssueDate >= month && r.IssueDate < month.AddMonths(1))
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var rate = CalculateCompletionRate(result);

        // Cache for 5 minutes
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(rate),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

        return rate;
    }
}
```

### Alternatives Considered
- **No caching**: Rejected - can't meet performance targets for large datasets
- **In-memory cache**: Rejected - doesn't scale horizontally, lost on pod restart
- **Materialized views**: Rejected - adds database complexity, harder to invalidate

---

## 9. Observability Implementation

### Decision
Structured JSON logs (via ILogger), OpenTelemetry metrics, distributed tracing with correlation IDs

### Rationale
- **Constitution Principle V**: Auditability & Observability required
- **FR-030/031/032**: Explicitly requires structured logs, metrics, tracing
- **ServiceDefaults**: Already configured OpenTelemetry
- **Debugging**: Correlation IDs trace requests across Invoice/PDF/Upload services

### Implementation Approach
```csharp
// Structured logging
_logger.LogInformation("Receipt created: {ReceiptId} for Invoice: {InvoiceId} by Staff: {StaffId} [CorrelationId: {CorrelationId}]",
    receipt.Id, request.InvoiceId, staffId, correlationId);

// Metrics (via ServiceDefaults + custom counters)
private static readonly Counter<long> ReceiptsCreatedCounter =
    Meter.CreateCounter<long>("receipts.created", "receipts", "Number of receipts created");

private static readonly Histogram<double> ReceiptCreationDuration =
    Meter.CreateHistogram<double>("receipts.creation_duration", "seconds", "Receipt creation duration");

// Tracing (automatic via OpenTelemetry, manual correlation ID propagation)
public class ReceiptService
{
    private readonly ILogger<ReceiptService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public async Task<ReceiptResponse> CreateReceiptAsync(CreateReceiptRequest request)
    {
        var correlationId = _httpContextAccessor.HttpContext.TraceIdentifier;

        using var activity = Activity.Current;
        activity?.SetTag("receipt.invoice_id", request.InvoiceId);
        activity?.SetTag("correlation_id", correlationId);

        // Pass correlation ID to Invoice Service, PDF Service
    }
}
```

### Alternatives Considered
- **Serilog**: Rejected - violates Maliev guidelines (use standard ILogger only)
- **Prometheus-net**: Rejected - violates guidelines (use OpenTelemetry via ServiceDefaults)
- **Custom logging**: Rejected - reinventing wheel, ServiceDefaults already configured

---

## 10. Error Handling Strategy

### Decision
Global `ExceptionHandlingMiddleware` with structured error responses + specific domain exceptions

### Rationale
- **Consistency**: All errors return same JSON structure
- **Security**: Hides stack traces in production
- **Client-Friendly**: Error codes + messages for UI display
- **Logging**: Captures all unhandled exceptions

### Implementation Approach
```csharp
public class ExceptionHandlingMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (DuplicateReceiptException ex)
        {
            await WriteErrorResponse(context, StatusCodes.Status409Conflict, "DUPLICATE_RECEIPT", ex.Message);
        }
        catch (InvoiceNotFoundException ex)
        {
            await WriteErrorResponse(context, StatusCodes.Status404NotFound, "INVOICE_NOT_FOUND", ex.Message);
        }
        catch (InvoiceServiceUnavailableException ex)
        {
            await WriteErrorResponse(context, StatusCodes.Status503ServiceUnavailable, "INVOICE_SERVICE_UNAVAILABLE", ex.Message);
        }
        catch (TaxValidationException ex)
        {
            await WriteErrorResponse(context, StatusCodes.Status400BadRequest, "TAX_VALIDATION_FAILED", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorResponse(context, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "An error occurred");
        }
    }
}

// Domain exceptions
public class DuplicateReceiptException : Exception
{
    public DuplicateReceiptException(Guid invoiceId)
        : base($"Invoice {invoiceId} is already fully receipted") { }
}
```

### Alternatives Considered
- **Try-catch in every controller**: Rejected - duplicative, inconsistent
- **Problem Details (RFC 7807)**: Considered - good pattern but middleware simpler for MVP
- **No global handler**: Rejected - leaks stack traces, inconsistent errors

---

## Summary of Key Decisions

| Area | Decision | Rationale |
|------|----------|-----------|
| **Receipt Numbering** | Sequential per entity with prefix (ENTITY-YYYY-NNNNNN) | Tax compliance, gap detection |
| **Immutability** | Immutable after creation, void+recreate for corrections | Audit integrity, legal compliance |
| **Invoice Integration** | HTTP with 5s timeout, 2 retries, exp backoff | Performance, resilience |
| **Concurrency** | Optimistic locking via EF Core Timestamp | Correctness without blocking |
| **Audit Trail** | Separate table, 7-year retention | Compliance, query performance |
| **PDF Generation** | Async MassTransit/RabbitMQ publish | Non-blocking, scalable |
| **Tax Validation** | Pluggable ITaxValidator interface | Flexibility, testability |
| **Analytics Caching** | Redis with 5-min TTL | Performance, reduced DB load |
| **Observability** | Structured logs + OpenTelemetry + correlation IDs | Debugging, compliance, monitoring |
| **Error Handling** | Global middleware + domain exceptions | Consistency, security, UX |

---

**Phase 0 Status**: ✅ Complete - All architectural decisions documented. Ready for Phase 1 (Data Model & Contracts).
