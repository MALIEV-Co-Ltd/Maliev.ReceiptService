# Quickstart Guide: Receipt Service Core

**Feature**: Receipt Service Core
**Date**: 2025-12-05
**For**: Developers implementing the Receipt Service

## Overview

This guide helps you get started with implementing the Receipt Service. Follow these steps in order for a smooth development experience.

---

## Prerequisites

Before starting implementation:

1. **Read the Documentation**:
   - [spec.md](./spec.md) - Feature requirements and success criteria
   - [research.md](./research.md) - Architectural decisions and patterns
   - [data-model.md](./data-model.md) - Entity definitions and relationships
   - [contracts/](./contracts/) - API and message contracts

2. **Required Tools**:
   - .NET 10 SDK
   - Docker Desktop (for Testcontainers)
   - PostgreSQL client (optional, for manual DB inspection)
   - RabbitMQ management UI (optional, for message inspection)
   - IDE: Visual Studio 2025, VS Code, or Rider

3. **GitHub Packages Access**:
   - Obtain `GITOPS_PAT` token with `read:packages` scope
   - Configure `nuget.config` with credentials (see Step 1 below)

---

## Step 1: Project Setup

### 1.1 Create Solution and Projects

```bash
cd B:\maliev\Maliev.ReceiptService

# Create solution
dotnet new sln -n Maliev.ReceiptService

# Create API project
dotnet new webapi -n ReceiptService.Api -o ReceiptService.Api --framework net10.0

# Create Test project
dotnet new xunit -n ReceiptService.Tests -o ReceiptService.Tests --framework net10.0

# Add projects to solution
dotnet sln add ReceiptService.Api/ReceiptService.Api.csproj
dotnet sln add ReceiptService.Tests/ReceiptService.Tests.csproj

# Add test reference to API
cd ReceiptService.Tests
dotnet add reference ../ReceiptService.Api/ReceiptService.Api.csproj
cd ..
```

### 1.2 Configure nuget.config

Create `nuget.config` in repository root:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github" value="https://nuget.pkg.github.com/maliev/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="%NUGET_USERNAME%" />
      <add key="ClearTextPassword" value="%NUGET_PASSWORD%" />
    </github>
  </packageSourceCredentials>
</configuration>
```

**Set environment variables** (PowerShell):
```powershell
$env:NUGET_USERNAME = "your-github-username"
$env:NUGET_PASSWORD = "your-gitops-pat-token"
```

### 1.3 Install Required Packages

**API Project** (`ReceiptService.Api`):

```bash
cd ReceiptService.Api

# Core packages
dotnet add package Maliev.Aspire.ServiceDefaults --version 1.0.0
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.0.0
dotnet add package MassTransit.RabbitMQ --version 8.3.0
dotnet add package Microsoft.AspNetCore.OpenApi --version 10.0.0
dotnet add package Scalar.AspNetCore --version 1.2.0
dotnet add package AspNetCore.HealthChecks.UI.Client --version 8.0.0

# Optional helpers
dotnet add package Microsoft.AspNetCore.Mvc.Versioning --version 5.1.0
dotnet add package Polly.Extensions.Http --version 3.0.0

cd ..
```

**Test Project** (`ReceiptService.Tests`):

```bash
cd ReceiptService.Tests

dotnet add package xunit --version 2.9.0
dotnet add package xunit.runner.visualstudio --version 2.8.0
dotnet add package Moq --version 4.20.0
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 10.0.0
dotnet add package Testcontainers.PostgreSql --version 3.9.0
dotnet add package Testcontainers.RabbitMQ --version 3.9.0
dotnet add package Testcontainers.Redis --version 3.9.0

cd ..
```

---

## Step 2: Database Setup

### 2.1 Create DbContext

`ReceiptService.Api/Data/ReceiptDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ReceiptService.Api.Models.Entities;

namespace ReceiptService.Api.Data;

public class ReceiptDbContext : DbContext
{
    public ReceiptDbContext(DbContextOptions<ReceiptDbContext> options) : base(options) { }

    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptLineItem> ReceiptLineItems => Set<ReceiptLineItem>();
    public DbSet<ReceiptAuditEvent> ReceiptAuditEvents => Set<ReceiptAuditEvent>();
    public DbSet<InvoiceBalanceTracker> InvoiceBalanceTrackers => Set<InvoiceBalanceTracker>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Receipt configuration
        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReceiptNumber).IsUnique();
            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => e.IssueDate);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Subtotal).HasPrecision(18, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.WithholdingTaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.RowVersion).IsRowVersion();
        });

        // ReceiptLineItem configuration
        modelBuilder.Entity<ReceiptLineItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReceiptId);
            entity.HasOne<Receipt>().WithMany().HasForeignKey(e => e.ReceiptId).OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.TaxRate).HasPrecision(5, 2);
            entity.Property(e => e.LineTotal).HasPrecision(18, 2);
        });

        // ReceiptAuditEvent configuration
        modelBuilder.Entity<ReceiptAuditEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReceiptId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.RetainUntil);
            entity.HasIndex(e => e.EventType);

            entity.HasOne<Receipt>().WithMany().HasForeignKey(e => e.ReceiptId).OnDelete(DeleteBehavior.NoAction);
        });

        // InvoiceBalanceTracker configuration
        modelBuilder.Entity<InvoiceBalanceTracker>(entity =>
        {
            entity.HasKey(e => e.InvoiceId);
            entity.HasIndex(e => e.RemainingBalance);

            entity.Property(e => e.TotalInvoiceAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalReceiptedAmount).HasPrecision(18, 2);
            entity.Property(e => e.RemainingBalance).HasPrecision(18, 2);
            entity.Property(e => e.RowVersion).IsRowVersion();
        });
    }
}
```

### 2.2 Create Initial Migration

```bash
cd ReceiptService.Api
dotnet ef migrations add InitialCreate
cd ..
```

---

## Step 3: Implement Program.cs

`ReceiptService.Api/Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Secrets & ServiceDefaults (MUST BE FIRST)
builder.AddGoogleSecretManagerVolume();
builder.AddServiceDefaults();

