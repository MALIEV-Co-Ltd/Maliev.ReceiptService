# Maliev.ReceiptService Developer Guide

This repository hosts the **Receipt Service**, a core microservice built with **.NET 10.0** and **ASP.NET Core**.
It follows a clean, modular architecture separating API, Data, and Testing concerns.
This guide is intended for AI agents and developers to ensure consistency and correctness.

---

## 1. Build, Run & Test Commands

Execute all commands from the repository root: `B:\maliev\Maliev.ReceiptService`

### Build

Build the entire solution (treats warnings as errors — all must be fixed):
```bash
dotnet build Maliev.ReceiptService.slnx
```

### Run Application

Run the API project:
```bash
dotnet run --project Maliev.ReceiptService.Api/Maliev.ReceiptService.Api.csproj
```

### Testing

The project uses **xUnit** for unit and integration testing.

**Run All Tests:**
```bash
dotnet test Maliev.ReceiptService.slnx --verbosity normal
```

**Run Tests for a Specific Project:**
```bash
dotnet test Maliev.ReceiptService.Tests/Maliev.ReceiptService.Tests.csproj
```

**Run a Single Test File (Class):**
```bash
dotnet test Maliev.ReceiptService.Tests/Maliev.ReceiptService.Tests.csproj --filter "FullyQualifiedName~Maliev.ReceiptService.Tests.Unit.TaxValidatorTests"
```

**Run a Single Test Case:**
```bash
dotnet test Maliev.ReceiptService.Tests/Maliev.ReceiptService.Tests.csproj --filter "FullyQualifiedName=Maliev.ReceiptService.Tests.Unit.TaxValidatorTests.ValidateReceipt_WithValidThailandTaxFields_ReturnsSuccess"
```

**Run with Code Coverage:**
```bash
dotnet test Maliev.ReceiptService.slnx --collect:"XPlat Code Coverage"
```

### Linting & Code Style
```bash
# Format check
dotnet format Maliev.ReceiptService.slnx

# Fix issues automatically
dotnet format Maliev.ReceiptService.slnx
```

### EF Core Migrations

```bash
dotnet ef migrations add <MigrationName> --project Maliev.ReceiptService.Data --startup-project Maliev.ReceiptService.Data
```

---

## 2. Project Structure

- **Maliev.ReceiptService.Api**:
  - Contains Controllers, Consumers (MassTransit), and Program.cs (Service Composition).
  - Uses `Maliev.Aspire.ServiceDefaults` for shared infrastructure.
- **Maliev.ReceiptService.Data**:
  - Entity Framework Core context (`ReceiptDbContext`), Entities, and Migrations.
- **Maliev.ReceiptService.Tests**:
  - xUnit test project containing both Unit and Integration tests.

---

## 3. Code Style & Conventions

### C# Naming & Formatting

- **Namespaces**: File-scoped (`namespace Maliev.ReceiptService.Api.Controllers;`)
- **Classes/Methods/Properties**: `PascalCase` (e.g., `ReceiptService`, `CreateReceiptAsync`)
- **Private fields**: `_camelCase` (underscore prefix) (e.g., `_logger`, `_repository`)
- **Parameters/locals**: `camelCase` (e.g., `receiptId`, `customerName`)
- **Async methods**: Suffix with `Async` (e.g., `CreateReceiptAsync`)
- **Interfaces**: Prefix with `I` (e.g., `IReceiptService`)
- **Permissions**: GCP-style `{domain}.{plural-resource}.{action}` as `public const string` in a `Permissions` static class
  - Valid: `receipt.receipts.create`, `receipt.receipts.read`
  - Invalid: `receipt.receipt.create` (singular), `receipt.create` (missing resource)
- **XML docs**: Required on ALL public methods and properties
- **Nullable**: Enabled (`<Nullable>enable</Nullable>`). Use `?` explicitly
- **Imports**: System first, then third-party, then local. Alphabetize within groups. Remove unused `using`
- **Braces**: Allman style (new line) for methods and control structures. Expression-bodied for properties/accessors
- **Indentation**: 4 spaces, LF line endings, UTF-8, trim trailing whitespace
- **Test Methods**: `MethodName_StateUnderTest_ExpectedBehavior` (e.g., `Validate_WithInvalidTaxId_ReturnsFailure`)

