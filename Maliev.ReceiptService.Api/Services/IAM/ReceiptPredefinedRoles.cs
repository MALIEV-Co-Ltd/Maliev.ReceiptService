namespace Maliev.ReceiptService.Api.Services.IAM;

/// <summary>
/// Predefined roles for the Receipt Service.
/// </summary>
public static class ReceiptPredefinedRoles
{
    /// <summary>Full access to all receipt operations.</summary>
    public const string Admin = "roles.receipt.admin";
    /// <summary>Operational access to manage receipts.</summary>
    public const string Manager = "roles.receipt.manager";
    /// <summary>Can create and manage own receipts.</summary>
    public const string Creator = "roles.receipt.creator";
    /// <summary>Read-only access to receipts.</summary>
    public const string Viewer = "roles.receipt.viewer";
    /// <summary>Focused on auditing and reporting.</summary>
    public const string Auditor = "roles.receipt.auditor";

    /// <summary>
    /// Collection of all predefined roles for the Receipt Service.
    /// </summary>
    public static readonly IReadOnlyList<(string RoleId, string Description, string[] Permissions)> All = new List<(string, string, string[])>
    {
        (Admin, "Full access to all receipt service operations", ReceiptPermissions.All.ToArray()),

        (Manager, "Manage receipts and payments, but cannot export audit logs", new[]
        {
            ReceiptPermissions.Receipts.Create,
            ReceiptPermissions.Receipts.Read,
            ReceiptPermissions.Receipts.Update,
            ReceiptPermissions.Receipts.Void,
            ReceiptPermissions.Receipts.Query,
            ReceiptPermissions.Receipts.Export,
            ReceiptPermissions.Receipts.Send,
            ReceiptPermissions.PartialPayments.Create,
            ReceiptPermissions.PartialPayments.Read,
            ReceiptPermissions.PartialPayments.Manage,
            ReceiptPermissions.Audits.Read
        }),

        (Creator, "Can create and read receipts and payments", new[]
        {
            ReceiptPermissions.Receipts.Create,
            ReceiptPermissions.Receipts.Read,
            ReceiptPermissions.PartialPayments.Create,
            ReceiptPermissions.PartialPayments.Read
        }),

        (Viewer, "Read-only access to receipts", new[]
        {
            ReceiptPermissions.Receipts.Read,
            ReceiptPermissions.PartialPayments.Read
        }),

        (Auditor, "Access to view and export receipts and audit logs", new[]
        {
            ReceiptPermissions.Receipts.Read,
            ReceiptPermissions.Receipts.Query,
            ReceiptPermissions.Receipts.Export,
            ReceiptPermissions.Audits.Read,
            ReceiptPermissions.Audits.Export
        })
    };
}