// 2. Infrastructure
builder.AddNpgsqlDbContext<ReceiptDbContext>("ReceiptDbContext");
builder.AddRedisDistributedCache("receipt:");
builder.AddJwtAuthentication();
builder.AddDefaultCors();
builder.AddMassTransitWithRabbitMq();

// 3. API & Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiVersioning(o => {
    o.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    o.ReportApiVersions = true;
});
builder.Services.AddOpenApi("v1", o => {
    o.AddDocumentTransformer((doc, _, _) => {
        doc.Info.Title = "MALIEV Receipt API";
        return Task.CompletedTask;
    });
});

// 4. Custom Services
builder.Services.AddScoped<IReceiptService, Services.ReceiptService>();
builder.Services.AddScoped<IReceiptNumberGenerator, ReceiptNumberGenerator>();
builder.Services.AddScoped<ITaxValidator, ThailandTaxValidator>();
builder.Services.AddHttpClient<IInvoiceServiceClient, InvoiceServiceClient>()
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 2;
        options.Retry.Delay = TimeSpan.FromSeconds(1);
        options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5);
    });

// 5. Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ReceiptDbContext>(tags: new[] { "db", "ready" });

var app = builder.Build();

// 6. Migrations
if (!app.Environment.IsEnvironment("Testing"))
{
    await app.MigrateDatabaseAsync<ReceiptDbContext>();
}

// 7. Middleware Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 8. Endpoints
app.MapDefaultEndpoints(servicePrefix: "receipt");
app.MapApiDocumentation(servicePrefix: "receipt");

app.Run();

// For testing
public partial class Program { }
```

---

## Step 4: Implement Core Services

### 4.1 Receipt Service (Business Logic)

`ReceiptService.Api/Services/ReceiptService.cs`:

```csharp
public class ReceiptService : IReceiptService
{
    private readonly ReceiptDbContext _context;
    private readonly IInvoiceServiceClient _invoiceClient;
    private readonly IReceiptNumberGenerator _numberGenerator;
    private readonly ITaxValidator _taxValidator;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ReceiptService> _logger;

    public async Task<ReceiptResponse> CreateReceiptAsync(CreateReceiptRequest request, string staffId, Guid correlationId)
    {
        using var activity = Activity.Current;
        activity?.SetTag("invoice_id", request.InvoiceId);
        activity?.SetTag("correlation_id", correlationId);

        // 1. Retrieve invoice from Invoice Service (with retry/timeout)
        var invoice = await _invoiceClient.GetInvoiceAsync(request.InvoiceId, correlationId);
        if (invoice == null)
            throw new InvoiceNotFoundException(request.InvoiceId);

        // 2. Validate tax compliance
        var taxValidation = _taxValidator.ValidateReceipt(invoice, request);
        if (!taxValidation.IsValid)
            throw new TaxValidationException(taxValidation.Errors);

        // 3. Check/update balance tracker (optimistic locking)
        var tracker = await GetOrCreateBalanceTrackerAsync(invoice);
        if (request.Amount > tracker.RemainingBalance)
            throw new InsufficientBalanceException(request.Amount, tracker.RemainingBalance);

        tracker.TotalReceiptedAmount += request.Amount;
        tracker.RemainingBalance -= request.Amount;
        tracker.LastUpdatedAt = DateTime.UtcNow;

        // 4. Generate sequential receipt number
        var receiptNumber = await _numberGenerator.GenerateNextNumberAsync("MALIEV", 2025);

        // 5. Create receipt entity
        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            ReceiptNumber = receiptNumber,
            InvoiceId = request.InvoiceId,
            // ... map all fields from invoice ...
            Status = ReceiptStatus.PendingPdf,
            CreatedBy = staffId,
            CorrelationId = correlationId
        };

        _context.Receipts.Add(receipt);

