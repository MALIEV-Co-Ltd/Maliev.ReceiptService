# Data Model: Receipt Service Core

**Feature**: Receipt Service Core
**Date**: 2025-12-05
**Status**: Complete

## Overview

This document defines the data model for the Receipt Service, including all entities, their properties, relationships, validation rules, and state transitions. The model enforces immutability after creation (corrections via void+recreate only) and maintains a complete audit trail for financial compliance.

---

## Entities

### 1. Receipt

**Purpose**: Represents a customer-facing receipt issued by MALIEV for a paid or partially paid invoice. Receipts are the authoritative record of payment acknowledgment.

**Table Name**: `Receipts`

**Properties**:

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| `Id` | Guid | PK, Required | Unique identifier (UUID v4) |
| `ReceiptNumber` | string(50) | Required, Unique, Indexed | Sequential format: ENTITY-YYYY-NNNNNN (e.g., MALIEV-2025-000001) |
| `InvoiceId` | Guid | Required, Indexed, FK → Invoice Service | Reference to invoice in Invoice Service |
| `IssueDate` | DateTime | Required, Default: UtcNow | Receipt issue timestamp (UTC) |
| `CustomerName` | string(200) | Required | Customer name from invoice |
| `CustomerTaxId` | string(50) | Optional | Customer tax ID for compliance |
| `CustomerAddress` | string(500) | Optional | Customer address |
| `Subtotal` | decimal(18,2) | Required, >= 0 | Amount before taxes |
| `TaxAmount` | decimal(18,2) | Required, >= 0 | Total tax amount |
| `WithholdingTaxAmount` | decimal(18,2) | Optional, >= 0 | Withholding tax if applicable |
| `TotalAmount` | decimal(18,2) | Required, > 0 | Final receipt amount (Subtotal + Tax - WithholdingTax) |
| `Currency` | string(3) | Required, Default: "THB" | ISO 4217 currency code |
| `PaymentMethod` | string(50) | Optional | Payment method (Cash, Bank Transfer, Credit Card, etc.) |
| `Status` | enum | Required, Default: Active | Active, Void, PendingPdf |
| `PdfReferenceId` | Guid? | Optional, FK → Upload Service | Reference to PDF in Upload Service |
| `CreatedAt` | DateTime | Required, Default: UtcNow | Creation timestamp (UTC) |
| `CreatedBy` | string(100) | Required, FK → User Service | Staff member ID who created receipt |
| `VoidedAt` | DateTime? | Optional | Void timestamp if voided |
| `VoidedBy` | string(100) | Optional, FK → User Service | Staff member ID who voided |
| `VoidReason` | string(500) | Optional | Reason for voiding |
| `CorrelationId` | Guid | Required | Distributed tracing identifier |
| `RowVersion` | byte[] | Timestamp | Optimistic concurrency control |

**Indexes**:
- `IX_Receipts_ReceiptNumber` (Unique)
- `IX_Receipts_InvoiceId`
- `IX_Receipts_IssueDate`
- `IX_Receipts_Status`
- `IX_Receipts_CreatedAt`

**Immutability Rules**:
- All financial fields (`ReceiptNumber`, `Subtotal`, `TaxAmount`, `TotalAmount`, `Currency`) are `init`-only after creation
- Only `Status`, `PdfReferenceId`, `VoidedAt`, `VoidedBy`, `VoidReason` can be modified
- Modifications trigger audit event creation

**State Transitions**:
```
Active → Void (via Void() method)
Active → PendingPdf → Active (via PDF generation callback)
Void → [No transitions allowed]
```

**Validation Rules**:
- `TotalAmount` must equal `Subtotal + TaxAmount - WithholdingTaxAmount`
- `ReceiptNumber` must follow format: `^[A-Z]+-\d{4}-\d{6}$`
- `Status` = Void requires `VoidReason`, `VoidedBy`, `VoidedAt`
- `Currency` must be valid ISO 4217 code

---

### 2. ReceiptLineItem

**Purpose**: Represents individual line items from the invoice that are included in the receipt. Maintains itemization for audit and tax compliance.

**Table Name**: `ReceiptLineItems`

