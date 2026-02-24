# External Service DTOs: Receipt Service

**Feature**: Receipt Service Core
**Date**: 2025-12-06
**Purpose**: Document data transfer objects received from external services

---

## Overview

This document defines DTOs for data structures received from external microservices that the Receipt Service integrates with. These schemas ensure developers understand the expected contract when implementing service clients.

---

## 1. InvoiceDto (from Invoice Service)

**Source**: Invoice Service API (`GET /v1/invoices/{id}`)

**Purpose**: Complete invoice data retrieved before receipt creation

**Usage**: Retrieved by `InvoiceServiceClient` in task T027, consumed by `ReceiptService.CreateReceiptAsync()` in task T047

### Schema

```csharp
public class InvoiceDto
{
    // Invoice Identity
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } // "Draft", "Issued", "Paid", "Void", etc.

    // Customer Details
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; }
    public string CustomerTaxId { get; set; }
    public string CustomerAddress { get; set; }

    // Financial Amounts
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal? WithholdingTaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } // ISO 4217 code (e.g., "THB")

    // Tax Compliance Fields (FR-009, FR-012)
    public string SellerTaxId { get; set; }
    public decimal VatRate { get; set; }
    public string WithholdingTaxType { get; set; } // "None", "Type1", "Type3", "Type53"

    // Line Items
    public List<InvoiceLineItemDto> LineItems { get; set; }

    // Split Invoice Support (FR-018, FR-019)
    public List<InvoiceSegmentDto> Segments { get; set; }

    // Payment Tracking
    public decimal TotalPaidAmount { get; set; }
    public decimal RemainingBalance { get; set; }
    public string PaymentStatus { get; set; } // "Unpaid", "PartiallyPaid", "FullyPaid"

    // Audit Fields
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
}
```

### InvoiceLineItemDto

```csharp
public class InvoiceLineItemDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public string Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }
}
```

### InvoiceSegmentDto (for Split Invoices - US4)

```csharp
public class InvoiceSegmentDto
{
    public Guid SegmentId { get; set; }
    public string SegmentName { get; set; }
    public decimal SegmentAmount { get; set; }
    public decimal SegmentTaxRate { get; set; }
    public decimal SegmentTotal { get; set; }
    public List<Guid> LineItemIds { get; set; } // References to LineItems in this segment
}
```

---

## 2. Sample Invoice JSON Response

**Example**: Full payment invoice from Invoice Service

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "invoiceNumber": "INV-2025-000123",
  "issueDate": "2025-11-15T00:00:00Z",
  "dueDate": "2025-12-15T00:00:00Z",
  "status": "Paid",
  "customerId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "customerName": "Acme Corporation",
  "customerTaxId": "1234567890123",
  "customerAddress": "123 Business Street, Bangkok 10110, Thailand",
  "subtotal": 1000.00,
  "taxAmount": 70.00,
  "withholdingTaxAmount": null,
  "totalAmount": 1070.00,
  "currency": "THB",
  "sellerTaxId": "0105536000000",
  "vatRate": 7.00,
  "withholdingTaxType": "None",
  "lineItems": [
    {
      "id": "a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d",
      "lineNumber": 1,
      "description": "Professional Services - December 2025",
      "quantity": 10.0,
      "unitPrice": 100.00,
      "taxRate": 7.00,
      "lineTotal": 1070.00
    }
  ],
  "segments": [],
  "totalPaidAmount": 1070.00,
  "remainingBalance": 0.00,
  "paymentStatus": "FullyPaid",
  "createdAt": "2025-11-15T10:30:00Z",
  "createdBy": "staff-56789"
}
```

---

## 3. Validation Rules

**Receipt Service MUST validate** (per FR-002, FR-008, FR-009):

| Field | Validation Rule |
|-------|----------------|
| `Status` | Must be "Paid" or "PartiallyPaid" to be eligible for receipting |
| `PaymentStatus` | Must not be "Unpaid" |
| `RemainingBalance` | Must be > 0 for partial payments, = 0 for full payments |
| `TotalAmount` | Must equal `Subtotal + TaxAmount - WithholdingTaxAmount` |
| `SellerTaxId` | Required for tax compliance (Thailand) |
| `VatRate` | Must be valid (typically 7.00 for Thailand) |
| `LineItems` | Must have at least 1 line item |

---

## 4. Error Handling

**Invoice Service Response Codes**:

| Status Code | Meaning | Receipt Service Action |
|-------------|---------|------------------------|
| 200 OK | Invoice found and returned | Proceed with validation |
| 404 Not Found | Invoice ID does not exist | Throw `InvoiceNotFoundException` |
| 503 Service Unavailable | Invoice Service is down | Retry per FR-025 (5s timeout, 2 retries, exp backoff) |
| 500 Internal Server Error | Invoice Service error | Retry per FR-025 |

---

## 5. Usage in Receipt Service

**Implementation Task**: T025-T028

**Example Usage**:

```csharp
public class InvoiceServiceClient : IInvoiceServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InvoiceServiceClient> _logger;

    public async Task<InvoiceDto> GetInvoiceAsync(Guid invoiceId, Guid correlationId)
    {
        _logger.LogInformation("Retrieving invoice {InvoiceId} [CorrelationId: {CorrelationId}]",
            invoiceId, correlationId);

        var response = await _httpClient.GetAsync($"/v1/invoices/{invoiceId}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvoiceNotFoundException(invoiceId);
        }

        response.EnsureSuccessStatusCode();

        var invoice = await response.Content.ReadFromJsonAsync<InvoiceDto>();

        _logger.LogInformation("Retrieved invoice {InvoiceNumber} for customer {CustomerName}",
            invoice.InvoiceNumber, invoice.CustomerName);

        return invoice;
    }
}
```

---

## 6. Future DTOs (Placeholder)

### PdfGeneratedEvent (from PDF Service)

**Purpose**: Callback when PDF generation completes (Phase 8: T105-T108)

**Schema**: See `message-contracts.md` for complete definition

---

**Document Status**: ✅ Complete
**Referenced By**: tasks.md (T025, T027, T047, T079, T080)
**Dependencies**: Invoice Service API contract (external)
