using Maliev.Aspire.ServiceDefaults.IAM;

namespace Maliev.ReceiptService.Api.Services.IAM;

/// <summary>
/// Service for registering Receipt Service permissions and roles with IAM.
/// </summary>
public class ReceiptIAMRegistrationService : IAMRegistrationService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiptIAMRegistrationService"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">The logger.</param>
    public ReceiptIAMRegistrationService(
        IConfiguration configuration,
        ILogger<ReceiptIAMRegistrationService> logger)
        : base(configuration, logger, "receipt")
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return ReceiptPermissions.AllWithDescriptions.Select(p => new PermissionRegistration
        {
            PermissionId = p.Key,
            Description = p.Value
        });
    }

    /// <inheritdoc />
    protected override IEnumerable<RoleRegistration> GetPredefinedRoles()
    {
        return ReceiptPredefinedRoles.All.Select(r => new RoleRegistration
        {
            RoleId = r.RoleId,
            Description = r.Description,
            PermissionIds = r.Permissions.ToList(),
            IsCustom = false
        });
    }
}
