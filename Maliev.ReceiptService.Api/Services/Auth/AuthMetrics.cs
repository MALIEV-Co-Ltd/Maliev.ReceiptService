using System.Diagnostics;
using System.Diagnostics.Metrics;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.ReceiptService.Api.Services.Auth;

/// <summary>
/// Service for recording authorization-related business metrics for ReceiptService.
/// </summary>
public class AuthMetrics : IAuthMetrics
{
    private readonly Counter<long> _authSuccessCounter;
    private readonly Counter<long> _authFailureCounter;
    private readonly KeyValuePair<string, object?>[] _defaultTags;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthMetrics"/> class.
    /// </summary>
    /// <param name="meterFactory">The meter factory.</param>
    /// <param name="configuration">The configuration.</param>
    public AuthMetrics(IMeterFactory meterFactory, IConfiguration configuration)
    {
        var serviceName = configuration["Service:Name"] ?? "ReceiptService";
        var meterName = "receipts-auth-meter";
        var meter = meterFactory.Create(meterName);

        _authSuccessCounter = meter.CreateCounter<long>("receipt_auth_success_total", description: "Total successful authorizations");
        _authFailureCounter = meter.CreateCounter<long>("receipt_auth_failure_total", description: "Total failed authorizations");

        _defaultTags = new[]
        {
            new KeyValuePair<string, object?>("service_name", serviceName),
            new KeyValuePair<string, object?>("version", configuration["Service:Version"] ?? "1.0.0"),
            new KeyValuePair<string, object?>("environment", configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development")
        };
    }

    /// <summary>
    /// Records a successful authorization event.
    /// </summary>
    /// <param name="permission">The permission that was checked.</param>
    public void RecordSuccess(string permission)
    {
        var tags = new TagList();
        foreach (var tag in _defaultTags) tags.Add(tag);
        tags.Add("permission", permission);
        _authSuccessCounter.Add(1, tags);
    }

    /// <summary>
    /// Records a failed authorization event.
    /// </summary>
    /// <param name="permission">The permission that was checked.</param>
    /// <param name="reason">The reason for failure.</param>
    public void RecordFailure(string permission, string reason)
    {
        var tags = new TagList();
        foreach (var tag in _defaultTags) tags.Add(tag);
        tags.Add("permission", permission);
        tags.Add("reason", reason);
        _authFailureCounter.Add(1, tags);
    }
}
