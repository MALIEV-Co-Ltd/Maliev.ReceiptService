namespace Maliev.ReceiptService.Application.Authorization;

/// <summary>
/// Describes caller-specific receipt visibility constraints.
/// </summary>
/// <param name="PrincipalId">Authenticated principal identifier.</param>
/// <param name="RestrictToCreatedReceipts">Whether receipt access must be limited to receipts created by the principal.</param>
public sealed record ReceiptAccessScope(string PrincipalId, bool RestrictToCreatedReceipts)
{
    /// <summary>
    /// Gets an unrestricted receipt access scope.
    /// </summary>
    public static ReceiptAccessScope Unrestricted { get; } = new(string.Empty, RestrictToCreatedReceipts: false);
}
