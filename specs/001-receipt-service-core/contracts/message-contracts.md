# Message Contracts: Receipt Service

**Feature**: Receipt Service Core
**Date**: 2025-12-05
**Messaging**: RabbitMQ via MassTransit

## Overview

The Receipt Service publishes events to RabbitMQ for asynchronous processing by downstream services (PDF Service, Upload Service, etc.). All events follow the Maliev routing key pattern: `maliev.{service}.{version}.{entity}.{action}`

---

## Published Events

### 1. PdfGenerationRequestedEvent

**Purpose**: Triggers PDF generation for a newly created receipt

**Routing Key**: `maliev.receipt.v1.pdf.requested`

**Exchange**: `maliev.receipt` (topic exchange)

**Publisher**: Receipt Service

**Consumer**: PDF Service

**Trigger**: Immediately after receipt creation (FR-011)

**Performance**: Published within 1 second of receipt creation (SC-004)

**Message Schema**:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": [
    "receiptId",
    "receiptNumber",
    "correlationId",
    "timestamp",
    "customerDetails",
    "financialDetails",
    "lineItems",
    "taxFields",
    "templateId"
  ],
  "properties": {
    "receiptId": {
      "type": "string",
      "format": "uuid",
      "description": "Receipt UUID for idempotency"
    },
    "receiptNumber": {
      "type": "string",
      "pattern": "^[A-Z]+-\\d{4}-\\d{6}$",
      "description": "Sequential receipt number",
      "example": "MALIEV-2025-000042"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid",
      "description": "Distributed tracing correlation ID"
    },
    "timestamp": {
      "type": "string",
      "format": "date-time",
      "description": "Event timestamp (UTC)"
    },
    "customerDetails": {
      "type": "object",
      "required": ["name"],
      "properties": {
        "name": {
          "type": "string",
          "description": "Customer name"
        },
        "taxId": {
          "type": "string",
          "nullable": true,
          "description": "Customer tax ID"
        },
        "address": {
          "type": "string",
          "nullable": true,
          "description": "Customer address"
        }
      }
    },
    "financialDetails": {
      "type": "object",
      "required": ["issueDate", "subtotal", "taxAmount", "totalAmount", "currency"],
      "properties": {
        "issueDate": {
          "type": "string",
          "format": "date-time",
          "description": "Receipt issue date"
        },
        "subtotal": {
          "type": "number",
          "format": "decimal",
          "minimum": 0
        },
        "taxAmount": {
          "type": "number",
          "format": "decimal",
          "minimum": 0
        },
        "withholdingTaxAmount": {
          "type": "number",
          "format": "decimal",
          "nullable": true,
          "minimum": 0
        },
        "totalAmount": {
          "type": "number",
          "format": "decimal",
          "minimum": 0.01
        },
        "currency": {
          "type": "string",
          "minLength": 3,
          "maxLength": 3,
          "description": "ISO 4217 currency code"
        },
        "paymentMethod": {
          "type": "string",
          "nullable": true,
          "description": "Payment method"
        }
      }
    },
    "lineItems": {
      "type": "array",
      "minItems": 1,
      "items": {
        "type": "object",
        "required": ["lineNumber", "description", "quantity", "unitPrice", "taxRate", "lineTotal"],
        "properties": {
          "lineNumber": {
            "type": "integer",
            "minimum": 1
          },
          "description": {
            "type": "string"
          },
          "quantity": {
            "type": "number",
            "format": "decimal",
            "minimum": 0.0001
          },
          "unitPrice": {
            "type": "number",
            "format": "decimal",
            "minimum": 0
          },
          "taxRate": {
            "type": "number",
            "format": "decimal",
            "minimum": 0
          },
          "lineTotal": {
            "type": "number",
            "format": "decimal",
            "minimum": 0
          }
        }
      }
    },
    "taxFields": {
      "type": "object",
      "description": "Government-required tax compliance fields (FR-012)",
      "additionalProperties": true,
      "properties": {
        "taxId": {
          "type": "string",
          "description": "Seller tax ID (required for Thailand)"
        },
        "vatRate": {
          "type": "number",
          "format": "decimal",
          "description": "VAT rate percentage"
        },
        "withholdingTaxType": {
          "type": "string",
          "nullable": true,
          "enum": ["None", "Type1", "Type3", "Type53"],
          "description": "Withholding tax classification (Thailand)"
        }
      }
    },
    "templateId": {
      "type": "string",
      "description": "PDF template identifier",
      "example": "receipt-v1"
    }
  }
}
```

**Example Payload**:

```json
{
  "receiptId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "receiptNumber": "MALIEV-2025-000042",
  "correlationId": "a3b5c7d9-1234-5678-90ab-cdef12345678",
  "timestamp": "2025-12-05T14:30:00Z",
  "customerDetails": {
    "name": "Acme Corp",
    "taxId": "1234567890123",
    "address": "123 Business Street, Bangkok 10110"
  },
  "financialDetails": {
    "issueDate": "2025-12-05T14:30:00Z",
    "subtotal": 1000.00,
    "taxAmount": 70.00,
    "withholdingTaxAmount": null,
    "totalAmount": 1070.00,
    "currency": "THB",
    "paymentMethod": "Bank Transfer"
  },
  "lineItems": [
    {
      "lineNumber": 1,
      "description": "Professional Services - December 2025",
      "quantity": 10.0,
      "unitPrice": 100.00,
      "taxRate": 7.00,
      "lineTotal": 1070.00
    }
  ],
  "taxFields": {
    "taxId": "0105536000000",
    "vatRate": 7.00,
    "withholdingTaxType": "None"
  },
  "templateId": "receipt-v1"
}
```

**Idempotency**: PDF Service should deduplicate using `receiptId` (receipts never change after creation)

**Retry Policy**: MassTransit default retry (3 attempts with exponential backoff)

**Dead Letter Queue**: Messages that fail after retries → `maliev.receipt.dlq`

---

### 2. ReceiptCreatedEvent (Internal Domain Event)

**Purpose**: Internal event for audit trail, analytics, and potential future consumers

**Routing Key**: `maliev.receipt.v1.receipt.created`

**Exchange**: `maliev.receipt` (topic exchange)

**Publisher**: Receipt Service

**Consumer**: Internal (audit processor, analytics aggregator)

**Trigger**: After receipt creation and PDF event publishing

**Message Schema**:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": [
    "receiptId",
    "receiptNumber",
    "invoiceId",
    "totalAmount",
    "currency",
    "status",
    "createdBy",
    "correlationId",
    "timestamp"
  ],
  "properties": {
    "receiptId": {
      "type": "string",
      "format": "uuid"
    },
    "receiptNumber": {
      "type": "string"
    },
    "invoiceId": {
      "type": "string",
      "format": "uuid"
    },
    "totalAmount": {
      "type": "number",
      "format": "decimal"
    },
    "currency": {
      "type": "string"
    },
    "status": {
      "type": "string",
      "enum": ["Active", "PendingPdf"]
    },
    "createdBy": {
      "type": "string"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid"
    },
    "timestamp": {
      "type": "string",
      "format": "date-time"
    }
  }
}
```

