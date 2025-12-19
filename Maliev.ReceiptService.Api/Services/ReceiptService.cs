using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Maliev.ReceiptService.Data.Data;
using Maliev.ReceiptService.Api.Events;
using Maliev.ReceiptService.Api.Exceptions;
using Maliev.ReceiptService.Api.Extensions;
using Maliev.ReceiptService.Api.Models.Dtos;
using Maliev.ReceiptService.Data.Models.Entities;
using Maliev.ReceiptService.Data.Models.Enums;
using Maliev.ReceiptService.Api.Models.Requests;
using Maliev.ReceiptService.Api.Models.Responses;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Maliev.ReceiptService.Api.Services;

/// <summary>
/// Receipt service implementation per quickstart.md Step 4.1
/// Handles receipt creation with tax validation, balance tracking, numbering, and PDF event publishing
/// </summary>
public class ReceiptService : IReceiptService
{
    private readonly ReceiptDbContext _context;
    private readonly IInvoiceServiceClient _invoiceClient;
    private readonly ITaxValidator _taxValidator;
    private readonly IReceiptNumberGenerator _numberGenerator;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ReceiptService> _logger;
    private readonly Counter<long> _receiptsCreatedCounter;
    private readonly Histogram<double> _creationDurationHistogram;

    public ReceiptService(
        ReceiptDbContext context,
        IInvoiceServiceClient invoiceClient,
        ITaxValidator taxValidator,
        IReceiptNumberGenerator numberGenerator,
        IPublishEndpoint publishEndpoint,
        ILogger<ReceiptService> logger,
        Counter<long> receiptsCreatedCounter,
        Histogram<double> creationDurationHistogram)
    {
        _context = context;
        _invoiceClient = invoiceClient;
        _taxValidator = taxValidator;
        _numberGenerator = numberGenerator;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _receiptsCreatedCounter = receiptsCreatedCounter;
        _creationDurationHistogram = creationDurationHistogram;
    }

