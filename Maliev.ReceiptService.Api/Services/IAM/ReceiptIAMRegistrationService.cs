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
            new PermissionRegistration { PermissionId = ReceiptPermissions.Receipts.Create.Replace("Permission:", ""), Description = "Create new receipts" },
            new PermissionRegistration { PermissionId = ReceiptPermissions.Receipts.Read.Replace("Permission:", ""), Description = "Read receipt details" },
            new PermissionRegistration { PermissionId = ReceiptPermissions.Receipts.Update.Replace("Permission:", ""), Description = "Update receipt information" },
            new PermissionRegistration { PermissionId = ReceiptPermissions.Receipts.Void.Replace("Permission:", ""), Description = "Void receipts" },
            new PermissionRegistration { PermissionId = ReceiptPermissions.Receipts.Query.Replace("Permission:", ""), Description = "Query receipt history" },
            new PermissionRegistration { PermissionId = ReceiptPermissions.Receipts.Export.Replace("Permission:", ""), Description = "Export receipt data" },
            new PermissionRegistration { PermissionId = ReceiptPermissions.PartialPayments.Create.Replace("Permission:", ""), Description = "Create partial payment records" },
            new PermissionRegistration { PermissionId = ReceiptPermissions.PartialPayments.Read.Replace("Permission:", ""), Description = "Read partial payment details" },
            new PermissionRegistration { PermissionId = ReceiptPermissions.PartialPayments.Manage.Replace("Permission:", ""), Description = "Update/Delete partial payments" },
            new PermissionRegistration { PermissionId = ReceiptPermissions.Audit.Read.Replace("Permission:", ""), Description = "Read receipt audit logs" },
            new PermissionRegistration { PermissionId = ReceiptPermissions.Audit.Export.Replace("Permission:", ""), Description = "Export audit data" }
        };
    }

    protected override IEnumerable<RoleRegistration> GetPredefinedRoles()
    {
        return ReceiptPredefinedRoles.GetRoles().Select(r => new RoleRegistration
        {
            RoleId = r.RoleId,
            Description = r.Description,
            PermissionIds = r.PermissionIds.Select(p => p.Replace("Permission:", "")).ToList(),
            IsCustom = r.IsCustom
        });
    }

    public async Task RegisterWithCheckAsync(CancellationToken cancellationToken)
    {
        await base.RegisterAsync(cancellationToken);
    }
}