---

### 3. ReceiptVoidedEvent (Internal Domain Event)

**Purpose**: Internal event for audit trail and balance restoration notification

**Routing Key**: `maliev.receipt.v1.receipt.voided`

**Exchange**: `maliev.receipt` (topic exchange)

**Publisher**: Receipt Service

**Consumer**: Internal (audit processor, analytics aggregator)

**Trigger**: After receipt void operation

**Message Schema**:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": [
    "receiptId",
    "receiptNumber",
    "invoiceId",
    "voidReason",
    "voidedBy",
    "correlationId",
    "timestamp"
  ],
  "properties": {
    "receiptId": {
      "type": "string",
      "format": "uuid"
    },
    "receiptNumber": {
      "type": "string"
    },
    "invoiceId": {
      "type": "string",
      "format": "uuid"
    },
    "voidReason": {
      "type": "string"
    },
    "voidedBy": {
      "type": "string"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid"
    },
    "timestamp": {
      "type": "string",
      "format": "date-time"
    }
  }
}
```

---

## Consumed Events (Future)

### 1. PdfGeneratedEvent

**Purpose**: Notifies Receipt Service that PDF generation is complete

**Routing Key**: `maliev.pdf.v1.pdf.generated`

**Exchange**: `maliev.pdf` (topic exchange)

**Publisher**: PDF Service

**Consumer**: Receipt Service

**Trigger**: After PDF is uploaded to Upload Service

**Message Schema**:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": [
    "receiptId",
    "pdfReferenceId",
    "uploadServiceUrl",
    "correlationId",
    "timestamp"
  ],
  "properties": {
    "receiptId": {
      "type": "string",
      "format": "uuid",
      "description": "Original receipt ID for correlation"
    },
    "pdfReferenceId": {
      "type": "string",
      "format": "uuid",
      "description": "PDF file reference in Upload Service"
    },
    "uploadServiceUrl": {
      "type": "string",
      "format": "uri",
      "description": "Direct URL to download PDF"
    },
    "correlationId": {
      "type": "string",
      "format": "uuid"
    },
    "timestamp": {
      "type": "string",
      "format": "date-time"
    }
  }
}
```

