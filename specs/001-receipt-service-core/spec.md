# Feature Specification: Receipt Service Core

**Feature Branch**: `001-receipt-service-core`
**Created**: 2025-12-05
**Status**: Draft
**Input**: User description: "The Receipt Service is responsible for managing all receipts issued by MALIEV to customers and acts as the authoritative source of receipt data within the microservice ecosystem..."

## Clarifications

### Session 2025-12-05

- Q: What numbering scheme should be used for receipt identifiers to ensure uniqueness and compliance? → A: Sequential per legal entity with prefix (e.g., MALIEV-2025-000001, MALIEV-2025-000002)
- Q: What is the required retention period for audit trail records? → A: 7 years (standard financial record retention)
- Q: Can receipts be modified after creation, or are they immutable? → A: Immutable after creation; corrections via void+recreate only
- Q: What level of observability (logging, metrics, tracing) is required for production support? → A: Structured logging + key metrics + distributed tracing (correlation IDs across services)
- Q: What timeout and retry strategy should be used for Invoice Service calls? → A: 5-second timeout with 2 retries and exponential backoff (1s, 2s delays)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Full Payment Receipt Issuance (Priority: P1)

A finance staff member needs to generate a receipt for a customer who has paid their invoice in full. The staff member selects the paid invoice, initiates receipt generation, and the system automatically creates a receipt record, validates all invoice data, applies tax rules, and triggers PDF generation.

**Why this priority**: This is the core value proposition of the Receipt Service - converting paid invoices into compliant receipts. Without this, no receipts can be issued at all.

**Independent Test**: Can be fully tested by creating a paid invoice, generating a receipt, and verifying the receipt record exists with correct data and a PDF generation event was published. Delivers immediate value by enabling basic receipt issuance.

**Acceptance Scenarios**:

1. **Given** a valid paid invoice exists in the Invoice Service, **When** staff initiates receipt creation for the full invoice amount, **Then** a receipt record is created with all invoice details, tax calculations, and a PDF generation event is published to RabbitMQ
2. **Given** a receipt was successfully created, **When** the PDF Service completes generation, **Then** the receipt record is updated with the PDF reference ID from the Upload Service
3. **Given** an invoice has already been fully receipted, **When** staff attempts to create another full-amount receipt for the same invoice, **Then** the system rejects the request with a clear error message about duplicate receipts

---

### User Story 2 - Partial Payment Receipt Handling (Priority: P2)

A customer makes a partial payment on their invoice. The finance staff needs to generate a receipt for only the amount paid, while tracking the remaining balance. The system should allow multiple partial receipts until the invoice is fully receipted.

**Why this priority**: Many customers pay in installments or partial amounts. This capability is essential for real-world business operations but builds on the foundational full-payment flow.

**Independent Test**: Can be tested by creating an invoice, generating multiple receipts for portions of the total, and verifying the system correctly tracks remaining balance and prevents over-receipting. Delivers value by supporting flexible payment scenarios.

**Acceptance Scenarios**:

1. **Given** an invoice with a total of $1000, **When** staff creates a receipt for $400, **Then** a partial receipt is generated and the system tracks $600 as the remaining receiptable balance
2. **Given** an invoice already has $400 receipted, **When** staff creates a second receipt for $300, **Then** the receipt is accepted and the remaining balance updates to $300
3. **Given** an invoice has $700 receipted with $300 remaining, **When** staff attempts to create a receipt for $500, **Then** the system rejects the request indicating it would exceed the invoice total
4. **Given** multiple partial receipts exist for an invoice, **When** staff queries the invoice receipt status, **Then** the system returns all receipt records and the current balance breakdown

---

### User Story 3 - Receipt Void and Correction (Priority: P2)

A receipt was issued incorrectly or needs to be canceled due to payment reversal. The finance staff needs to void the receipt while maintaining a complete audit trail. The system should mark the receipt as void without deleting it, and adjust any related balances.

**Why this priority**: Financial corrections are legally required for compliance and audit purposes. While not needed for initial issuance, this is critical before going to production.

