namespace Maliev.ReceiptService.Api.Services.IAM;

/// <summary>
/// Defines all permission strings for the ReceiptService.
/// </summary>
public static class ReceiptPermissions
{
    public static class Receipts
    {
        public const string Create = "receipt.receipts.create";
        public const string Read = "receipt.receipts.read";
        public const string Update = "receipt.receipts.update";
        public const string Void = "receipt.receipts.void";
        public const string Query = "receipt.receipts.query";
        public const string Export = "receipt.receipts.export";
    }

    public static class PartialPayments
    {
        public const string Create = "receipt.partial-payments.create";
        public const string Read = "receipt.partial-payments.read";
        public const string Manage = "receipt.partial-payments.manage";
    }

    public static class Audit
    {
        public const string Read = "receipt.audit.read";
        public const string Export = "receipt.audit.export";
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