**Handler Behavior**:
1. Find receipt by `receiptId`
2. Update `PdfReferenceId` to `pdfReferenceId`
3. Change `Status` from `PendingPdf` to `Active`
4. Create audit event (`EventType` = `PdfGenerated`)

---

## MassTransit Configuration

**Program.cs Setup**:

```csharp
builder.AddMassTransitWithRabbitMq();  // From ServiceDefaults

// Custom consumer registration (for PdfGeneratedEvent)
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PdfGeneratedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("rabbitmq"));

        cfg.ReceiveEndpoint("receipt-pdf-generated", e =>
        {
            e.ConfigureConsumer<PdfGeneratedEventConsumer>(context);
            e.Bind("maliev.pdf", s =>
            {
                s.RoutingKey = "maliev.pdf.v1.pdf.generated";
                s.ExchangeType = "topic";
            });
        });

        cfg.ConfigureEndpoints(context);
    });
});
```

**Publisher Example**:

```csharp
public class ReceiptService : IReceiptService
{
    private readonly IPublishEndpoint _publishEndpoint;

    public async Task<ReceiptResponse> CreateReceiptAsync(CreateReceiptRequest request, string staffId, Guid correlationId)
    {
        // ... create receipt in database ...

        // Publish PDF generation event
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
            // ... populate other fields ...
        };

        await _publishEndpoint.Publish(pdfEvent);  // Fire-and-forget

        return receipt.ToResponse();
    }
}
```

---

## Error Handling

**Message Failures**:
- **Retry Policy**: 3 attempts with exponential backoff (1s, 2s, 4s delays)
- **Dead Letter Queue**: Failed messages after retries → `maliev.receipt.dlq`
- **Monitoring**: Track DLQ depth via metrics, alert if > 10 messages

**Idempotency**:
- PDF Service: Use `receiptId` to deduplicate (receipts are immutable)
- Receipt Service: Use `receiptId` + event type to deduplicate audit events

---

## Testing Contracts

**Unit Tests** (Message Contract Validation):

```csharp
[Fact]
public void PdfGenerationRequestedEvent_ShouldSerializeCorrectly()
{
    var event = new PdfGenerationRequestedEvent
    {
        ReceiptId = Guid.NewGuid(),
        ReceiptNumber = "MALIEV-2025-000001",
        // ... populate fields ...
    };

    var json = JsonSerializer.Serialize(event);
    var deserialized = JsonSerializer.Deserialize<PdfGenerationRequestedEvent>(json);

    Assert.NotNull(deserialized);
    Assert.Equal(event.ReceiptId, deserialized.ReceiptId);
    Assert.Equal(event.ReceiptNumber, deserialized.ReceiptNumber);
}
```

**Integration Tests** (with Testcontainers.RabbitMQ):

```csharp
[Fact]
public async Task CreateReceipt_ShouldPublishPdfGenerationEvent()
{
    // Arrange
    var harness = _factory.Services.GetRequiredService<ITestHarness>();
    await harness.Start();

    // Act
    await _client.PostAsJsonAsync("/v1/receipts", new CreateReceiptRequest { ... });

    // Assert
    var published = await harness.Published.Any<PdfGenerationRequestedEvent>(x =>
        x.Context.Message.ReceiptNumber == "MALIEV-2025-000001");

    Assert.True(published);
}
```

---

**Phase 1 (Message Contracts) Status**: ✅ Complete - All message contracts defined with schemas, routing keys, and error handling strategies.