**Independent Test**: Can be tested by creating a receipt, voiding it, and verifying it's marked void (not deleted), audit trail is created, and invoice balance is restored. Delivers value by ensuring compliance and correction capabilities.

**Acceptance Scenarios**:

1. **Given** a valid receipt exists, **When** staff initiates a void operation with a reason, **Then** the receipt status changes to "void", the audit trail records the action with timestamp and reason, and the invoice receiptable balance is restored
2. **Given** a voided receipt, **When** staff attempts to void it again, **Then** the system rejects the request indicating the receipt is already void
3. **Given** a receipt needs correction, **When** staff voids the incorrect receipt and creates a new corrected receipt, **Then** both receipts appear in history with the voided one clearly marked and the new one active

---

### User Story 4 - Split Invoice Receipt Generation (Priority: P3)

An invoice has been split across multiple payment methods or installment plans. The finance staff needs to generate receipts that align with the split invoice structure, ensuring each receipt portion corresponds to a specific payment segment.

**Why this priority**: Supports advanced payment scenarios and customer flexibility. This is an enhancement over basic partial payments as it requires coordination with invoice splitting logic.

**Independent Test**: Can be tested by creating a split invoice with defined segments, generating receipts for each segment, and verifying they align with the split structure. Delivers value for complex payment arrangements.

**Acceptance Scenarios**:

1. **Given** an invoice split into 3 payment segments of $500, $300, and $200, **When** staff generates a receipt for the first segment, **Then** a receipt for exactly $500 is created and linked to that specific invoice segment
2. **Given** a split invoice with segment-specific tax rates, **When** a receipt is generated for a segment, **Then** the receipt applies the correct tax rate for that segment only
3. **Given** multiple segments of a split invoice, **When** staff queries receipt status, **Then** the system clearly shows which segments have been receipted and which remain outstanding

---

### User Story 5 - Receipt Query and Analytics Access (Priority: P3)

Business analysts and dashboard tools need to query receipt data for reporting. The system must provide efficient access to receipt records, payment completion metrics, outstanding balances, and customer payment behavior without impacting operational performance.

**Why this priority**: Enables business intelligence and operational visibility but doesn't affect core receipt issuance functionality. Can be delivered after core CRUD operations work.

**Independent Test**: Can be tested by creating various receipts and querying through analytics endpoints to retrieve aggregated metrics and filtered datasets. Delivers value by enabling data-driven decision making.

**Acceptance Scenarios**:

1. **Given** multiple receipts across different customers and time periods, **When** an analytics tool queries for payment completion rate by month, **Then** the system returns accurate aggregated data within 2 seconds
2. **Given** receipt records exist, **When** a dashboard queries for outstanding receivables, **Then** the system calculates and returns the total invoiced amount minus total receipted amount
3. **Given** high query load from analytics tools, **When** operational staff create new receipts, **Then** receipt creation performance is not degraded by analytics queries (caching isolates reads from writes)

---

### User Story 6 - Receipt Data Validation and Tax Compliance (Priority: P1)

The system must validate all receipt data against local tax regulations before allowing receipt creation. Mandatory government-required fields must be present and correctly formatted in both the stored receipt and the PDF generation payload.

**Why this priority**: Tax compliance is non-negotiable and must be enforced from day one. Without this, receipts could be legally invalid.

**Independent Test**: Can be tested by attempting to create receipts with missing or invalid tax-required fields and verifying they are rejected with specific validation errors. Delivers value by ensuring legal compliance.

**Acceptance Scenarios**:

1. **Given** local tax regulations require a tax identification number, **When** staff creates a receipt from an invoice missing this field, **Then** the system rejects the request with a clear error identifying the missing required field
2. **Given** an invoice with withholding tax status, **When** a receipt is generated, **Then** the receipt record includes the withholding tax amount and status in the format required for tax reporting
3. **Given** mandatory fields are populated, **When** the PDF generation event is published, **Then** the payload includes all government-required fields in the exact format specified for tax compliance

---

### Edge Cases

