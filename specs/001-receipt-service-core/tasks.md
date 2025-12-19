# Tasks: Receipt Service Core

**Input**: Design documents from `/specs/001-receipt-service-core/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Constitution Principle III (Test-First Development) requires tests before implementation. All test tasks are included and MUST pass (fail initially) before implementation begins.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Per plan.md, this is a microservice project:
- **API Project**: `ReceiptService.Api/`
- **Test Project**: `ReceiptService.Tests/`
- Repository root: `B:\maliev\Maliev.ReceiptService\`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure per quickstart.md

- [X] T001 Create solution and projects (dotnet new sln, webapi, xunit) per quickstart.md Step 1.1
- [X] T002 Create nuget.config in repository root with GitHub Packages configuration per quickstart.md Step 1.2
- [X] T003 [P] Install core API packages in ReceiptService.Api/ReceiptService.Api.csproj (ServiceDefaults, EF Core, MassTransit, OpenAPI, Scalar) per quickstart.md Step 1.3
- [X] T004 [P] Install test packages in ReceiptService.Tests/ReceiptService.Tests.csproj (xUnit, Moq, Testcontainers) per quickstart.md Step 1.3
- [X] T005 [P] Create .gitignore in repository root (bin/, obj/, artifacts/, .vs/, etc.) per Constitution IX
- [X] T006 [P] Create .dockerignore in repository root (specs/, tests/, IDE files, build artifacts) per Constitution IX
- [X] T007 [P] Configure project to treat warnings as errors in ReceiptService.Api/ReceiptService.Api.csproj per Constitution VIII

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T008 Create ReceiptDbContext in ReceiptService.Api/Data/ReceiptDbContext.cs per quickstart.md Step 2.1 and data-model.md
- [X] T009 [P] Create Receipt entity in ReceiptService.Api/Models/Entities/Receipt.cs per data-model.md
- [X] T010 [P] Create ReceiptLineItem entity in ReceiptService.Api/Models/Entities/ReceiptLineItem.cs per data-model.md
- [X] T011 [P] Create ReceiptAuditEvent entity in ReceiptService.Api/Models/Entities/ReceiptAuditEvent.cs per data-model.md
- [X] T012 [P] Create InvoiceBalanceTracker entity in ReceiptService.Api/Models/Entities/InvoiceBalanceTracker.cs per data-model.md
- [X] T013 Configure entity relationships and indexes in ReceiptDbContext.OnModelCreating() per data-model.md
- [X] T014 Create initial EF Core migration (dotnet ef migrations add InitialCreate) per quickstart.md Step 2.2
- [X] T015 Implement Program.cs with ServiceDefaults, DbContext, Redis, MassTransit, JWT auth per quickstart.md Step 3 and Maliev guidelines
- [X] T016 [P] Create ExceptionHandlingMiddleware in ReceiptService.Api/Middleware/ExceptionHandlingMiddleware.cs per research.md Decision 10
- [X] T017 [P] Create domain exceptions (DuplicateReceiptException, InvoiceNotFoundException, etc.) in ReceiptService.Api/Exceptions/ per research.md Decision 10
- [X] T018 [P] Create ReceiptStatus enum in ReceiptService.Api/Models/Enums/ReceiptStatus.cs (Active, Void, PendingPdf)
- [X] T019 [P] Create AuditEventType enum in ReceiptService.Api/Models/Enums/AuditEventType.cs (Created, Voided, PdfGenerated)
- [X] T020 Create TestWebApplicationFactory in ReceiptService.Tests/TestWebApplicationFactory.cs with Testcontainers and dynamic JWT keys per quickstart.md Step 5.1
- [X] T021 [P] Create MockHttpMessageHandler in ReceiptService.Tests/Helpers/MockHttpMessageHandler.cs per Maliev guidelines Section 5
- [X] T022 [P] Create test database fixture in ReceiptService.Tests/Fixtures/TestDatabaseFixture.cs with Testcontainers.PostgreSql
- [X] T023 [P] Create test RabbitMQ fixture in ReceiptService.Tests/Fixtures/TestRabbitMqFixture.cs with Testcontainers.RabbitMQ
- [X] T024 [P] Create test Redis fixture in ReceiptService.Tests/Fixtures/TestRedisFixture.cs with Testcontainers.Redis
- [X] T025 Create InvoiceDto in ReceiptService.Api/Models/Dtos/InvoiceDto.cs (received from Invoice Service)
- [X] T026 [P] Create IInvoiceServiceClient interface in ReceiptService.Api/Services/IInvoiceServiceClient.cs
- [X] T027 [P] Implement InvoiceServiceClient in ReceiptService.Api/Services/InvoiceServiceClient.cs with 5s timeout, 2 retries, exp backoff per research.md Decision 3
- [X] T028 Register InvoiceServiceClient with Polly resilience handler in Program.cs per research.md Decision 3

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 + 6 - Full Payment Receipt Issuance with Tax Compliance (Priority: P1) 🎯 MVP

**Combined Goal**: Generate tax-compliant receipts for fully paid invoices, validating all government-required fields before creation and publishing PDF generation events.

**Why Combined**: US6 (Tax Compliance) is a prerequisite for US1 (Receipt Issuance) - they must be implemented together for a valid MVP.

**Independent Test**: Create a paid invoice with all tax fields, generate a receipt, verify record exists with correct data, tax validation passed, PDF event published. Attempt creation with missing tax fields and verify rejection.

### Tests for User Stories 1 & 6

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T029 [P] [US1] Contract test for POST /v1/receipts in ReceiptService.Tests/Contract/CreateReceiptContractTests.cs per contracts/receipts-api.yaml
- [X] T030 [P] [US1] Contract test for GET /v1/receipts/{id} in ReceiptService.Tests/Contract/GetReceiptContractTests.cs per contracts/receipts-api.yaml
- [X] T031 [P] [US6] Unit test for TaxValidator in ReceiptService.Tests/Unit/TaxValidatorTests.cs per research.md Decision 7
- [X] T032 [P] [US1] Unit test for ReceiptNumberGenerator sequential logic in ReceiptService.Tests/Unit/ReceiptNumberGeneratorTests.cs per research.md Decision 1
- [X] T033 [US1] Integration test for full payment receipt creation in ReceiptService.Tests/Integration/ReceiptCreationTests.cs per quickstart.md Step 5.2
- [X] T034 [P] [US6] Integration test for tax validation rejection in ReceiptService.Tests/Integration/TaxValidationTests.cs
- [X] T035 [P] [US1] Integration test for duplicate prevention in ReceiptService.Tests/Integration/DuplicatePreventionTests.cs
- [X] T036 [P] [US1] MassTransit contract test for PdfGenerationRequestedEvent in ReceiptService.Tests/Contract/PdfGenerationEventTests.cs per contracts/message-contracts.md

### Implementation for User Stories 1 & 6

- [X] T037 [P] [US1] [US6] Create CreateReceiptRequest DTO in ReceiptService.Api/Models/Requests/CreateReceiptRequest.cs with Data Annotations per contracts/receipts-api.yaml
- [X] T038 [P] [US1] Create ReceiptResponse DTO in ReceiptService.Api/Models/Responses/ReceiptResponse.cs per contracts/receipts-api.yaml
- [X] T039 [P] [US1] Create ReceiptLineItemResponse DTO in ReceiptService.Api/Models/Responses/ReceiptLineItemResponse.cs
- [X] T040 [P] [US1] Create ReceiptMappingExtensions in ReceiptService.Api/Extensions/ReceiptMappingExtensions.cs (ToResponse(), ToEntity() methods) per research.md and Maliev guidelines
- [X] T041 [P] [US6] Create ITaxValidator interface in ReceiptService.Api/Services/ITaxValidator.cs per research.md Decision 7
- [X] T042 [P] [US6] Implement ThailandTaxValidator in ReceiptService.Api/Services/ThailandTaxValidator.cs with tax ID, VAT rate, withholding tax validation per research.md Decision 7
- [X] T043 [P] [US1] Create IReceiptNumberGenerator interface in ReceiptService.Api/Services/IReceiptNumberGenerator.cs
- [X] T044 [P] [US1] Implement ReceiptNumberGenerator in ReceiptService.Api/Services/ReceiptNumberGenerator.cs with sequential ENTITY-YYYY-NNNNNN logic per research.md Decision 1
- [X] T045 [P] [US1] Create PdfGenerationRequestedEvent in ReceiptService.Api/Events/PdfGenerationRequestedEvent.cs per contracts/message-contracts.md
- [X] T046 [P] [US1] Create IReceiptService interface in ReceiptService.Api/Services/IReceiptService.cs
- [X] T047 [US1] [US6] Implement ReceiptService.CreateReceiptAsync() in ReceiptService.Api/Services/ReceiptService.cs with invoice retrieval, tax validation, balance tracking, numbering, audit event creation, PDF event publishing per quickstart.md Step 4.1 and research.md
- [X] T048 [US1] Implement POST /v1/receipts in ReceiptService.Api/Controllers/ReceiptsController.cs per contracts/receipts-api.yaml
- [X] T049 [P] [US1] Implement GET /v1/receipts/{id} in ReceiptsController per contracts/receipts-api.yaml
- [X] T050 [US1] Add correlation ID propagation and structured logging to ReceiptService per research.md Decision 9 (FR-030)
- [X] T051 [P] [US1] Add OpenTelemetry metrics (receipts.created counter, creation_duration histogram) per research.md Decision 9 (FR-031)
- [X] T052 [US1] Register all services in Program.cs (IReceiptService, ITaxValidator, IReceiptNumberGenerator)

**Checkpoint**: At this point, tax-compliant full-payment receipts can be created with PDF generation events. This is the MVP!

---

## Phase 4: User Story 2 - Partial Payment Receipt Handling (Priority: P2)

**Goal**: Support partial payment receipts with accurate balance tracking and prevention of over-receipting.

**Independent Test**: Create invoice, generate multiple partial receipts, verify balance tracking prevents over-receipting and correctly calculates remaining amounts.

### Tests for User Story 2

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T053 [P] [US2] Integration test for partial payment receipt creation in ReceiptService.Tests/Integration/PartialPaymentTests.cs per spec.md acceptance scenarios
- [X] T054 [P] [US2] Integration test for multiple partial receipts on same invoice in PartialPaymentTests.cs
- [X] T055 [P] [US2] Integration test for over-receipting prevention in PartialPaymentTests.cs
- [X] T056 [P] [US2] Contract test for GET /v1/receipts query with invoice ID filter in ReceiptService.Tests/Contract/QueryReceiptsContractTests.cs

### Implementation for User Story 2

- [X] T057 [P] [US2] Implement GetOrCreateBalanceTrackerAsync() in ReceiptService per research.md Decision 4
- [X] T058 [US2] Extend ReceiptService.CreateReceiptAsync() to support partial amounts with balance tracking and optimistic locking per research.md Decision 4
- [X] T059 [P] [US2] Implement GET /v1/receipts query endpoint in ReceiptsController with invoice ID, status, date range filters per contracts/receipts-api.yaml
- [X] T060 [P] [US2] Add DbUpdateConcurrencyException handling to ReceiptService for balance conflicts per research.md Decision 4
- [X] T061 [US2] Add structured logging for partial payment operations with balance tracking details

**Checkpoint**: At this point, User Stories 1, 2, and 6 work independently. Partial payments are fully supported.

---

## Phase 5: User Story 3 - Receipt Void and Correction (Priority: P2)

**Goal**: Void receipts with complete audit trail and balance restoration for compliance.

**Independent Test**: Create receipt, void it, verify status changed, audit trail created, balance restored, cannot void again.

### Tests for User Story 3

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T062 [P] [US3] Contract test for POST /v1/receipts/{id}/void in ReceiptService.Tests/Contract/VoidReceiptContractTests.cs per contracts/receipts-api.yaml
- [X] T063 [P] [US3] Integration test for void operation in ReceiptService.Tests/Integration/ReceiptVoidTests.cs per spec.md acceptance scenarios
- [X] T064 [P] [US3] Integration test for double-void prevention in ReceiptVoidTests.cs
- [X] T065 [P] [US3] Integration test for void+recreate correction workflow in ReceiptVoidTests.cs
- [X] T066 [P] [US3] Contract test for GET /v1/receipts/{id}/audit-history in ReceiptService.Tests/Contract/AuditHistoryContractTests.cs

### Implementation for User Story 3

- [X] T067 [P] [US3] Create VoidReceiptRequest DTO in ReceiptService.Api/Models/Requests/VoidReceiptRequest.cs per contracts/receipts-api.yaml
- [X] T068 [P] [US3] Create AuditEvent DTO in ReceiptService.Api/Models/Responses/AuditEvent.cs for audit history responses
- [X] T069 [US3] Implement Receipt.Void() method in Receipt entity with immutability enforcement per data-model.md and research.md Decision 2
- [X] T070 [US3] Implement ReceiptService.VoidReceiptAsync() with balance restoration and audit event creation per spec.md
- [X] T071 [P] [US3] Implement POST /v1/receipts/{id}/void in ReceiptsController per contracts/receipts-api.yaml
- [X] T072 [P] [US3] Implement GET /v1/receipts/{id}/audit-history in ReceiptsController per contracts/receipts-api.yaml
- [X] T073 [US3] Add structured logging for void operations with reason and staff attribution

**Checkpoint**: User Stories 1-3 and 6 all work independently. Void and correction workflows complete.

---

## Phase 6: User Story 4 - Split Invoice Receipt Generation (Priority: P3)

**Goal**: Support receipts for split invoice segments with segment-specific tax rates and amounts.

**Independent Test**: Create split invoice with segments, generate receipts for each segment, verify alignment with split structure and segment-specific attributes.

### Tests for User Story 4

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T074 [P] [US4] Integration test for split invoice receipt generation in ReceiptService.Tests/Integration/SplitInvoiceReceiptTests.cs per spec.md acceptance scenarios
- [X] T075 [P] [US4] Integration test for segment-specific tax rate application in SplitInvoiceReceiptTests.cs
- [X] T076 [P] [US4] Integration test for split invoice receipt status query in SplitInvoiceReceiptTests.cs

### Implementation for User Story 4

- [X] T077 [P] [US4] Extend CreateReceiptRequest with optional InvoiceSegmentId field
- [X] T078 [P] [US4] Extend Receipt entity with nullable InvoiceSegmentId field and migration
- [X] T079 [P] [US4] Extend InvoiceDto to include Segments collection with segment-specific attributes
- [X] T080 [US4] Extend ReceiptService.CreateReceiptAsync() to support segment-specific receipt creation with segment tax rate and amount validation
- [X] T081 [P] [US4] Extend balance tracker logic to track segment-level balances
- [X] T082 [P] [US4] Extend GET /v1/receipts query to filter by segment ID and show segment receipting status
- [X] T083 [US4] Add structured logging for split invoice receipt operations with segment details

**Checkpoint**: User Stories 1-4 and 6 all work independently. Split invoice support complete.

---

## Phase 7: User Story 5 - Receipt Query and Analytics Access (Priority: P3)

**Goal**: Provide efficient analytics APIs with caching for business intelligence without impacting operational performance.

**Independent Test**: Create diverse receipts, query analytics endpoints, verify metrics accuracy and <2s response time for 100k records.

### Tests for User Story 5

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T084 [P] [US5] Contract test for GET /v1/analytics/payment-completion in ReceiptService.Tests/Contract/AnalyticsContractTests.cs per contracts/analytics-api.yaml
- [ ] T085 [P] [US5] Contract test for GET /v1/analytics/outstanding-receivables in AnalyticsContractTests.cs
- [ ] T086 [P] [US5] Contract test for GET /v1/analytics/customer-payment-behavior in AnalyticsContractTests.cs
- [ ] T087 [P] [US5] Contract test for GET /v1/analytics/processing-metrics in AnalyticsContractTests.cs
- [ ] T088 [P] [US5] Integration test for payment completion rate calculation in ReceiptService.Tests/Integration/AnalyticsTests.cs
- [ ] T089 [P] [US5] Integration test for Redis caching with 5-minute TTL in AnalyticsTests.cs
- [ ] T090 [P] [US5] Performance test for <2s response time with 100k receipts in AnalyticsTests.cs

### Implementation for User Story 5

- [X] T091 [P] [US5] Create PaymentCompletionResponse DTO in ReceiptService.Api/Models/Responses/PaymentCompletionResponse.cs per contracts/analytics-api.yaml
- [X] T092 [P] [US5] Create OutstandingReceivablesResponse DTO in ReceiptService.Api/Models/Responses/OutstandingReceivablesResponse.cs
- [X] T093 [P] [US5] Create PaymentBehaviorResponse DTO in ReceiptService.Api/Models/Responses/PaymentBehaviorResponse.cs
- [X] T094 [P] [US5] Create ProcessingMetricsResponse DTO in ReceiptService.Api/Models/Responses/ProcessingMetricsResponse.cs
- [X] T095 [P] [US5] Create IAnalyticsService interface in ReceiptService.Api/Services/IAnalyticsService.cs
- [X] T096 [US5] Implement AnalyticsService with Redis caching (5-min TTL) in ReceiptService.Api/Services/AnalyticsService.cs per research.md Decision 8
- [X] T097 [P] [US5] Implement GET /v1/analytics/payment-completion in ReceiptService.Api/Controllers/AnalyticsController.cs per contracts/analytics-api.yaml
- [X] T098 [P] [US5] Implement GET /v1/analytics/outstanding-receivables in AnalyticsController per contracts/analytics-api.yaml
- [X] T099 [P] [US5] Implement GET /v1/analytics/customer-payment-behavior in AnalyticsController per contracts/analytics-api.yaml
- [X] T100 [P] [US5] Implement GET /v1/analytics/processing-metrics in AnalyticsController per contracts/analytics-api.yaml
- [X] T101 [US5] Register IAnalyticsService in Program.cs and configure Redis caching
- [X] T102 [US5] Add OpenTelemetry metrics for analytics query performance (query_duration histogram) per FR-031

**Checkpoint**: All user stories (1-6) work independently. Full analytics capabilities available.

---

## Phase 8: Consumed Events - PDF Generation Callback

**Goal**: Handle PDF generation completion events from PDF Service to update receipt status.

**Independent Test**: Publish PdfGeneratedEvent, verify receipt status changes from PendingPdf to Active and PDF reference is stored.

### Tests for PDF Callback

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T103 [P] Integration test for PdfGeneratedEvent consumption in ReceiptService.Tests/Integration/PdfCallbackTests.cs
- [ ] T104 [P] Integration test for PDF reference update in PdfCallbackTests.cs

### Implementation for PDF Callback

- [X] T105 [P] Create PdfGeneratedEvent consumer DTO in ReceiptService.Api/Events/PdfGeneratedEvent.cs per contracts/message-contracts.md
- [X] T106 [P] Create PdfGeneratedEventConsumer in ReceiptService.Api/Consumers/PdfGeneratedEventConsumer.cs per contracts/message-contracts.md
- [X] T107 Configure MassTransit consumer in Program.cs with receipt-pdf-generated queue per contracts/message-contracts.md
- [X] T108 Add structured logging for PDF callback processing with correlation ID

**Checkpoint**: PDF generation lifecycle complete (create receipt → publish event → consume callback → update status).

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Production readiness improvements across all user stories

- [X] T109 [P] Create Dockerfile with multi-stage build, BuildKit secrets, app user, health check per Constitution X and Maliev guidelines Section 3
- [X] T110 [P] Create appsettings.json with connection strings (ReceiptDbContext, redis, rabbitmq) per Maliev guidelines Section 4
- [X] T111 [P] Create appsettings.Development.json with local development settings
- [X] T112 [P] Implement health checks for DbContext in Program.cs per Maliev guidelines (avoid duplicate Redis/Postgres checks)
- [X] T113 [P] Configure OpenAPI documentation with Scalar in Program.cs per Constitution II and Maliev guidelines
- [X] T114 [P] Add CORS configuration in Program.cs per Maliev guidelines
- [X] T115 [P] Add rate limiting configuration in Program.cs
- [X] T116 [P] Verify all structured logs include correlation IDs per FR-030
- [X] T117 [P] Verify all business metrics are tagged with service_name, version, region, environment per FR-031
- [X] T118 [P] Add README.md in repository root with quickstart instructions
- [X] T119 [P] Run `dotnet build --warnaserror` to verify zero warnings per Constitution VIII
- [ ] T120 [P] Run all tests and verify 80%+ coverage for business logic per Constitution III
- [ ] T121 [P] Validate quickstart.md can be executed end-to-end
- [ ] T122 [P] Performance test for 50 concurrent receipt creation requests in ReceiptService.Tests/Integration/ConcurrencyTests.cs per SC-009
- [ ] T123 [P] Integration test for Invoice Service timeout handling in ReceiptService.Tests/Integration/InvoiceServiceTimeoutTests.cs using MockHttpMessageHandler per SC-010
- [ ] T124 [P] Create GitHub Actions workflow for CI/CD with BuildKit secrets for NuGet per Constitution XIII
- [ ] T125 Final code review for constitution compliance (no AutoMapper, FluentValidation, FluentAssertions) per Constitution XIV

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1+6 (Phase 3)**: Depends on Foundational - Combined MVP (US6 is prerequisite for US1)
- **User Story 2 (Phase 4)**: Depends on Foundational - Can start after Phase 2 (independent of US1, but benefits from US1 completion)
- **User Story 3 (Phase 5)**: Depends on Foundational - Can start after Phase 2 (independent of US1/US2)
- **User Story 4 (Phase 6)**: Depends on Foundational and US1/US2 - Builds on partial payment logic
- **User Story 5 (Phase 7)**: Depends on Foundational and US1/US2/US3 - Requires receipt data to query
- **PDF Callback (Phase 8)**: Depends on US1 completion (receipt creation must work first)
- **Polish (Phase 9)**: Depends on desired user stories being complete

### User Story Dependencies

**Independent Stories** (can work in parallel after Foundational):
- **US1+US6** (MVP): Foundation only
- **US2**: Foundation only (but conceptually builds on US1)
- **US3**: Foundation only (but conceptually builds on US1)

**Dependent Stories**:
- **US4**: Requires US1 and US2 (split invoices build on partial payment logic)
- **US5**: Requires US1, US2, US3 (analytics need receipt data to query)

### Recommended Sequence (Priority-Based)

1. **Phase 1**: Setup
2. **Phase 2**: Foundational (CRITICAL - blocks everything)
3. **Phase 3**: US1+US6 (P1 MVP - tax-compliant full payment receipts)
4. **Phase 4**: US2 (P2 - partial payments)
5. **Phase 5**: US3 (P2 - void and correction)
6. **Phase 6**: US4 (P3 - split invoices)
7. **Phase 7**: US5 (P3 - analytics)
8. **Phase 8**: PDF Callback
9. **Phase 9**: Polish

### Parallel Opportunities

**Setup Phase (all tasks marked [P])**:
- T003 (API packages), T004 (test packages), T005 (gitignore), T006 (dockerignore), T007 (warnings config)

**Foundational Phase**:
- Entities: T009, T010, T011, T012 (all entities in parallel)
- Middleware/Exceptions: T016, T017 (parallel)
- Enums: T018, T019 (parallel)
- Test fixtures: T022, T023, T024 (parallel)
- Service clients: T025, T026, T027 (InvoiceDto and client in parallel after interface)

**User Story 1+6**:
- Tests: T029, T030, T031, T032, T034, T035, T036 (all tests in parallel)
- DTOs: T037, T038, T039, T040 (all DTOs and mappings in parallel)
- Services (after interfaces): T042 (TaxValidator), T044 (NumberGenerator), T045 (Event) in parallel
- Endpoints: T048, T049 (parallel)
- Observability: T050, T051 (parallel)

**User Story 2**:
- Tests: T053, T054, T055, T056 (all tests in parallel)
- Implementation: T057, T059, T060 (balance tracker, query endpoint, exception handling in parallel)

**User Story 3**:
- Tests: T062-T066 (all tests in parallel)
- DTOs: T067, T068 (parallel)
- Endpoints: T071, T072 (parallel)

**User Story 4**:
- Tests: T074-T076 (all tests in parallel)
- Extensions: T077, T078, T079, T081, T082 (all extensions in parallel)

**User Story 5**:
- Tests: T084-T090 (all tests in parallel)
- DTOs: T091-T094 (all DTOs in parallel)
- Endpoints: T097-T100 (all analytics endpoints in parallel)

**PDF Callback**:
- Tests: T103, T104 (parallel)
- Events: T105, T106 (parallel)

**Polish Phase**:
- Almost all tasks (T109-T123) can run in parallel except T119-T121 which need implementation complete

---

## Parallel Example: User Story 1+6 (MVP)

```bash
# After Foundational phase completes, launch all US1+US6 tests in parallel:
Task T029: "Contract test for POST /v1/receipts"
Task T030: "Contract test for GET /v1/receipts/{id}"
Task T031: "Unit test for TaxValidator"
Task T032: "Unit test for ReceiptNumberGenerator"
Task T034: "Integration test for tax validation rejection"
Task T035: "Integration test for duplicate prevention"
Task T036: "MassTransit contract test for PdfGenerationRequestedEvent"

