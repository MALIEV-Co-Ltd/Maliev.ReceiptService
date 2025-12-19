# Implementation Plan: Receipt Service Core

**Branch**: `001-receipt-service-core` | **Date**: 2025-12-05 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-receipt-service-core/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

The Receipt Service is the authoritative source for managing customer-facing receipts within the MALIEV microservice ecosystem. It generates receipts strictly from existing invoices retrieved from the Invoice Service, validates eligibility and tax compliance, handles full and partial payment scenarios, prevents duplicate receipts, publishes PDF generation events to RabbitMQ, and maintains a complete immutable audit trail for financial compliance. The service supports advanced payment structuring including split invoices, tracks receiptable balances, and exposes analytics APIs for business intelligence while maintaining strict data immutability after creation (corrections via void+recreate only).

## Technical Context

**Language/Version**: C# / .NET 10.0
**Framework**: ASP.NET Core Web API (`Microsoft.NET.Sdk.Web`)
**Primary Dependencies**:
- `Maliev.Aspire.ServiceDefaults` (from GitHub Packages) - Observability, resilience, health checks
- `Npgsql.EntityFrameworkCore.PostgreSQL` - Database ORM
- `MassTransit.RabbitMQ` - Message bus for PDF generation events
- `Microsoft.AspNetCore.OpenApi` / `Scalar.AspNetCore` - API documentation
- `AspNetCore.HealthChecks.UI.Client` - Health check endpoints

**Storage**:
- PostgreSQL - Primary data store (receipts, audit events, balance trackers)
- Redis - Distributed caching for analytics queries

**Messaging**: RabbitMQ - Event publishing to PDF Service with routing key pattern `maliev.receipt.v1.{entity}.{action}`

**External Service Dependencies**:
- **Invoice Service** (HTTP) - Source of invoice data for receipt generation (FR-001, FR-002)
- **PDF Service** (RabbitMQ consumer) - Receives PDF generation events (FR-011)
- **Upload Service** (indirect) - Stores generated PDFs, referenced by PDF Service (FR-013)
- **User Service** (HTTP) - JWT authentication and staff identity resolution (FR-023)
- **Financial/Accounting Service** (out-of-scope) - Manages supplier receipts issued TO MALIEV; Receipt Service only handles customer receipts issued BY MALIEV (FR-024)

**Testing**:
- xUnit - Test framework
- Moq - Mocking framework
- Testcontainers.PostgreSql - Real PostgreSQL containers for integration tests
- Testcontainers.RabbitMQ - Real RabbitMQ containers for message tests
- Testcontainers.Redis - Real Redis containers for cache tests

**Target Platform**: Linux containers (Docker) deployed via Kubernetes

**Project Type**: Microservice (ASP.NET Core Web API)

**Performance Goals** (from Success Criteria):
- Receipt creation: <5 seconds (SC-001)
- PDF event publishing: <1 second (SC-004)
- Void operations: <3 seconds (SC-006)
- Analytics queries: <2 seconds for 100k records (SC-007)
- Concurrent load: 50 simultaneous requests (SC-009)

**Constraints**:
- Data immutability: Receipts immutable after creation (FR-010b)
- Sequential numbering: No gaps allowed in receipt numbers (FR-010a)
- Invoice Service timeout: 5s per attempt, 2 retries with exponential backoff (FR-025)
- Audit retention: 7 years minimum (FR-016a)
- Data integrity: 99.99% (SC-008)

