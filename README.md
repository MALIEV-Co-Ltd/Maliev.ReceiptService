# Maliev Receipt Service

Comprehensive receipt and payment document management service for the MALIEV platform, handling receipt generation, issuance, void operations, and compliance tracking with full IAM integration.

## Service Description

The Receipt Service manages all payment receipts across the MALIEV platform. It handles receipt creation for various payment types, tracks void operations for compliance, integrates with the PDF Service for document generation, and maintains complete audit trails for tax and legal requirements.

## Architecture Overview

### Project Structure
```
Maliev.ReceiptService/
├── Maliev.ReceiptService.Api/          # Presentation layer
│   ├── Controllers/                    # REST API endpoints
│   ├── Services/                       # Business logic
│   └── Models/                         # DTOs
├── Maliev.ReceiptService.Data/         # Data access layer
│   ├── Entities/                       # EF Core entities
│   └── Migrations/                     # Database migrations
└── Maliev.ReceiptService.Tests/        # Integration tests
```

## Technologies Used

- **.NET 10.0** - Runtime and framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - ORM with PostgreSQL provider
- **PostgreSQL 18** - Relational database
- **Redis** - Distributed caching
- **RabbitMQ** - Message queue via MassTransit
- **OpenTelemetry** - Observability

## Dependencies

### Databases
- **PostgreSQL**: Receipt records, void tracking, compliance logs
- **Redis**: Receipt number sequence caching

### Messaging
- **RabbitMQ**: Events for receipt issuance and voids

### External Services
- **IAM Service**: Authentication and authorization
- **Payment Service**: Payment transaction data
- **Customer Service**: Customer information
- **PDF Service**: Receipt PDF generation
- **Accounting Service**: Financial record integration

## IAM Integration

### Required Permissions
- `receipts.read` - View receipts
- `receipts.create` - Issue new receipts
- `receipts.void` - Void receipts (requires special authorization)
- `receipts.reissue` - Reissue voided receipts
- `receipts.audit.read` - View receipt audit trails
- `receipts.export` - Export receipt data for compliance

### Predefined Roles
- **Cashier**: Create and view receipts
- **Supervisor**: Void and reissue receipts
- **Accountant**: Full access including audit trail review
- **Auditor**: Read-only access to all receipts and audit data

### Feature Flags
- `Features:PermissionBasedAuthEnabled` - Enable/disable IAM integration

## API Endpoints

### Receipts
- `GET /v1/receipts` - List receipts (with filters)
- `POST /v1/receipts` - Create and issue receipt
- `GET /v1/receipts/{id}` - Get receipt details
- `POST /v1/receipts/{id}/void` - Void receipt (requires reason)
- `POST /v1/receipts/{id}/reissue` - Reissue voided receipt
- `GET /v1/receipts/{id}/pdf` - Get receipt PDF
- `GET /v1/receipts/number/{receiptNumber}` - Get by receipt number
- `GET /v1/receipts/payment/{paymentId}` - Get receipts for payment
- `GET /v1/receipts/customer/{customerId}` - Get customer receipts

### Compliance
- `GET /v1/receipts/audit-trail/{id}` - Get receipt audit trail
- `GET /v1/receipts/void-log` - Get void operations log
- `POST /v1/receipts/export` - Export receipts for tax filing
- `GET /v1/receipts/compliance-report` - Generate compliance report

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "ReceiptDatabase": "Host=postgres;Port=5432;Database=maliev_receipts;Username=app;Password=secret",
    "Redis": "redis:6379"
  },
  "RabbitMQ": {
    "Host": "rabbitmq",
    "Username": "guest",
    "Password": "guest"
  },
  "Jwt": {
    "Key": "base64-encoded-key",
    "Issuer": "maliev-receipt-service",
    "Audience": "maliev-services"
  },
  "ExternalServices": {
    "IAM": {
      "BaseUrl": "http://iam-service:8080",
      "ServiceName": "ReceiptService"
    },
    "PDF": {
      "BaseUrl": "http://pdf-service:8080"
    }
  },
  "Features": {
    "PermissionBasedAuthEnabled": true
  },
  "Receipt": {
    "NumberPrefix": "RCP",
    "NumberLength": 10,
    "RequireVoidReason": true,
    "AllowVoidAfterDays": 7
  }
}
```

## Database

**PostgreSQL 18** with Entity Framework Core migrations.

**Main Tables:**
- `Receipts` - Receipt master (number, amount, date, status, customer)
- `ReceiptLines` - Line items (description, amount, tax)
- `ReceiptVoids` - Void operations (date, reason, user)
- `ReceiptAuditTrail` - Complete audit history
- `ReceiptSequence` - Receipt number sequence

**Status Values:**
- Issued, Voided, Reissued

## Running the Service

### Development
```bash
cd Maliev.ReceiptService.Api
dotnet run
```

**Access:**
- API: http://localhost:5000
- Health: http://localhost:5000/receipts/liveness
- Metrics: http://localhost:5000/receipts/metrics

### Docker
```bash
docker build -t maliev/receipt-service:latest .
docker run -p 8080:8080 maliev/receipt-service:latest
```

### Tests
```bash
# Ensure Docker is running
docker ps