# Then launch all DTOs/mappings in parallel:
Task T037: "Create CreateReceiptRequest DTO"
Task T038: "Create ReceiptResponse DTO"
Task T039: "Create ReceiptLineItemResponse DTO"
Task T040: "Create ReceiptMappingExtensions"

# Then launch service implementations in parallel (after interfaces):
Task T042: "Implement ThailandTaxValidator"
Task T044: "Implement ReceiptNumberGenerator"
Task T045: "Create PdfGenerationRequestedEvent"

# Then core service (sequential after above):
Task T047: "Implement ReceiptService.CreateReceiptAsync()"

# Then endpoints in parallel:
Task T048: "Implement POST /v1/receipts"
Task T049: "Implement GET /v1/receipts/{id}"

# Then observability in parallel:
Task T050: "Add correlation ID and logging"
Task T051: "Add OpenTelemetry metrics"
```

---

## Implementation Strategy

### MVP First (User Stories 1+6 Only)

**Goal**: Deliverable tax-compliant full-payment receipt issuance

1. ✅ Complete Phase 1: Setup (T001-T007)
2. ✅ Complete Phase 2: Foundational (T008-T028) - CRITICAL
3. ✅ Complete Phase 3: US1+US6 (T029-T052)
4. **STOP and VALIDATE**:
   - Run all US1+US6 tests
   - Manually test receipt creation via Scalar UI
   - Verify PDF event published to RabbitMQ
   - Verify tax validation rejection
   - Check audit trail created
5. Deploy/demo MVP

**What MVP Delivers**:
- Create tax-compliant receipts for full invoice amounts
- Validate all government-required tax fields
- Generate sequential receipt numbers
- Publish PDF generation events
- Prevent duplicate receipts
- Complete audit trail
- Structured logging and metrics

### Incremental Delivery

**Phase Releases**:

1. **Release 1 (MVP)**: Setup + Foundational + US1+US6
   - Tax-compliant full payment receipts
   - Test independently → Deploy

2. **Release 2**: Add US2 (Partial Payments)
   - All Release 1 features +
   - Partial payment support
   - Balance tracking
   - Test independently → Deploy

3. **Release 3**: Add US3 (Void & Correction)
   - All Release 1-2 features +
   - Void receipts with audit trail
   - Balance restoration
   - Test independently → Deploy

4. **Release 4**: Add US4 (Split Invoices)
   - All Release 1-3 features +
   - Split invoice receipt generation
   - Segment-specific tax rates
   - Test independently → Deploy

5. **Release 5**: Add US5 (Analytics)
   - All Release 1-4 features +
   - Payment completion metrics
   - Outstanding receivables
   - Customer behavior analytics
   - Test independently → Deploy

6. **Release 6**: Add PDF Callback + Polish
   - Full production-ready system
   - PDF lifecycle complete
   - Docker, CI/CD, documentation
   - Final testing → Production Deploy

Each release adds value without breaking previous features!

### Parallel Team Strategy

With **3 developers** after Foundational phase complete:

**Week 1-2**:
- Developer A: User Story 1+6 (MVP) - Priority 1
- Developer B: User Story 2 (Partial Payments) - Priority 2
- Developer C: User Story 3 (Void & Correction) - Priority 2

**Week 3**:
- Developer A: User Story 4 (Split Invoices) - depends on US1/US2
- Developer B: User Story 5 (Analytics) - depends on US1/US2/US3
- Developer C: PDF Callback

**Week 4**:
- All developers: Polish, testing, documentation, deployment

Stories complete and integrate independently.

---

## Task Count Summary

- **Setup (Phase 1)**: 7 tasks
- **Foundational (Phase 2)**: 21 tasks
- **User Story 1+6 (Phase 3)**: 24 tasks (8 tests + 16 implementation)
- **User Story 2 (Phase 4)**: 9 tasks (4 tests + 5 implementation)
- **User Story 3 (Phase 5)**: 12 tasks (5 tests + 7 implementation)
- **User Story 4 (Phase 6)**: 10 tasks (3 tests + 7 implementation)
- **User Story 5 (Phase 7)**: 19 tasks (7 tests + 12 implementation)
- **PDF Callback (Phase 8)**: 6 tasks (2 tests + 4 implementation)
- **Polish (Phase 9)**: 17 tasks (2 tests + 15 validation/infrastructure)

**Total**: 125 tasks

**Test Tasks**: 31 (24.8% of total)
**Parallel Tasks**: 73 (58.4% marked [P])

---

## Notes

- **[P] tasks**: Different files, no dependencies - can run in parallel
- **[Story] labels**: Map tasks to specific user stories for traceability
- **Each user story independently completable and testable**
- **Test-First Development (Constitution III)**: Tests MUST be written first and FAIL before implementation
- **Zero Warnings (Constitution VIII)**: Enforced via T007 and validated in T119
- **Real Infrastructure Testing (Constitution IV)**: All integration tests use Testcontainers (PostgreSQL, RabbitMQ, Redis)
- **No Banned Libraries (Constitution XIV)**: No AutoMapper, FluentValidation, or FluentAssertions (validated in T125)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
