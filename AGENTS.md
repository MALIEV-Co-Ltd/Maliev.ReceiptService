# Maliev.ReceiptService Developer Guide

This repository hosts the **Receipt Service**, a core microservice built with **.NET 10.0** and **ASP.NET Core**.
It follows a clean, modular architecture separating API, Data, and Testing concerns.
This guide is intended for AI agents and developers to ensure consistency and correctness.

## 1. Build, Run & Test Commands

Execute all commands from the repository root: `B:\maliev\Maliev.ReceiptService`

### Build
Build the entire solution:
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
dotnet test Maliev.ReceiptService.slnx
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

### Linting & Code Style
Enforce code style using the standard .NET CLI tools.
```bash
# Check for issues
dotnet format --verify-no-changes

# Fix issues automatically
dotnet format
```

## 2. Project Structure

- **Maliev.ReceiptService.Api**:
  - Contains Controllers, Consumers (MassTransit), and Program.cs (Service Composition).
  - Uses `Maliev.Aspire.ServiceDefaults` for shared infrastructure.
- **Maliev.ReceiptService.Data**:
  - Entity Framework Core context (`ReceiptDbContext`), Entities, and Migrations.
- **Maliev.ReceiptService.Tests**:
  - xUnit test project containing both Unit and Integration tests.

## 3. Code Style & Conventions

### General Guidelines
- **Framework**: Target `.NET 10.0`.
- **Nullable Reference Types**: Enabled globally. Explicitly handle potential nulls.
- **Implicit Usings**: Enabled. reduce noise in `using` directives.
- **Async/Await**: Use asynchronous patterns for all I/O-bound operations (DB, Http, Message Bus).

### Naming Conventions
- **Classes, Methods, Properties**: PascalCase (e.g., `ReceiptService`, `CreateReceiptAsync`).
- **Local Variables, Parameters**: camelCase (e.g., `receiptId`, `customerName`).
- **Private Fields**: `_camelCase` (e.g., `_logger`, `_repository`).
- **Interfaces**: Prefix with `I` (e.g., `IReceiptService`).
- **Test Methods**: `MethodName_Condition_ExpectedResult` (e.g., `Validate_WithInvalidTaxId_ReturnsFailure`).

### API Controller Patterns
- **Attributes**:
  - `[ApiController]`
  - `[Route("receipt/v{version:apiVersion}/receipts")]` (or relevant resource)
  - `[ApiVersion("1.0")]`
  - `[Authorize]` (unless public)
- **Response Types**:
  - Return `IActionResult` explicitly (e.g., `Ok(result)`, `CreatedAtAction(...)`).
  - Use `[ProducesResponseType]` for Swagger documentation.
- **Error Responses**:
  - Return standardized error objects (JSON).
  - **Format**: `{ "errorCode": "string", "message": "string" }`
  - **Example**:
    ```csharp
    return NotFound(new { errorCode = "RECEIPT_NOT_FOUND", message = $"Receipt {id} not found" });
    ```

### Dependency Injection
- Register services in `Program.cs`.
- Prefer `Scoped` lifetime for business logic services and DbContexts.
- Use `Singleton` for stateless utilities or caches.
- Inject interfaces, not concrete types.

### Database & Entity Framework
- **Entities**: Located in `Maliev.ReceiptService.Data/Models/Entities`.
- **Configuration**: Use strict typing. Avoid `dynamic`.
- **Migrations**:
  ```bash
  dotnet ef migrations add <MigrationName> --project Maliev.ReceiptService.Infrastructure --startup-project Maliev.ReceiptService.Infrastructure
  ```

### Logging & Observability
- Inject `ILogger<T>` into all classes.
- Use structured logging (placeholders), not string interpolation.
  - **Good**: `_logger.LogInformation("Creating receipt for Invoice {InvoiceId}", invoiceId);`
  - **Bad**: `_logger.LogInformation($"Creating receipt for Invoice {invoiceId}");`
- Use `Maliev.Aspire.ServiceDefaults` for OpenTelemetry setup.

## 4. Testing Standards

### Unit Tests
- Located in `Maliev.ReceiptService.Tests/Unit`.
- Focus on Business Logic, Validators, and Mappers.
- Mock external dependencies (Repositories, Clients) using `Moq`.
- Use `[Fact]` for single cases and `[Theory]` with `[InlineData]` for parameterized tests.

### Integration Tests
- Located in `Maliev.ReceiptService.Tests/Integration`.
- Use `WebApplicationFactory` to spin up an in-memory test server.
- Test full HTTP request/response cycle.
- Verify database state if applicable (using In-Memory or Testcontainers if configured).

## 5. Working with External Rules
- **Cursor/Copilot**: If specific `.cursor/rules` or `.github/copilot-instructions.md` exist, prioritized those over general defaults.
- **CLAUDE.md**: Refer to `CLAUDE.md` in the root for high-level project directives and recent changes.

## 6. Example: Adding a New Endpoint

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


## Database & EF Core — Mandatory Rules

### EF Core Design Package
- ❌ `Microsoft.EntityFrameworkCore.Design` MUST NOT be in Api projects
- ✅ It belongs ONLY in the Infrastructure (or Data) project where migrations live
- Migration commands must target Infrastructure as both project and startup-project (since EF Core Design package is in Infrastructure):
  ```
  dotnet ef migrations add <Name> --project Maliev.<Domain>Service.Infrastructure --startup-project Maliev.<Domain>Service.Infrastructure
  ```

### PostgreSQL xmin Concurrency — Mandatory Pattern
Use shadow property ONLY. Never add a Xmin/xmin property to domain entities.
```csharp
entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion();
```
- ❌ Never use `UseXminAsConcurrencyToken()` (removed in Npgsql EF v7)
- ❌ Never use entity property `public uint Xmin { get; set; }` or `public uint xmin { get; set; }`
- ❌ Never use `.Ignore(e => e.Xmin)` — remove the entity property instead