**Properties**:

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| `Id` | Guid | PK, Required | Unique identifier |
| `ReceiptId` | Guid | Required, FK → Receipts, Cascading Delete | Parent receipt |
| `InvoiceLineItemId` | Guid | Optional | Reference to original invoice line item |
| `LineNumber` | int | Required, >= 1 | Display order (1, 2, 3...) |
| `Description` | string(500) | Required | Item/service description |
| `Quantity` | decimal(18,4) | Required, > 0 | Quantity (supports fractional like 1.5 hours) |
| `UnitPrice` | decimal(18,2) | Required, >= 0 | Price per unit |
| `TaxRate` | decimal(5,2) | Required, >= 0 | Tax rate as percentage (e.g., 7.00 for 7%) |
| `LineTotal` | decimal(18,2) | Required, >= 0 | Line total (Quantity × UnitPrice × (1 + TaxRate/100)) |

**Indexes**:
- `IX_ReceiptLineItems_ReceiptId`
- `IX_ReceiptLineItems_InvoiceLineItemId`

**Immutability Rules**:
- All fields are `init`-only after creation
- Deleted only when parent `Receipt` is deleted (cascading)

**Validation Rules**:
- `LineTotal` must equal `Quantity × UnitPrice × (1 + TaxRate/100)` (with rounding tolerance of ±0.01)
- `LineNumber` must be unique within a receipt
- At least one line item required per receipt

---

### 3. ReceiptAuditEvent

**Purpose**: Immutable audit trail of all receipt lifecycle events. Required for financial compliance and 7-year retention.

**Table Name**: `ReceiptAuditEvents`

**Properties**:

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| `Id` | Guid | PK, Required | Unique identifier |
| `ReceiptId` | Guid | Required, FK → Receipts, No Cascade | Receipt being audited |
| `EventType` | enum | Required | Created, Voided, PdfGenerated, Corrected |
| `Timestamp` | DateTime | Required, Default: UtcNow, Indexed | Event occurrence time (UTC) |
| `StaffMemberId` | string(100) | Required, FK → User Service | User who performed action |
| `Reason` | string(1000) | Optional | Reason for action (e.g., void reason) |
| `PreviousState` | string(MAX) | Optional | JSON snapshot of receipt before change |
| `NewState` | string(MAX) | Required | JSON snapshot of receipt after change |
| `CorrelationId` | Guid | Required | Distributed tracing identifier |
| `RetainUntil` | DateTime | Required, Indexed | Timestamp + 7 years (for cleanup job) |

**Indexes**:
- `IX_ReceiptAuditEvents_ReceiptId`
- `IX_ReceiptAuditEvents_Timestamp`
- `IX_ReceiptAuditEvents_RetainUntil`
- `IX_ReceiptAuditEvents_EventType`

**Immutability Rules**:
- **Append-only**: No UPDATE or DELETE operations allowed
- Cleanup only after `RetainUntil` date

**Retention Policy**:
- `RetainUntil` = `Timestamp + 7 years`
- Automated cleanup job (scheduled task) deletes events where `RetainUntil < NOW()`

**Event Type Details**:
- **Created**: Receipt initially created, `PreviousState` = null
- **Voided**: Receipt voided, both states populated
- **PdfGenerated**: PDF reference added, both states populated
- **Corrected**: Original voided + new created (2 events)

---

### 4. InvoiceBalanceTracker

**Purpose**: Tracks receiptable balance for each invoice to prevent over-receipting and support partial payments. Uses optimistic locking to prevent race conditions.

**Table Name**: `InvoiceBalanceTrackers`

**Properties**:

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| `InvoiceId` | Guid | PK, Required, FK → Invoice Service | Invoice being tracked |
| `TotalInvoiceAmount` | decimal(18,2) | Required, > 0 | Total invoice amount (from Invoice Service) |
| `TotalReceiptedAmount` | decimal(18,2) | Required, >= 0, Default: 0 | Sum of all non-void receipts |
| `RemainingBalance` | decimal(18,2) | Required, >= 0 | TotalInvoiceAmount - TotalReceiptedAmount |
| `LastUpdatedAt` | DateTime | Required, Default: UtcNow | Last balance update timestamp |
| `RowVersion` | byte[] | Timestamp, Required | Optimistic concurrency control |

**Indexes**:
- `PK_InvoiceBalanceTrackers_InvoiceId` (Clustered PK)
- `IX_InvoiceBalanceTrackers_RemainingBalance`

**Concurrency Control**:
- Uses EF Core's `[Timestamp]` attribute for `RowVersion`
- Any update that fails concurrency check returns 409 Conflict to user
- User retries with fresh data