**Scale/Scope**:
- Analytics dataset: Up to 100,000 receipts per query
- Receipt number format: ENTITY-YYYY-NNNNNN (e.g., MALIEV-2025-000001)
- Observability: Structured JSON logs, distributed tracing with correlation IDs, business metrics

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence/Notes |
|-----------|--------|----------------|
| **I. Service Autonomy** | ✅ PASS | Own PostgreSQL database (`ReceiptDbContext`), own domain logic, interacts with Invoice/PDF/Upload/User services only via HTTP APIs and RabbitMQ events. No direct database access to other services. |
| **II. Explicit Contracts** | ✅ PASS | Will document APIs via OpenAPI/Scalar. REST API versioning (v1). Backward compatibility enforced. MassTransit message contracts versioned with routing keys `maliev.receipt.v1.*`. |
| **III. Test-First Development** | ✅ PASS | Plan includes test structure before implementation. xUnit tests for all user stories. Will follow Red-Green-Refactor. Target 80%+ coverage for receipt creation, void, validation, balance tracking logic. |
| **IV. Real Infrastructure Testing** | ✅ PASS | Testcontainers for PostgreSQL, RabbitMQ, and Redis. No in-memory providers. Integration tests will use real containers matching production. |
| **V. Auditability & Observability** | ✅ PASS | Structured JSON logs with correlation IDs (FR-030). Immutable audit trail for 7 years (FR-016, FR-016a). Health checks for liveness/readiness via `MapDefaultEndpoints`. Distributed tracing across services (FR-032). |
| **VI. Security & Compliance** | ✅ PASS | JWT authentication via `AddJwtAuthentication()`. Tax compliance validation (FR-009). 7-year audit retention for financial compliance (FR-016a). Sensitive data (customer details, financial data) encrypted in transit (HTTPS required). |
| **VII. Secrets Management** | ✅ PASS | Google Secret Manager via `AddGoogleSecretManagerVolume()`. No secrets in code. nuget.config with placeholder credentials. |
| **VIII. Zero Warnings Policy** | ✅ PASS | Build configuration will treat warnings as errors. |
| **IX. Clean Project Artifacts** | ✅ PASS | `.gitignore` for bin/obj/artifacts. `.dockerignore` for specs, tests, IDE files. |
| **X. Docker Best Practices** | ✅ PASS | Multi-stage build with SDK/runtime split. Built-in `app` user. BuildKit secrets for NuGet. Health check on `/receipt/liveness`. Port 8080. |
| **XI. Simplicity & Maintainability** | ✅ PASS | YAGNI applied. No AutoMapper/FluentValidation. Extension methods for mapping. Direct EF Core queries (no repository pattern unless justified). |
| **XII. Business Metrics & Analytics** | ✅ PASS | FR-031 specifies business metrics: receipt creation rate, void rate, error rates, processing duration, Invoice Service latency. Analytics APIs for payment completion rates, outstanding receivables (FR-021). Metrics tagged with service_name, version, region, environment. |
| **XIII. .NET Aspire Integration** | ✅ PASS | ServiceDefaults as NuGet package from GitHub Packages. nuget.config with credential placeholders. `AddServiceDefaults()` and `MapDefaultEndpoints(servicePrefix: "receipt")` in Program.cs. Docker uses BuildKit secrets for NuGet auth. |
| **XIV. Code Quality & Library Standards** | ✅ PASS | NO AutoMapper (using extension methods for mapping). NO FluentValidation (using Data Annotations). NO FluentAssertions (using xUnit Assert.*). |

**Gate Result**: ✅ ALL GATES PASS - Proceed to Phase 0

### Post-Phase 1 Re-evaluation (2025-12-05)

After completing Phase 1 (Data Model & Contracts), all constitutional principles remain satisfied:

| Principle | Status | Change from Initial |
|-----------|--------|---------------------|
| **I-XIV** | ✅ PASS | No changes - All design decisions align with constitution |

**Specific Validations**:
- Data model uses EF Core directly (no repository pattern) - aligns with Principle XI (Simplicity)
- Mapping uses extension methods (no AutoMapper) - complies with Principle XIV
- Validation uses Data Annotations (no FluentValidation) - complies with Principle XIV
- Test structure uses real infrastructure via Testcontainers - complies with Principle IV
- OpenAPI contracts use Scalar (not Swagger) - complies with Principle II

