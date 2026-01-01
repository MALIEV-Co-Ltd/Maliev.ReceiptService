namespace Maliev.ReceiptService.Api.Services.IAM;

/// <summary>
/// Defines all permission strings for the ReceiptService.
/// Note: Constants include "Permission:" prefix for integration with ServiceDefaults policy provider.
/// </summary>
public static class ReceiptPermissions
{
    /// <summary>
    /// Permissions for receipt operations.
    /// </summary>
    public static class Receipts
    {
        /// <summary>Permission to create a receipt.</summary>
        public const string Create = "Permission:receipt.receipts.create";
        /// <summary>Permission to read a receipt.</summary>
        public const string Read = "Permission:receipt.receipts.read";
        /// <summary>Permission to update a receipt.</summary>
        public const string Update = "Permission:receipt.receipts.update";
        /// <summary>Permission to void a receipt.</summary>
        public const string Void = "Permission:receipt.receipts.void";
        /// <summary>Permission to query receipts.</summary>
        public const string Query = "Permission:receipt.receipts.query";
        /// <summary>Permission to export receipts.</summary>
        public const string Export = "Permission:receipt.receipts.export";
    }

    /// <summary>
    /// Permissions for partial payment operations.
    /// </summary>
    public static class PartialPayments
    {
        /// <summary>Permission to create a partial payment.</summary>
        public const string Create = "Permission:receipt.partial-payments.create";
        /// <summary>Permission to read a partial payment.</summary>
        public const string Read = "Permission:receipt.partial-payments.read";
        /// <summary>Permission to manage partial payments.</summary>
        public const string Manage = "Permission:receipt.partial-payments.manage";
    }

    /// <summary>
    /// Permissions for audit operations.
    /// </summary>
    public static class Audit
    {
        /// <summary>Permission to read audit logs.</summary>
        public const string Read = "Permission:receipt.audit.read";
        /// <summary>Permission to export audit logs.</summary>
        public const string Export = "Permission:receipt.audit.export";
    }

    /// <summary>
    /// Gets all defined permissions.
    /// </summary>
    public static IEnumerable<string> All => new[]
    {
        Receipts.Create,
        Receipts.Read,
        Receipts.Update,
        Receipts.Void,
        Receipts.Query,
        Receipts.Export,
        PartialPayments.Create,
        PartialPayments.Read,
        PartialPayments.Manage,
        Audit.Read,
        Audit.Export
    };
}