# Run tests
dotnet test
```

## Test Status

**From Test Summary (2025-12-24):**
- **Status**: FAILED (15 tests)
- **Critical Issue**: Authorization checks not properly enforced
- **Pattern**: Expected Forbidden (403), but received NotFound (404)

**Issues to Fix:**
1. Implement proper permission validation BEFORE resource lookup
2. Return 403 Forbidden for insufficient permissions
3. Return 404 Not Found only when resource doesn't exist AND user has permission

**Failed Test Examples:**
- `CreateReceipt_WithoutPermission_ShouldFail` - Expected 403, got 404
- `VoidReceipt_WithoutPermission_ShouldFail` - Expected 403, got 404

## Key Features

### Receipt Management
- **Automatic Numbering**: Sequential receipt numbers with prefix
- **Multi-Payment Support**: Receipts for multiple payment methods
- **Tax Calculation**: Automatic tax computation and breakdown
- **Currency Support**: Multi-currency receipts with exchange rates
- **Batch Issuance**: Bulk receipt generation

### Compliance
- **Immutable Records**: Receipts cannot be edited (only voided)
- **Void Tracking**: Complete audit trail for voided receipts
- **Legal Compliance**: Meets tax authority requirements
- **Export Capabilities**: Export for tax filing and audits
- **Retention Policy**: Automatic archival per legal requirements

### Integration
- **PDF Generation**: Automatic PDF receipt generation
- **Email Delivery**: Send receipts to customers via email
- **Payment Linking**: Link receipts to payment transactions
- **Accounting Integration**: Auto-create accounting journal entries

### Audit & Security
- **Complete Audit Trail**: Who, what, when for every operation
- **Void Authorization**: Special permission required to void
- **Time-Based Controls**: Void only allowed within configured period
- **Tamper Detection**: Digital signatures for receipt integrity

## Events Published

- `ReceiptIssuedEvent` - New receipt issued
- `ReceiptVoidedEvent` - Receipt voided
- `ReceiptReissuedEvent` - Receipt reissued after void
- `ReceiptExportedEvent` - Receipts exported for compliance

## Events Consumed

- `PaymentCompletedEvent` - Auto-issue receipt for payment
- `RefundProcessedEvent` - Issue credit receipt for refund

## Business Rules

1. **Immutability**: Issued receipts cannot be modified
2. **Void Period**: Receipts can only be voided within configured days (default: 7)
3. **Void Reason Required**: All voids must have documented reason
4. **Sequential Numbers**: Receipt numbers must be sequential (no gaps)
5. **Reissuance**: Voided receipts can be reissued with new number
6. **Permission Required**: Void operations require special permission

## Compliance Features

### Tax Authority Requirements
- Sequential numbering (no gaps)
- Immutable once issued
- Void tracking with justification
- Minimum retention period (7 years)
- Digital signature support

### Audit Support
- Complete change history
- User accountability
- Void operation logging
- Export capabilities for auditors

## Support

For detailed permissions and roles, see `specs/002-iam-integration/data-model.md`

- Test Summary: `B:\maliev\all-services-test-summary.txt`
- ServiceDefaults: `B:\maliev\Maliev.Aspire\Maliev.Aspire.ServiceDefaults\README.md`

## License

Proprietary - Copyright 2025 MALIEV Co., Ltd. All rights reserved.