**Validation Rules**:
- `TotalReceiptedAmount` ≤ `TotalInvoiceAmount`
- `RemainingBalance` must equal `TotalInvoiceAmount - TotalReceiptedAmount`
- Cannot create receipt if `request.Amount > RemainingBalance`

**Lifecycle**:
- **Created**: When first receipt for invoice is created
- **Updated**: Every time a receipt is created or voided
- **Deleted**: Never (or via manual cleanup after invoice archival)

---

## Entity Relationships

```
Receipt (1) ──┬─→ (N) ReceiptLineItem
              │
              └─→ (N) ReceiptAuditEvent

InvoiceBalanceTracker (1) ←──→ (N) Receipt
   (via InvoiceId)

External References (HTTP/FK to other services):
- Receipt.InvoiceId → Invoice Service
- Receipt.CreatedBy → User Service
- Receipt.PdfReferenceId → Upload Service
```

**Relationship Details**:

| Relationship | Type | Cascade | Description |
|--------------|------|---------|-------------|
| Receipt → ReceiptLineItem | One-to-Many | Delete | Deleting receipt deletes all line items |
| Receipt → ReceiptAuditEvent | One-to-Many | None | Audit events retained even if receipt deleted |
| InvoiceBalanceTracker → Receipt | One-to-Many | None | Balance tracker independent of receipts |

---

## Database Schema (PostgreSQL DDL)

```sql
-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Receipts table
CREATE TABLE "Receipts" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "ReceiptNumber" VARCHAR(50) NOT NULL UNIQUE,
    "InvoiceId" UUID NOT NULL,
    "IssueDate" TIMESTAMP NOT NULL DEFAULT NOW(),
    "CustomerName" VARCHAR(200) NOT NULL,
    "CustomerTaxId" VARCHAR(50),
    "CustomerAddress" VARCHAR(500),
    "Subtotal" DECIMAL(18,2) NOT NULL CHECK ("Subtotal" >= 0),
    "TaxAmount" DECIMAL(18,2) NOT NULL CHECK ("TaxAmount" >= 0),
    "WithholdingTaxAmount" DECIMAL(18,2) CHECK ("WithholdingTaxAmount" >= 0),
    "TotalAmount" DECIMAL(18,2) NOT NULL CHECK ("TotalAmount" > 0),
    "Currency" VARCHAR(3) NOT NULL DEFAULT 'THB',
    "PaymentMethod" VARCHAR(50),
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Active' CHECK ("Status" IN ('Active', 'Void', 'PendingPdf')),
    "PdfReferenceId" UUID,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "CreatedBy" VARCHAR(100) NOT NULL,
    "VoidedAt" TIMESTAMP,
    "VoidedBy" VARCHAR(100),
    "VoidReason" VARCHAR(500),
    "CorrelationId" UUID NOT NULL,
    "RowVersion" BYTEA NOT NULL DEFAULT E'\\x00000000'
);

CREATE INDEX "IX_Receipts_ReceiptNumber" ON "Receipts" ("ReceiptNumber");
CREATE INDEX "IX_Receipts_InvoiceId" ON "Receipts" ("InvoiceId");
CREATE INDEX "IX_Receipts_IssueDate" ON "Receipts" ("IssueDate");
CREATE INDEX "IX_Receipts_Status" ON "Receipts" ("Status");
CREATE INDEX "IX_Receipts_CreatedAt" ON "Receipts" ("CreatedAt");

-- Receipt Line Items table
CREATE TABLE "ReceiptLineItems" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "ReceiptId" UUID NOT NULL REFERENCES "Receipts"("Id") ON DELETE CASCADE,
    "InvoiceLineItemId" UUID,
    "LineNumber" INT NOT NULL CHECK ("LineNumber" >= 1),
    "Description" VARCHAR(500) NOT NULL,
    "Quantity" DECIMAL(18,4) NOT NULL CHECK ("Quantity" > 0),
    "UnitPrice" DECIMAL(18,2) NOT NULL CHECK ("UnitPrice" >= 0),
    "TaxRate" DECIMAL(5,2) NOT NULL CHECK ("TaxRate" >= 0),
    "LineTotal" DECIMAL(18,2) NOT NULL CHECK ("LineTotal" >= 0),
    UNIQUE ("ReceiptId", "LineNumber")
);

CREATE INDEX "IX_ReceiptLineItems_ReceiptId" ON "ReceiptLineItems" ("ReceiptId");
CREATE INDEX "IX_ReceiptLineItems_InvoiceLineItemId" ON "ReceiptLineItems" ("InvoiceLineItemId");

-- Receipt Audit Events table
CREATE TABLE "ReceiptAuditEvents" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "ReceiptId" UUID NOT NULL REFERENCES "Receipts"("Id"),
    "EventType" VARCHAR(50) NOT NULL CHECK ("EventType" IN ('Created', 'Voided', 'PdfGenerated', 'Corrected')),
    "Timestamp" TIMESTAMP NOT NULL DEFAULT NOW(),
    "StaffMemberId" VARCHAR(100) NOT NULL,
    "Reason" VARCHAR(1000),
    "PreviousState" TEXT,
    "NewState" TEXT NOT NULL,
    "CorrelationId" UUID NOT NULL,
    "RetainUntil" TIMESTAMP NOT NULL
);

CREATE INDEX "IX_ReceiptAuditEvents_ReceiptId" ON "ReceiptAuditEvents" ("ReceiptId");
CREATE INDEX "IX_ReceiptAuditEvents_Timestamp" ON "ReceiptAuditEvents" ("Timestamp");
CREATE INDEX "IX_ReceiptAuditEvents_RetainUntil" ON "ReceiptAuditEvents" ("RetainUntil");
CREATE INDEX "IX_ReceiptAuditEvents_EventType" ON "ReceiptAuditEvents" ("EventType");

-- Invoice Balance Trackers table
CREATE TABLE "InvoiceBalanceTrackers" (
    "InvoiceId" UUID PRIMARY KEY,
    "TotalInvoiceAmount" DECIMAL(18,2) NOT NULL CHECK ("TotalInvoiceAmount" > 0),
    "TotalReceiptedAmount" DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK ("TotalReceiptedAmount" >= 0),
    "RemainingBalance" DECIMAL(18,2) NOT NULL CHECK ("RemainingBalance" >= 0),
    "LastUpdatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "RowVersion" BYTEA NOT NULL DEFAULT E'\\x00000000',
    CHECK ("TotalReceiptedAmount" <= "TotalInvoiceAmount"),
    CHECK ("RemainingBalance" = ("TotalInvoiceAmount" - "TotalReceiptedAmount"))
);

CREATE INDEX "IX_InvoiceBalanceTrackers_RemainingBalance" ON "InvoiceBalanceTrackers" ("RemainingBalance");
```

