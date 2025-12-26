using Maliev.Aspire.ServiceDefaults.IAM;
using Microsoft.Extensions.Options;

namespace Maliev.ReceiptService.Api.Services.IAM;

public class ReceiptIAMRegistrationService : IAMRegistrationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReceiptIAMRegistrationService> _logger;

    public ReceiptIAMRegistrationService(
        IHttpClientFactory httpClientFactory,
        ILogger<ReceiptIAMRegistrationService> logger,
        IConfiguration configuration) 
        : base(httpClientFactory, logger, "receipt")
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return new[]
        {
            new PermissionRegistration { PermissionId = ReceiptPermissions.Receipts.Create, Description = "Create new receipts" },
            new PermissionRegistration { PermissionId = ReceiptPermissions.Receipts.Read, Description = "Read receipt details" },
            new PermissionRegistration { PermissionId = ReceiptPermissions.Receipts.Update, Description = "Update receipt information" },
            new PermissionRegistration { PermissionId = ReceiptPermissions.Receipts.Void, Description = "Void receipts" },
            new PermissionRegistration { PermissionId = ReceiptPermissions.Receipts.Query, Description = "Query receipt history" },
            new PermissionRegistration { PermissionId = ReceiptPermissions.Receipts.Export, Description = "Export receipt data" },
            new PermissionRegistration { PermissionId = ReceiptPermissions.PartialPayments.Create, Description = "Create partial payment records" },
            new PermissionRegistration { PermissionId = ReceiptPermissions.PartialPayments.Read, Description = "Read partial payment details" },
            new PermissionRegistration { PermissionId = ReceiptPermissions.PartialPayments.Manage, Description = "Update/Delete partial payments" },
            new PermissionRegistration { PermissionId = ReceiptPermissions.Audit.Read, Description = "Read receipt audit logs" },
            new PermissionRegistration { PermissionId = ReceiptPermissions.Audit.Export, Description = "Export audit data" }
        };
    }

    protected override IEnumerable<RoleRegistration> GetPredefinedRoles()
    {
        return ReceiptPredefinedRoles.GetRoles();
    }

    public async Task RegisterWithCheckAsync(CancellationToken cancellationToken)
    {
        var iamEnabled = _configuration.GetValue<bool>("Features:PermissionBasedAuthEnabled", true);
        if (!iamEnabled)
        {
            _logger.LogInformation("IAM registration skipped (PermissionBasedAuthEnabled=false)");
            return;
        }

        await base.RegisterAsync(cancellationToken);
    }
}
