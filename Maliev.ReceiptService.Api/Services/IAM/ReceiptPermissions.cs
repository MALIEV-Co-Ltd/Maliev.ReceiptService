namespace Maliev.ReceiptService.Api.Services.IAM;

/// <summary>
/// Defines all permission strings for the ReceiptService.
/// Note: Constants include "Permission:" prefix for integration with ServiceDefaults policy provider.
/// </summary>
public static class ReceiptPermissions
{
    public static class Receipts
    {
        public const string Create = "Permission:receipt.receipts.create";
        public const string Read = "Permission:receipt.receipts.read";
        public const string Update = "Permission:receipt.receipts.update";
        public const string Void = "Permission:receipt.receipts.void";
        public const string Query = "Permission:receipt.receipts.query";
        public const string Export = "Permission:receipt.receipts.export";
    }

    public static class PartialPayments
    {
        public const string Create = "Permission:receipt.partial-payments.create";
        public const string Read = "Permission:receipt.partial-payments.read";
        public const string Manage = "Permission:receipt.partial-payments.manage";
    }

    public static class Audit
    {
        public const string Read = "Permission:receipt.audit.read";
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