---

## Data Integrity Constraints

### Application-Level Validation (EF Core + Data Annotations)

```csharp
public class Receipt
{
    [Key]
    public Guid Id { get; init; }

    [Required]
    [MaxLength(50)]
    [RegularExpression(@"^[A-Z]+-\d{4}-\d{6}$")]
    public string ReceiptNumber { get; init; }

    [Required]
    public Guid InvoiceId { get; init; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Subtotal { get; init; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; init; }

    [Range(0, double.MaxValue)]
    public decimal? WithholdingTaxAmount { get; init; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal TotalAmount { get; init; }

    [Timestamp]
    public byte[] RowVersion { get; set; }

    // Computed property for validation
    public bool IsValidTotal =>
        Math.Abs(TotalAmount - (Subtotal + TaxAmount - (WithholdingTaxAmount ?? 0))) < 0.01m;
}
```

### Database-Level Constraints

- **CHECK constraints**: Enforce non-negative amounts, valid enum values
- **UNIQUE constraints**: Prevent duplicate receipt numbers
- **FOREIGN KEY constraints**: Maintain referential integrity (with appropriate cascading)
- **NOT NULL**: Ensure required fields present

---

## Migration Strategy

**EF Core Migrations**:
1. Initial migration creates all tables
2. Seed data for testing environments (sample receipts, audit events)
3. Production migrations run on startup via `app.MigrateDatabaseAsync<ReceiptDbContext>()`

**Data Migration Considerations**:
- If migrating from legacy system, maintain legacy receipt numbers in separate column
- Audit events should be backfilled for existing receipts (with `EventType` = "Migrated")

---

**Phase 1 (Data Model) Status**: ✅ Complete - All entities defined with properties, relationships, validation rules, and database schema. Ready for API contract generation.
