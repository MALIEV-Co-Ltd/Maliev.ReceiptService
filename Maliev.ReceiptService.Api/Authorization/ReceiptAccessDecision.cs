namespace Maliev.ReceiptService.Api.Authorization;

/// <summary>
/// Result of checking receipt object access.
/// </summary>
public enum ReceiptAccessDecision
{
    /// <summary>The caller can access the receipt or does not require object scoping.</summary>
    Allowed,

    /// <summary>The receipt does not exist.</summary>
    NotFound,

    /// <summary>The receipt exists but is outside the caller's allowed scope.</summary>
    Forbidden
}