### C# Patterns

- **DI**: Constructor injection with `private readonly` fields
- **Controllers**: `[ApiController]`, `[ApiVersion("1")]`, `[Route("receipt/v{version:apiVersion}/receipts")]`
- **Logging**: `ILogger<T>` with structured placeholders (never interpolate): `_logger.LogInformation("Processing {ReceiptId}", receiptId)`
- **Error handling**: Global exception middleware. Return `ProblemDetails` / `ErrorResponse` DTOs. Never expose stack traces
- **Manual mapping**: Static extension methods (`ToDto()`, `ToEntity()`). AutoMapper is banned
- **Validation**: `System.ComponentModel.DataAnnotations` on DTOs. FluentValidation is banned

### API Controller Patterns

- **Attributes**:
  - `[ApiController]`
  - `[Route("receipt/v{version:apiVersion}/receipts")]` (or relevant resource)
  - `[ApiVersion("1")]`
  - `[RequirePermission("receipt.receipts.action")]` on all endpoints (not plain `[Authorize]`)
- **Response Types**:
  - Return `IActionResult` explicitly (e.g., `Ok(result)`, `CreatedAtAction(...)`).
  - Use `[ProducesResponseType]` for documentation.
- **Error Responses**:
  - Return standardized error objects (JSON).
  - **Format**: `{ "errorCode": "string", "message": "string" }`

### Dependency Injection

- Register services in `Program.cs`.
- Prefer `Scoped` lifetime for business logic services and DbContexts.
- Use `Singleton` for stateless utilities or caches.
- Inject interfaces, not concrete types.

### Database & Entity Framework

- **Entities**: Located in `Maliev.ReceiptService.Data/Models/Entities`.
- **Configuration**: Use strict typing. Avoid `dynamic`.

### Logging & Observability

- Inject `ILogger<T>` into all classes.
- Use structured logging (placeholders), not string interpolation.
  - **Good**: `_logger.LogInformation("Creating receipt for Invoice {InvoiceId}", invoiceId);`
  - **Bad**: `_logger.LogInformation($"Creating receipt for Invoice {invoiceId}");`
- Use `Maliev.Aspire.ServiceDefaults` for OpenTelemetry setup.

---

## 4. Banned Libraries (Build Will Fail)

| Banned | Use Instead |
|--------|-------------|
| AutoMapper | Manual mapping extensions |
| FluentValidation | DataAnnotations or manual validation |
| FluentAssertions | Standard xUnit `Assert.*` |
| Swashbuckle/Swagger | Scalar (at `/{service}/scalar`) |
| InMemoryDatabase (EF Core) | Testcontainers with real PostgreSQL |

---

## 5. Testing Standards

### Unit Tests
- Located in `Maliev.ReceiptService.Tests/Unit`.
- Focus on Business Logic, Validators, and Mappers.
- Mock external dependencies (Repositories, Clients) using `Moq`.
- Use `[Fact]` for single cases and `[Theory]` with `[InlineData]` for parameterized tests.

### Integration Tests
- Located in `Maliev.ReceiptService.Tests/Integration`.
- Use `BaseIntegrationTestFactory<TProgram, TDbContext>` with Testcontainers (PostgreSQL, Redis, RabbitMQ). Never InMemoryDatabase.
- Test full HTTP request/response cycle.
- Verify database state if applicable.

### Testing Strategy (4-Tier Pyramid Context)

This service's tests cover **Tier 1 (Unit)** and **Tier 2 (Service Integration)** of the Maliev testing pyramid:

| Tier | What to Test | Infrastructure |
|------|-------------|---------------|
| **Unit** | Business logic, domain models, service methods with mocked dependencies | None (mocks only) |
| **Service Integration** | API endpoints, database persistence, permission enforcement, input validation | `BaseIntegrationTestFactory` + Testcontainers (Postgres/Redis/RabbitMQ) |

