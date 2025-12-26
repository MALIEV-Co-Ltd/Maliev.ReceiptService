using Maliev.Aspire.ServiceDefaults.IAM;

namespace Maliev.ReceiptService.Api.Services.IAM;

public static class ReceiptPredefinedRoles
{
    public static IEnumerable<RoleRegistration> GetRoles()
    {
        yield return new RoleRegistration
        {
            RoleId = "roles.receipt.admin",
            Description = "Full access to all receipt service operations",
            PermissionIds = ReceiptPermissions.All.ToList()
        };

        yield return new RoleRegistration
        {
            RoleId = "roles.receipt.manager",
            Description = "Manage receipts and payments, but cannot export audit logs",
            PermissionIds = new List<string>
            {
                ReceiptPermissions.Receipts.Create,
                ReceiptPermissions.Receipts.Read,
                ReceiptPermissions.Receipts.Update,
                ReceiptPermissions.Receipts.Void,
                ReceiptPermissions.Receipts.Query,
                ReceiptPermissions.Receipts.Export,
                ReceiptPermissions.PartialPayments.Create,
                ReceiptPermissions.PartialPayments.Read,
                ReceiptPermissions.PartialPayments.Manage,
                ReceiptPermissions.Audit.Read
            }
        };

        yield return new RoleRegistration
        {
            RoleId = "roles.receipt.creator",
            Description = "Can create and read receipts and payments",
            PermissionIds = new List<string>
            {
                ReceiptPermissions.Receipts.Create,
                ReceiptPermissions.Receipts.Read,
                ReceiptPermissions.PartialPayments.Create,
                ReceiptPermissions.PartialPayments.Read
            }
        };

        yield return new RoleRegistration
        {
            RoleId = "roles.receipt.viewer",
            Description = "Read-only access to receipts",
            PermissionIds = new List<string>
            {
                ReceiptPermissions.Receipts.Read,
                ReceiptPermissions.PartialPayments.Read
            }
        };

        yield return new RoleRegistration
        {
            RoleId = "roles.receipt.auditor",
            Description = "Access to view and export receipts and audit logs",
            PermissionIds = new List<string>
            {
                ReceiptPermissions.Receipts.Read,
                ReceiptPermissions.Receipts.Query,
                ReceiptPermissions.Receipts.Export,
                ReceiptPermissions.Audit.Read,
                ReceiptPermissions.Audit.Export
            }
        };
    }
}