- What happens when the Invoice Service is temporarily unavailable during receipt creation? The system should fail gracefully using a 5-second timeout per attempt with up to 2 retries (exponential backoff: 1s, 2s delays), returning a clear error message if all attempts fail, and never creating partial/invalid receipts.
- How does the system handle race conditions where two staff members try to create receipts for the same invoice simultaneously? The system must use optimistic locking or similar concurrency control to prevent duplicate receipts.
- What happens when a receipt is created successfully but the PDF generation event fails to publish to RabbitMQ? The receipt should still exist but be marked as "pending PDF" to allow retry of PDF generation without recreating the receipt.
- How does the system handle invoices with zero or negative amounts? Validation should reject negative amounts; zero-amount receipts may be allowed if business rules permit (e.g., for fully discounted items).
- What happens when an invoice has been partially paid and then the customer requests a refund for part of the paid amount? The system should support voiding the affected receipt and adjusting balances accordingly.
- How does the system handle currency precision and rounding for partial payments? The system must use consistent precision (e.g., 2 decimal places for most currencies) and handle rounding to prevent balance mismatches.
- What happens when tax rates change between invoice creation and receipt issuance? The system should use the tax rates from the invoice at the time it was created, not current rates, to maintain consistency.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST retrieve complete invoice details from the Invoice Service using the provided Invoice ID before creating any receipt
- **FR-002**: System MUST validate that an invoice is eligible for receipt issuance by checking invoice status, payment status, and business rules
- **FR-003**: System MUST prevent duplicate receipts by validating that the same invoice segment has not already been fully receipted
- **FR-004**: System MUST support receipt creation for full invoice amounts, capturing all invoice line items, taxes, and totals
- **FR-005**: System MUST support receipt creation for partial invoice amounts, specifying the exact amount being receipted
- **FR-006**: System MUST track the receiptable balance for each invoice, calculated as invoice total minus sum of all valid (non-void) receipts
- **FR-007**: System MUST enforce that total receipted amount cannot exceed the invoice total amount
- **FR-008**: System MUST validate invoice totals, itemization, taxes, and withholding tax status before creating a receipt
- **FR-009**: System MUST apply tax regulation validation rules ensuring mandatory government-required fields are present and correctly formatted
- **FR-010**: System MUST store receipt records persistently with all financial data, invoice references, amounts, taxes, and metadata
- **FR-010a**: System MUST generate unique receipt numbers using sequential numbering per legal entity with prefix format ENTITY-YYYY-NNNNNN (e.g., MALIEV-2025-000001), ensuring no gaps or duplicates in the sequence
- **FR-010b**: System MUST treat receipts as immutable after creation - no edits to financial data, amounts, or line items are allowed; corrections require voiding the original receipt and creating a new one
- **FR-011**: System MUST publish a PDF generation event to RabbitMQ immediately after successfully creating a receipt record
- **FR-012**: System MUST include all government-required fields in the PDF generation event payload sent to the PDF Service
- **FR-013**: System MUST store PDF reference identifiers (from Upload Service) in the receipt record but NOT store the PDF binary data
- **FR-014**: System MUST support voiding receipts while preserving the original record and creating an audit trail entry
- **FR-015**: System MUST restore invoice receiptable balance when a receipt is voided
- **FR-016**: System MUST record all receipt lifecycle events (creation, void, correction, adjustments) in an immutable audit trail
- **FR-016a**: System MUST retain audit trail records for a minimum of 7 years to comply with financial record retention regulations
- **FR-017**: System MUST associate audit trail entries with the staff member (User Service identity) who performed the action and timestamp
- **FR-018**: System MUST support split invoice receipt generation, allowing receipts to be linked to specific invoice segments
- **FR-019**: System MUST apply segment-specific attributes (tax rates, amounts, line items) when generating receipts for split invoices
- **FR-020**: System MUST expose query APIs for retrieving receipt records by invoice ID, customer, date range, status, and other filters
- **FR-021**: System MUST expose analytics APIs for payment completion rates, outstanding receivables, customer payment behavior, and processing time metrics
- **FR-022**: System MUST implement caching strategies for frequently accessed receipt data to ensure query performance
- **FR-023**: System MUST integrate with User Service for authorization and staff identity resolution
- **FR-024**: System MUST NOT store or manage external receipts issued by suppliers to MALIEV (those belong to Financial/Accounting Service)
- **FR-025**: System MUST handle Invoice Service unavailability gracefully with appropriate error messages and retry logic (5-second timeout per attempt, maximum 2 retries with exponential backoff delays of 1s and 2s)
- **FR-026**: System MUST use optimistic locking or equivalent concurrency control to prevent duplicate receipt creation in concurrent scenarios
- **FR-027**: System MUST maintain data integrity and consistency for all persistent financial data
- **FR-028**: System MUST validate currency precision and handle rounding consistently to prevent balance mismatches
- **FR-029**: System MUST preserve invoice-time tax rates and attributes in receipts regardless of subsequent tax rate changes
- **FR-030**: System MUST emit structured logs (JSON format) for all receipt operations, including correlation IDs for tracing requests across Invoice Service, PDF Service, and Upload Service
- **FR-031**: System MUST expose key business metrics including receipt creation rate, void rate, error rates by type, processing duration, and invoice service call latency
- **FR-032**: System MUST propagate correlation IDs in all outbound requests and events to enable distributed tracing across the microservice ecosystem

