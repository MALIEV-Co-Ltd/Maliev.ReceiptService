namespace Maliev.ReceiptService.Api.Services.IAM;

/// <summary>
/// Defines permission constants for the Receipt Service.
/// Follows GCP-style naming: {service}.{resource}.{action}
/// </summary>
public static class ReceiptPermissions
{
    /// <summary>Permissions for receipt operations.</summary>
    public static class Receipts
    {
        /// <summary>Permission to create receipts.</summary>
        public const string Create = "receipt.receipts.create";
        /// <summary>Permission to read receipts.</summary>
        public const string Read = "receipt.receipts.read";
        /// <summary>Permission to update receipts.</summary>
        public const string Update = "receipt.receipts.update";
        /// <summary>Permission to void receipts.</summary>
        public const string Void = "receipt.receipts.void";
        /// <summary>Permission to send receipts.</summary>
        public const string Send = "receipt.receipts.send";
        /// <summary>Permission to query receipts.</summary>
        public const string Query = "receipt.receipts.query";
        /// <summary>Permission to export receipts.</summary>
        public const string Export = "receipt.receipts.export";
    }

    /// <summary>Permissions for partial payment operations.</summary>
    public static class PartialPayments
    {
        /// <summary>Permission to create partial payments.</summary>
        public const string Create = "receipt.partial-payments.create";
        /// <summary>Permission to read partial payments.</summary>
        public const string Read = "receipt.partial-payments.read";
        /// <summary>Permission to manage partial payments.</summary>
        public const string Manage = "receipt.partial-payments.manage";
    }

    /// <summary>Permissions for audit operations.</summary>
    public static class Audits
    {
        /// <summary>Permission to read audit logs.</summary>
        public const string Read = "receipt.audits.read";
        /// <summary>Permission to export audit logs.</summary>
        public const string Export = "receipt.audits.export";
    }

    /// <summary>
    /// Collection of all defined receipt permissions with descriptions.
    /// </summary>
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
        { Audits.Read, "Read receipt audit logs" },
        { Audits.Export, "Export audit data" }
    };

    /// <summary>All available permission codes</summary>
    public static IEnumerable<string> All => AllWithDescriptions.Keys;
}
