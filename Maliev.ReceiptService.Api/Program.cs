using Maliev.Aspire.ServiceDefaults;
using Maliev.ReceiptService.Api.Services;
using Maliev.ReceiptService.Api.Services.IAM;
using Maliev.ReceiptService.Infrastructure.Data;

// Initialize bootstrap logging
using var loggerFactory = LoggerFactory.Create(logBuilder => logBuilder.AddConsole());
var bootstrapLogger = loggerFactory.CreateLogger("Program");

try
{
    Program.Log.StartingHost(bootstrapLogger, "Receipt Service");

    var builder = WebApplication.CreateBuilder(args);

    // --- Secrets & Configuration ---
    builder.AddGoogleSecretManagerVolume(); // Load secrets from /mnt/secrets if available

    // --- Infrastructure & Observability ---
    builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience
    builder.AddStandardMiddleware(options =>
    {
        options.EnableRequestLogging = true;
    });
    builder.AddServiceMeters("receipts-meter", "receipts-auth-meter"); // Register service meters for OpenTelemetry business metrics

    builder.Services.AddSingleton<Maliev.ReceiptService.Api.Services.Auth.AuthMetrics>();
    builder.Services.AddSingleton<Maliev.Aspire.ServiceDefaults.Authorization.IAuthMetrics>(sp =>
        sp.GetRequiredService<Maliev.ReceiptService.Api.Services.Auth.AuthMetrics>());

    // Database Context with ServiceDefaults (skip in Testing environment - handled by test factory)
    builder.AddPostgresDbContext<ReceiptDbContext>(connectionName: "ReceiptDbContext");

    builder.AddStandardCache("receipt:"); // Redis + in-memory fallback, memory-optimized // Redis with in-memory fallback

    // MassTransit with RabbitMQ - register consumers
    builder.AddMassTransitWithRabbitMq(configurator =>
    {
        // Register PDF callback consumer
        configurator.AddConsumer<Maliev.ReceiptService.Api.Consumers.PdfGeneratedEventConsumer>();
    });

    // --- API Configuration ---
    builder.AddStandardCors(); // CORS with fail-fast validation
    builder.AddDefaultApiVersioning(); // API versioning with URL segment reader

    // JWT Authentication (tests override via PostConfigureAll with dynamic RSA keys)
    builder.AddJwtAuthentication();
    builder.Services.AddPermissionAuthorization();

    // Add OpenAPI (must be in Program.cs for XML comments to work via source generator)
    if (!builder.Environment.IsProduction())
    {
        builder.AddStandardOpenApi(
            title: "MALIEV Receipt Service API",
            description: "Receipt management service. Handles receipt creation from invoices, PDF generation, tax validation, balance tracking, partial payments, voiding, and audit trail tracking.");
    }

    builder.Services.AddControllers();

    // Register Metrics
    var meter = new System.Diagnostics.Metrics.Meter("receipts-meter");
    var receiptsCreatedCounter = meter.CreateCounter<long>("receipts.created.total", "receipts", "Total number of receipts created");
    var creationDurationHistogram = meter.CreateHistogram<double>("receipts.creation.duration", "milliseconds", "Receipt creation duration");
    builder.Services.AddSingleton(receiptsCreatedCounter);
    builder.Services.AddSingleton(creationDurationHistogram);

    // Application Services
    builder.Services.AddScoped<ITaxValidator, ThailandTaxValidator>();
    builder.Services.AddScoped<IReceiptNumberGenerator, ReceiptNumberGenerator>();
    builder.Services.AddScoped<IReceiptService, Maliev.ReceiptService.Api.Services.ReceiptService>();
    builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

    // IAM Integration
    builder.AddIAMServiceClient("receipt");
    builder.Services.AddIAMRegistration<ReceiptIAMRegistrationService>("receipt");

    // External Service Clients with Polly v8 Resilience
    builder.AddServiceClient<IInvoiceServiceClient, InvoiceServiceClient>("InvoiceService");

    var app = builder.Build();
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    // --- Database Migrations ---
    await app.MigrateDatabaseAsync<ReceiptDbContext>();

    // Middleware Pipeline
    app.UseStandardMiddleware();
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseCors();

    app.UseAuthentication();
    app.UseAuthorization();

    // Map endpoints after middleware
    app.MapControllers();

    // Map Aspire default endpoints (/health, /alive, /metrics)
    app.MapDefaultEndpoints(servicePrefix: "receipt");

    // Map OpenAPI and Scalar documentation (dev/staging only)
    app.MapApiDocumentation(servicePrefix: "receipt");

    Program.Log.ServiceStarted(logger, "Receipt Service");
    await app.RunAsync();
}
catch (Exception ex)
{
    Program.Log.HostTerminated(bootstrapLogger, ex, "Receipt Service");
    throw;
}
finally
{
    loggerFactory.Dispose();
}

/// <summary>
/// Main program class for the application
/// </summary>
public partial class Program
{
    internal static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Starting {ServiceName} host")]
        public static partial void StartingHost(ILogger logger, string serviceName);

        [LoggerMessage(Level = LogLevel.Critical, Message = "{ServiceName} host terminated unexpectedly during startup")]
        public static partial void HostTerminated(ILogger logger, Exception ex, string serviceName);

        [LoggerMessage(Level = LogLevel.Information, Message = "{ServiceName} started successfully")]
        public static partial void ServiceStarted(ILogger logger, string serviceName);

        [LoggerMessage(Level = LogLevel.Error, Message = "Database migration failed - application may not function correctly")]
        public static partial void MigrationFailed(ILogger logger, Exception exception);
    }
}