### Key Entities

- **Receipt**: Represents a customer-facing receipt issued by MALIEV for a paid or partially paid invoice. Receipts are immutable after creation - financial data cannot be edited; corrections require voiding and recreating. Core attributes include receipt number (sequential per legal entity with prefix format: ENTITY-YYYY-NNNNNN, e.g., MALIEV-2025-000001), invoice reference, customer details, issue date, amounts (subtotal, taxes, withholding tax, total), payment method, status (active/void), PDF reference ID, and timestamps.

- **Receipt Line Item**: Represents individual line items from the invoice that are included in the receipt. Attributes include description, quantity, unit price, tax rate, line total, and reference to original invoice line item.

- **Receipt Audit Event**: Immutable record of all lifecycle events for a receipt. Attributes include event type (created/voided/corrected/adjusted), timestamp, staff member ID, reason/notes, previous state, new state, and correlation ID. Records must be retained for minimum 7 years for regulatory compliance.

- **Invoice Balance Tracker**: Tracks the receiptable balance for each invoice. Attributes include invoice ID, total invoice amount, total receipted amount (sum of non-void receipts), remaining balance, last updated timestamp, and concurrency version for optimistic locking.

- **PDF Generation Event**: Event published to RabbitMQ to trigger PDF creation. Attributes include receipt ID, customer details, all receipt data, government-required fields formatted for tax compliance, template identifier, and correlation ID for distributed tracing across Receipt Service, PDF Service, and Upload Service.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Finance staff can generate a receipt for a paid invoice in under 5 seconds from initiation to receipt record creation
- **SC-002**: System prevents 100% of duplicate receipt attempts with clear error messaging
- **SC-003**: All receipts include mandatory government-required fields with zero compliance violations in audit reviews
- **SC-004**: PDF generation events are successfully published within 1 second of receipt creation for 99.9% of receipts
- **SC-005**: System accurately tracks invoice receiptable balances with zero calculation errors across partial payments and voids
- **SC-006**: Receipt void operations complete in under 3 seconds and correctly restore invoice balances
- **SC-007**: Analytics queries return results within 2 seconds for datasets covering up to 100,000 receipts
- **SC-008**: System maintains 99.99% data integrity with no lost or corrupted receipt records
- **SC-009**: Receipt creation operations succeed even under concurrent load of 50 simultaneous requests without duplicate creation
- **SC-010**: System handles Invoice Service unavailability gracefully with 100% of requests failing cleanly (no partial receipts created)
- **SC-011**: Audit trail captures 100% of receipt lifecycle events with complete staff attribution and timestamps
- **SC-012**: Payment completion rate reporting is accurate within 0.1% margin of error when compared to manual calculation