**Tier 3 (System Integration)** — cross-service workflows and event chains — is tested in `Maliev.Aspire.Tests/`.

#### Key Rules
- Use `BaseIntegrationTestFactory<TProgram, TDbContext>` for integration tests (real Testcontainers, never InMemoryDatabase)
- Test naming: `MethodName_StateUnderTest_ExpectedBehavior` or `HTTP_METHOD_Path_Scenario_ExpectedStatus`
- Minimum 80% code coverage per service
- Use `[Fact]` for single cases, `[Theory]` for parameterized tests
- **Eventual consistency**: Use `TestHelpers.WaitForAsync`. Never `Task.Delay`
- **MassTransit consumers**: Must have consumer tests using `AddMassTransitTestHarness()`

> Full ecosystem test strategy: `Maliev.Aspire.Tests/TEST_PLAN.md`

---

## 6. Working with External Rules

- **Cursor/Copilot**: If specific `.cursor/rules` or `.github/copilot-instructions.md` exist, prioritize those over general defaults.
- **CLAUDE.md**: Refer to `CLAUDE.md` in the root for high-level project directives and recent changes.

---

## 7. Example: Adding a New Endpoint

1. **Define Request/Response DTOs** in `Maliev.ReceiptService.Api/Models`.
2. **Add Service Method** to `IReceiptService` and implementation.
3. **Add Controller Method**:
   ```csharp
   [HttpGet("{id}")]
   [RequirePermission(ReceiptPermissions.Receipts.Read)]
   public async Task<IActionResult> Get(Guid id) { ... }
   ```
4. **Add Unit Test** in `Tests/Unit`.
5. **Add Integration Test** in `Tests/Integration`.

---

## 8. Mandatory Rules

- **`TreatWarningsAsErrors = true`**: Zero warnings allowed. No suppression
- **`[RequirePermission("domain.resources.action")]`**: On all endpoints, not plain `[Authorize]`
- **Creator role scope**: `roles.receipt.creator` must remain scoped through `ReceiptAccessGuard` and `ReceiptAccessScope`. Create checks InvoiceService `CreatedBy`; read/query checks local `Receipt.CreatedBy`.
- **Cross-boundary DTOs**: Before changing receipt creation, `InvoiceDto`, InvoiceService callers, controller payloads, or receipt/PDF MassTransit events, verify both sides of the JSON/message contract and add wire-shape tests where practical.
- **API versioning**: All routes versioned (`v1/`)
- **Service prefix**: Routes prefixed with service domain (e.g., `/receipt`)
- **Scalar docs**: Configured at `/{service}/scalar`
- **Secrets**: Never hardcoded. Use GCP Secret Manager or environment variables
- **Async/await**: All the way down. Pass `CancellationToken`
- **EF Core Design package**: Only in Data/Infrastructure project, never in Api
- **PostgreSQL xmin**: Shadow property only — `entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion()`. Never add entity property
- **Temporary files**: Generate in `/temp` folder, clean up afterwards

---

## 9. Git Rules

- Each `Maliev.*` folder is an independent git repo. `cd` into it before git commands
- **Commit early and often** after every meaningful unit of work. Do not accumulate changes
- **Never use `git checkout` to restore files** — commit first, then `git revert` or `git reset --soft`
- Feature branches merged to `develop` via PR. Do not push without being asked

---

## 10. Database & EF Core — Mandatory Rules

### EF Core Design Package
- `Microsoft.EntityFrameworkCore.Design` MUST NOT be in Api projects
- It belongs ONLY in the Data/Infrastructure project where migrations live

### PostgreSQL xmin Concurrency — Mandatory Pattern
Use shadow property ONLY. Never add a Xmin/xmin property to domain entities.
```csharp
entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion();
```
- Never use `UseXminAsConcurrencyToken()` (removed in Npgsql EF v7)
- Never use entity property `public uint Xmin { get; set; }` or `public uint xmin { get; set; }`
- Never use `.Ignore(e => e.Xmin)` — remove the entity property instead
