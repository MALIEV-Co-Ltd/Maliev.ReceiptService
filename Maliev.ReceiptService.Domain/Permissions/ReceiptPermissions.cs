namespace Maliev.ReceiptService.Domain.Permissions;

public static class ReceiptPermissions
{
    public static class Receipts
    {
        public const string Create = "receipt.receipts.create";
        public const string Read = "receipt.receipts.read";
        public const string Update = "receipt.receipts.update";
        public const string Void = "receipt.receipts.void";
        public const string Send = "receipt.receipts.send";
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

    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { Receipts.Create, "Create new receipts" },
        { Receipts.Read, "Read receipt details" },
        { Receipts.Update, "Update receipt information" },
        { Receipts.Void, "Void receipts" },
        { Receipts.Send, "Send receipt to customer" },
        { Receipts.Query, "Query receipt history" },
        { Receipts.Export, "Export receipt data" },
        { PartialPayments.Create, "Create partial payment records" },
        { PartialPayments.Read, "Read partial payment details" },
        { PartialPayments.Manage, "Update/Delete partial payments" },
        { Audit.Read, "Read receipt audit logs" },
        { Audit.Export, "Export audit data" }
    };

    public static IEnumerable<string> All => AllWithDescriptions.Keys;
}