    public async Task<ReceiptResponse> CreateReceiptAsync(
        CreateReceiptRequest request,
        string staffId,
        Guid correlationId)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Creating receipt for invoice {InvoiceId}, amount {Amount}, segmentId {SegmentId}, correlation {CorrelationId}",
            request.InvoiceId, request.Amount, request.InvoiceSegmentId, correlationId);

        // Step 1: Retrieve invoice from Invoice Service
        var invoice = await _invoiceClient.GetInvoiceAsync(request.InvoiceId);
        if (invoice == null)
        {
            throw new InvoiceNotFoundException(request.InvoiceId, $"Invoice {request.InvoiceId} not found");
        }

        // Step 1a: For split invoices, validate segment and extract segment-specific data
        InvoiceSegmentDto? segment = null;
        decimal validationSubtotal = invoice.Subtotal;
        decimal validationTaxAmount = invoice.TaxAmount;
        decimal validationTaxRate = invoice.VatRate;
        List<InvoiceLineItemDto> validationLineItems = invoice.LineItems;

        if (request.InvoiceSegmentId.HasValue)
        {
            segment = invoice.Segments.FirstOrDefault(s => s.SegmentId == request.InvoiceSegmentId.Value);
            if (segment == null)
            {
                throw new InvalidOperationException(
                    $"Segment {request.InvoiceSegmentId.Value} not found in invoice {request.InvoiceId}");
            }

            // Use segment-specific amounts and tax rate for validation
            validationTaxRate = segment.SegmentTaxRate;
            validationSubtotal = segment.SegmentAmount;
            validationTaxAmount = segment.SegmentAmount * (segment.SegmentTaxRate / 100);

            // Filter line items to only those belonging to this segment
            validationLineItems = invoice.LineItems
                .Where(li => segment.LineItemIds.Contains(li.Id))
                .ToList();

            _logger.LogInformation(
                "Processing split invoice segment {SegmentId} ({SegmentName}): Amount={SegmentAmount}, TaxRate={SegmentTaxRate}%",
                segment.SegmentId, segment.SegmentName, segment.SegmentAmount, segment.SegmentTaxRate);
        }

        // Step 2: Validate tax compliance
        var taxValidation = _taxValidator.ValidateReceipt(invoice, request);
        if (!taxValidation.IsSuccess)
        {
            throw new TaxValidationException(taxValidation.Errors, string.Join(", ", taxValidation.Errors));
        }

        // Step 3: Get or create balance tracker (segment-aware)
        var trackerTotalAmount = segment?.SegmentTotal ?? invoice.TotalAmount;
        var balanceTracker = await GetOrCreateBalanceTrackerAsync(
            request.InvoiceId,
            trackerTotalAmount,
            request.InvoiceSegmentId);

        // Step 4: Validate receipt amount doesn't exceed remaining balance
        if (request.Amount > balanceTracker.RemainingBalance)
        {
            var segmentInfo = request.InvoiceSegmentId.HasValue ? $" (segment {request.InvoiceSegmentId.Value})" : "";
            throw new OverReceiptingException(
                request.InvoiceId,
                request.Amount,
                balanceTracker.RemainingBalance,
                $"Receipt amount {request.Amount:F2} exceeds remaining balance {balanceTracker.RemainingBalance:F2}{segmentInfo}");
        }

        // Step 5: Generate sequential receipt number
        var receiptNumber = await _numberGenerator.GenerateNextReceiptNumberAsync("MALIEV", DateTime.UtcNow.Year);

        // Step 6: Create receipt entity (using segment-specific values if applicable)
        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            ReceiptNumber = receiptNumber,
            InvoiceId = request.InvoiceId,
            InvoiceSegmentId = request.InvoiceSegmentId,
            IssueDate = DateTime.UtcNow,
            CustomerName = invoice.CustomerName,
            CustomerTaxId = invoice.CustomerTaxId,
            CustomerAddress = invoice.CustomerAddress,
            Subtotal = validationSubtotal,
            TaxAmount = validationTaxAmount,
            WithholdingTaxAmount = invoice.WithholdingTaxAmount,
            TotalAmount = request.Amount,
            Currency = invoice.Currency,
            PaymentMethod = request.PaymentMethod,
            Status = ReceiptStatus.PendingPdf,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = staffId,
            CorrelationId = correlationId,
            LineItems = validationLineItems.Select((li, index) => new ReceiptLineItem
            {
                Id = Guid.NewGuid(),
                ReceiptId = Guid.NewGuid(), // Will be set by EF Core
                InvoiceLineItemId = li.Id,
                LineNumber = index + 1,
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                TaxRate = li.TaxRate,
                LineTotal = li.LineTotal
            }).ToList()
        };

        // Step 7: Update balance tracker
        balanceTracker.TotalReceiptedAmount += request.Amount;
        balanceTracker.RemainingBalance -= request.Amount;

        // Step 8: Create audit event
        var auditEvent = new ReceiptAuditEvent
        {
            Id = Guid.NewGuid(),
            ReceiptId = receipt.Id,
            EventType = AuditEventType.Created,
            Timestamp = DateTime.UtcNow,
            StaffMemberId = staffId,
            Reason = "Receipt created",
            CorrelationId = correlationId
        };

        // Step 9: Save to database with optimistic concurrency control
        _context.Receipts.Add(receipt);
        _context.ReceiptAuditEvents.Add(auditEvent);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DuplicateReceiptException(
                request.InvoiceId,
                "Invoice balance was modified by another operation. Please retry.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
        {
            // Unique constraint violation on InvoiceBalanceTracker - concurrent insert detected
            throw new DuplicateReceiptException(
                request.InvoiceId,
                "Another receipt is being created for this invoice concurrently. Please retry.");
        }

        _logger.LogInformation(
            "Receipt created: {ReceiptNumber} for invoice {InvoiceId}",
            receiptNumber, request.InvoiceId);

        // Step 10: Publish PDF generation event
        var pdfEvent = new PdfGenerationRequestedEvent
        {
            ReceiptId = receipt.Id,
            ReceiptNumber = receipt.ReceiptNumber,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            CustomerDetails = new CustomerDetails
            {
                Name = receipt.CustomerName,
                TaxId = receipt.CustomerTaxId,
                Address = receipt.CustomerAddress
            },
            FinancialDetails = new FinancialDetails
            {
                IssueDate = receipt.IssueDate,
                Subtotal = receipt.Subtotal,
                TaxAmount = receipt.TaxAmount,
                WithholdingTaxAmount = receipt.WithholdingTaxAmount,
                TotalAmount = receipt.TotalAmount,
                Currency = receipt.Currency,
                PaymentMethod = receipt.PaymentMethod
            },
            LineItems = receipt.LineItems.Select(li => new LineItemDto
            {
                LineNumber = li.LineNumber,
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                TaxRate = li.TaxRate,
                LineTotal = li.LineTotal
            }).ToList(),
            TaxFields = new TaxFields
            {
                TaxId = invoice.CustomerTaxId ?? string.Empty,
                VatRate = invoice.VatRate,
                WithholdingTaxType = invoice.WithholdingTaxType
            },
            TemplateId = "receipt-v1"
        };

        await _publishEndpoint.Publish(pdfEvent);

        _logger.LogInformation(
            "PDF generation event published for receipt {ReceiptNumber}",
            receiptNumber);

        // Record metrics (per research.md Decision 9, FR-031)
        stopwatch.Stop();
        _receiptsCreatedCounter.Add(1, new KeyValuePair<string, object?>("service.name", "ReceiptService"));
        _creationDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("service.name", "ReceiptService"));

        _logger.LogInformation(
            "Receipt created successfully: {ReceiptNumber}, duration: {Duration}ms",
            receiptNumber, stopwatch.ElapsedMilliseconds);

        return receipt.ToResponse();
    }

    public async Task<ReceiptResponse?> GetReceiptByIdAsync(Guid id)
    {
        var receipt = await _context.Receipts
            .Include(r => r.LineItems)
            .FirstOrDefaultAsync(r => r.Id == id);

        return receipt?.ToResponse();
    }

    public async Task<PagedResponse<ReceiptResponse>> QueryReceiptsAsync(
        Guid? invoiceId = null,
        ReceiptStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? segmentId = null,
        int page = 1,
        int pageSize = 20,
        string sortBy = "issueDate",
        string sortOrder = "desc")
    {
        _logger.LogInformation(
            "Querying receipts: invoiceId={InvoiceId}, status={Status}, fromDate={FromDate}, toDate={ToDate}, segmentId={SegmentId}, page={Page}, pageSize={PageSize}",
            invoiceId, status, fromDate, toDate, segmentId, page, pageSize);

        // Build query with filters
        var query = _context.Receipts
            .Include(r => r.LineItems)
            .AsQueryable();

        if (invoiceId.HasValue)
        {
            query = query.Where(r => r.InvoiceId == invoiceId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.IssueDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.IssueDate <= toDate.Value);
        }

        // US4: Filter by segment ID for split invoice receipts (T082)
        if (segmentId.HasValue)
        {
            query = query.Where(r => r.InvoiceSegmentId == segmentId.Value);
        }

        // Apply sorting
        query = sortBy.ToLower() switch
        {
            "receiptnumber" => sortOrder.ToLower() == "asc"
                ? query.OrderBy(r => r.ReceiptNumber)
                : query.OrderByDescending(r => r.ReceiptNumber),
            "totalamount" => sortOrder.ToLower() == "asc"
                ? query.OrderBy(r => r.TotalAmount)
                : query.OrderByDescending(r => r.TotalAmount),
            _ => sortOrder.ToLower() == "asc"
                ? query.OrderBy(r => r.IssueDate)
                : query.OrderByDescending(r => r.IssueDate)
        };

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination
        var receipts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        _logger.LogInformation(
            "Query returned {Count} receipts (page {Page} of {TotalPages})",
            receipts.Count, page, totalPages);

        return new PagedResponse<ReceiptResponse>
        {
            Data = receipts.Select(r => r.ToResponse()).ToList(),
            Pagination = new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            }
        };
    }

    /// <summary>
    /// Voids a receipt with balance restoration and audit trail creation
    /// Task: T070 [US3] Implement ReceiptService.VoidReceiptAsync()
    /// </summary>
    public async Task<ReceiptResponse> VoidReceiptAsync(
        Guid receiptId,
        string reason,
        string staffId,
        Guid correlationId)
    {
        _logger.LogInformation(
            "Voiding receipt {ReceiptId}, reason: {Reason}, staff: {StaffId}, correlation: {CorrelationId}",
            receiptId, reason, staffId, correlationId);

        // Step 1: Retrieve receipt with line items
        var receipt = await _context.Receipts
            .Include(r => r.LineItems)
            .FirstOrDefaultAsync(r => r.Id == receiptId);

        if (receipt == null)
        {
            throw new ReceiptNotFoundException(receiptId, $"Receipt {receiptId} not found");
        }

        // Step 2: Capture previous state for audit
        var previousState = System.Text.Json.JsonSerializer.Serialize(new
        {
            receipt.Id,
            receipt.ReceiptNumber,
            receipt.Status,
            receipt.TotalAmount,
            receipt.InvoiceId
        });

        // Step 3: Void the receipt (will throw if already voided)
        receipt.Void(staffId, reason);

        // Step 4: Restore balance tracker (segment-aware for US4)
        // Convert null to Guid.Empty for whole-invoice tracking (EF Core composite key requirement)
        var segmentIdValue = receipt.InvoiceSegmentId ?? Guid.Empty;
        var balanceTracker = await _context.InvoiceBalanceTrackers
            .FirstOrDefaultAsync(t => t.InvoiceId == receipt.InvoiceId && t.SegmentId == segmentIdValue);

        if (balanceTracker != null)
        {
            balanceTracker.TotalReceiptedAmount -= receipt.TotalAmount;
            balanceTracker.RemainingBalance += receipt.TotalAmount;
            balanceTracker.LastUpdatedAt = DateTime.UtcNow;

            if (receipt.InvoiceSegmentId.HasValue)
            {
                _logger.LogInformation(
                    "Restored balance for segment {SegmentId}: +{Amount}, new balance: {Balance}",
                    receipt.InvoiceSegmentId.Value, receipt.TotalAmount, balanceTracker.RemainingBalance);
            }
        }

        // Step 5: Create audit event for void operation
        var auditEvent = new ReceiptAuditEvent
        {
            Id = Guid.NewGuid(),
            ReceiptId = receipt.Id,
            EventType = AuditEventType.Voided,
            Timestamp = DateTime.UtcNow,
            StaffMemberId = staffId,
            Reason = reason,
            PreviousState = previousState,
            NewState = System.Text.Json.JsonSerializer.Serialize(new
            {
                receipt.Id,
                receipt.ReceiptNumber,
                receipt.Status,
                receipt.TotalAmount,
                receipt.InvoiceId,
                receipt.VoidedAt,
                receipt.VoidedBy,
                receipt.VoidReason
            }),
            CorrelationId = correlationId,
            RetainUntil = DateTime.UtcNow.AddYears(7)
        };

        _context.ReceiptAuditEvents.Add(auditEvent);

        // Step 6: Save changes
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Receipt {ReceiptNumber} voided successfully by {StaffId}, reason: {Reason}",
            receipt.ReceiptNumber, staffId, reason);

        return receipt.ToResponse();
    }

    /// <summary>
    /// Gets audit history for a receipt
    /// Task: T072 [P] [US3] Implement GET /v1/receipts/{id}/audit-history
    /// </summary>
    public async Task<List<AuditEvent>> GetAuditHistoryAsync(Guid receiptId)
    {
        _logger.LogInformation("Retrieving audit history for receipt {ReceiptId}", receiptId);

        // Verify receipt exists
        var receiptExists = await _context.Receipts.AnyAsync(r => r.Id == receiptId);
        if (!receiptExists)
        {
            throw new ReceiptNotFoundException(receiptId, $"Receipt {receiptId} not found");
        }

        // Get audit events in chronological order
        var auditEvents = await _context.ReceiptAuditEvents
            .Where(e => e.ReceiptId == receiptId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        _logger.LogInformation(
            "Retrieved {Count} audit events for receipt {ReceiptId}",
            auditEvents.Count, receiptId);

        return auditEvents.Select(e => new AuditEvent
        {
            Id = e.Id,
            ReceiptId = e.ReceiptId,
            EventType = e.EventType.ToString(),
            Timestamp = e.Timestamp,
            StaffMemberId = e.StaffMemberId,
            Reason = e.Reason,
            PreviousState = e.PreviousState,
            NewState = e.NewState,
            CorrelationId = e.CorrelationId,
            RetainUntil = e.RetainUntil
        }).ToList();
    }

    /// <summary>
    /// Gets or creates invoice balance tracker per research.md Decision 4
    /// Extended for US4 to support segment-level tracking (T081)
    /// </summary>
    private async Task<InvoiceBalanceTracker> GetOrCreateBalanceTrackerAsync(
        Guid invoiceId,
        decimal totalInvoiceAmount,
        Guid? segmentId = null)
    {
        // Convert null to Guid.Empty for whole-invoice tracking (EF Core composite key requirement)
        var segmentIdValue = segmentId ?? Guid.Empty;

        var tracker = await _context.InvoiceBalanceTrackers
            .FirstOrDefaultAsync(t => t.InvoiceId == invoiceId && t.SegmentId == segmentIdValue);

        if (tracker == null)
        {
            tracker = new InvoiceBalanceTracker
            {
                InvoiceId = invoiceId,
                SegmentId = segmentIdValue,
                TotalInvoiceAmount = totalInvoiceAmount,
                TotalReceiptedAmount = 0,
                RemainingBalance = totalInvoiceAmount,
                LastUpdatedAt = DateTime.UtcNow
            };

            _context.InvoiceBalanceTrackers.Add(tracker);

            if (segmentId.HasValue)
            {
                _logger.LogInformation(
                    "Created balance tracker for invoice {InvoiceId}, segment {SegmentId}, amount {Amount}",
                    invoiceId, segmentId.Value, totalInvoiceAmount);
            }
        }

        return tracker;
    }
}