**Gate Result**: ✅ ALL GATES STILL PASS - Ready for Phase 2 (Task Generation)

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
Maliev.ReceiptService/
├── nuget.config                           # GitHub Packages configuration
├── .gitignore                             # Exclude bin/obj/artifacts
├── .dockerignore                          # Exclude specs/tests/IDE files
├── Dockerfile                             # Multi-stage build with BuildKit secrets
├── Maliev.ReceiptService.sln             # Solution file
├──ReceiptService.Api/              # Main API project
│   ├── ReceiptService.Api.csproj
│   ├── Program.cs                         # ServiceDefaults, DbContext, MassTransit, Controllers
│   ├── appsettings.json                   # Connection strings (ServiceDbContext, redis, rabbitmq)
│   ├── Controllers/
│   │   ├── ReceiptsController.cs          # POST /v1/receipts, GET /v1/receipts/{id}, POST /v1/receipts/{id}/void
│   │   └── AnalyticsController.cs         # GET /v1/analytics/payment-completion, /outstanding-receivables
│   ├── Models/
│   │   ├── Entities/
│   │   │   ├── Receipt.cs                 # Entity: Receipt number, invoice ref, amounts, status
│   │   │   ├── ReceiptLineItem.cs         # Entity: Line items
│   │   │   ├── ReceiptAuditEvent.cs       # Entity: Audit trail (immutable, 7-year retention)
│   │   │   └── InvoiceBalanceTracker.cs   # Entity: Balance tracking with optimistic locking
│   │   ├── Requests/
│   │   │   ├── CreateReceiptRequest.cs    # DTO: InvoiceId, Amount, PaymentMethod (Data Annotations)
│   │   │   └── VoidReceiptRequest.cs      # DTO: Reason
│   │   └── Responses/
│   │       ├── ReceiptResponse.cs         # DTO: Receipt details
│   │       └── AnalyticsResponse.cs       # DTO: Metrics
│   ├── Services/
│   │   ├── IReceiptService.cs
│   │   ├── ReceiptService.cs              # Business logic: creation, void, validation
│   │   ├── IInvoiceServiceClient.cs
│   │   ├── InvoiceServiceClient.cs        # HTTP client with 5s timeout, 2 retries, exp backoff
│   │   ├── IReceiptNumberGenerator.cs
│   │   ├── ReceiptNumberGenerator.cs      # Sequential ENTITY-YYYY-NNNNNN with gap prevention
│   │   └── ITaxValidator.cs
│   │       └── TaxValidator.cs            # Validates government-required fields
│   ├── Data/
│   │   ├── ReceiptDbContext.cs            # EF Core DbContext
│   │   └── Migrations/                    # EF Core migrations
│   ├── Events/
│   │   ├── PdfGenerationRequestedEvent.cs # MassTransit message contract
│   │   └── ReceiptCreatedEvent.cs         # Internal domain event
│   ├── Extensions/
│   │   ├── ReceiptMappingExtensions.cs    # ToResponse(), ToEntity() methods (NO AutoMapper)
│   │   └── ServiceCollectionExtensions.cs # DI registration helpers
│   └── Middleware/
│       └── ExceptionHandlingMiddleware.cs # Global exception handling with structured logging
│
└── ReceiptService.Tests/                  # Test project
    ├── ReceiptService.Tests.csproj
    ├── TestWebApplicationFactory.cs       # xUnit test factory with dynamic RSA keys
    ├── Fixtures/
    │   ├── TestDatabaseFixture.cs         # Testcontainers.PostgreSql setup
    │   ├── TestRabbitMqFixture.cs         # Testcontainers.RabbitMQ setup
    │   └── TestRedisFixture.cs            # Testcontainers.Redis setup
    ├── Unit/
    │   ├── ReceiptNumberGeneratorTests.cs # Sequential numbering logic
    │   ├── TaxValidatorTests.cs           # Validation rules
    │   └── ReceiptMappingTests.cs         # Extension method mapping
    ├── Integration/
    │   ├── ReceiptCreationTests.cs        # Full receipt creation flow with DB
    │   ├── ReceiptVoidTests.cs            # Void operation with audit trail
    │   ├── PartialPaymentTests.cs         # Balance tracking tests
    │   ├── DuplicatePreventionTests.cs    # Concurrency control (optimistic locking)
    │   └── InvoiceServiceTimeoutTests.cs  # Retry logic with MockHttpMessageHandler
    └── Contract/
        ├── PdfGenerationEventTests.cs     # MassTransit message contract validation
        └── ApiContractTests.cs            # OpenAPI schema validation
```

**Structure Decision**: Standard microservice structure following Maliev guidelines. Single API project with layered architecture (Controllers → Services → Data). Test project uses real infrastructure via Testcontainers. No repository pattern (direct EF Core). Extension methods for mapping. Data Annotations for validation.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**No violations** - All constitution principles satisfied. No complexity justification required.