        // 6. Create audit event
        var auditEvent = new ReceiptAuditEvent
        {
            ReceiptId = receipt.Id,
            EventType = AuditEventType.Created,
            StaffMemberId = staffId,
            NewState = JsonSerializer.Serialize(receipt),
            CorrelationId = correlationId,
            RetainUntil = DateTime.UtcNow.AddYears(7)
        };
        _context.ReceiptAuditEvents.Add(auditEvent);

        // 7. Save to database
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Invoice balance was modified concurrently");
        }

        // 8. Publish PDF generation event
        await _publishEndpoint.Publish(new PdfGenerationRequestedEvent
        {
            ReceiptId = receipt.Id,
            // ... map all required fields ...
        });

        _logger.LogInformation("Receipt created: {ReceiptNumber} for Invoice: {InvoiceId}", receipt.ReceiptNumber, receipt.InvoiceId);

        return receipt.ToResponse();
    }
}
```

---

## Step 5: Testing Setup

### 5.1 Test Web Application Factory

`ReceiptService.Tests/TestWebApplicationFactory.cs`:

```csharp
public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder().Build();
    private readonly RabbitMqContainer _mqContainer = new RabbitMqBuilder().Build();
    private readonly RedisContainer _redisContainer = new RedisBuilder().Build();

    public RSA TestRsaKey { get; } = RSA.Create(2048);
    public string TestIssuer { get; } = "https://test.maliev.com";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Override Auth
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(TestRsaKey),
                    ValidateIssuer = true,
                    ValidIssuer = TestIssuer,
                    ValidateAudience = false
                };
            });

            // Override DB
            services.RemoveAll<DbContextOptions<ReceiptDbContext>>();
            services.AddDbContext<ReceiptDbContext>(o =>
                o.UseNpgsql(_dbContainer.GetConnectionString()));

            // Override Redis/RabbitMQ (use Testcontainer URLs)
        });
        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _mqContainer.StartAsync();
        await _redisContainer.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await _mqContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
        TestRsaKey.Dispose();
    }
}
```

### 5.2 Sample Integration Test

`ReceiptService.Tests/Integration/ReceiptCreationTests.cs`:

```csharp
public class ReceiptCreationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public ReceiptCreationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", factory.GenerateTestToken("staff-123"));
    }

    [Fact]
    public async Task CreateReceipt_WithValidInvoice_ShouldReturn201()
    {
        // Arrange
        var request = new CreateReceiptRequest
        {
            InvoiceId = Guid.NewGuid(),
            Amount = 1070.00m,
            PaymentMethod = "Bank Transfer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var receipt = await response.Content.ReadFromJsonAsync<ReceiptResponse>();
        Assert.NotNull(receipt);
        Assert.Matches(@"^MALIEV-\d{4}-\d{6}$", receipt.ReceiptNumber);
    }
}
```

---

## Step 6: Run and Test

### 6.1 Local Development

```bash
# Run database migrations
cd ReceiptService.Api
dotnet ef database update

# Run the API
dotnet run

# API will be available at: http://localhost:8080/receipt
# Health checks: http://localhost:8080/receipt/health
# Metrics: http://localhost:8080/receipt/metrics
# API docs: http://localhost:8080/receipt/scalar/v1
```

### 6.2 Run Tests

```bash
cd ReceiptService.Tests
dotnet test

# With detailed output
dotnet test --logger "console;verbosity=detailed"

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## Next Steps

1. **Complete all entities**: Implement models from [data-model.md](./data-model.md)
2. **Implement controllers**: Use contracts from [contracts/](./contracts/)
3. **Write tests first**: Follow Test-First Development (Constitution Principle III)
4. **Add observability**: Implement structured logging, metrics, tracing (FR-030/031/032)
5. **Run `/speckit.tasks`**: Generate implementation task breakdown

---

## Common Issues & Solutions

**Issue**: NuGet restore fails for `Maliev.Aspire.ServiceDefaults`
- **Solution**: Verify `NUGET_USERNAME` and `NUGET_PASSWORD` environment variables are set, and GITOPS_PAT has `read:packages` scope

**Issue**: Testcontainers fails to start
- **Solution**: Ensure Docker Desktop is running and you have sufficient resources (4GB RAM minimum)

**Issue**: EF migrations fail
- **Solution**: Check connection string in `appsettings.json` points to accessible PostgreSQL instance

**Issue**: JWT authentication fails in tests
- **Solution**: Verify `TestWebApplicationFactory` generates token with correct `TestRsaKey` and `TestIssuer`

---

## Useful Commands

```bash
# Create new migration
dotnet ef migrations add MigrationName --project ReceiptService.Api

# Update database
dotnet ef database update --project ReceiptService.Api

# Rollback migration
dotnet ef database update PreviousMigrationName --project ReceiptService.Api

# Generate SQL script
dotnet ef migrations script --project ReceiptService.Api

# Watch mode (auto-rebuild on file changes)
dotnet watch run --project ReceiptService.Api

# Format code
dotnet format

# Check for warnings (should be zero per Constitution)
dotnet build --warnaserror
```

---

**Quickstart Status**: ✅ Complete - Ready for implementation! Follow this guide step-by-step for a smooth development experience.
