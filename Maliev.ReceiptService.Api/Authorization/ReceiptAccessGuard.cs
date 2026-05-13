using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Maliev.ReceiptService.Application.Authorization;
using Maliev.ReceiptService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Maliev.ReceiptService.Api.Authorization;

/// <summary>
/// Applies receipt object-scope rules that are narrower than endpoint action permissions.
/// </summary>
public sealed class ReceiptAccessGuard
{
    private static readonly string[] RoleClaimTypes =
    [
        ClaimTypes.Role,
        "role",
        "roles",
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    ];

    private readonly ReceiptDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiptAccessGuard"/> class.
    /// </summary>
    /// <param name="context">Receipt database context.</param>
    public ReceiptAccessGuard(ReceiptDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Builds the receipt visibility scope for the current principal.
    /// </summary>
    /// <param name="user">Authenticated principal.</param>
    /// <returns>Receipt access scope.</returns>
    public ReceiptAccessScope GetScope(ClaimsPrincipal user)
    {
        var principalId = GetPrincipalId(user);
        if (string.IsNullOrWhiteSpace(principalId))
        {
            return new ReceiptAccessScope(string.Empty, RestrictToCreatedReceipts: true);
        }

        var roles = GetRoles(user);
        var hasWildcardPermission = user.Claims.Any(c =>
            (c.Type == "permissions" || c.Type == "permission") &&
            string.Equals(c.Value, "*", StringComparison.OrdinalIgnoreCase));

        var hasCreatorRole = roles.Any(IsCreatorRole);
        var hasUnrestrictedRole = roles.Any(IsUnrestrictedRole) || hasWildcardPermission;

        return new ReceiptAccessScope(principalId, hasCreatorRole && !hasUnrestrictedRole);
    }

    /// <summary>
    /// Gets the best available principal identifier for audit and ownership checks.
    /// </summary>
    /// <param name="user">Authenticated principal.</param>
    /// <returns>Principal identifier, if present.</returns>
    public string? GetPrincipalId(ClaimsPrincipal user)
    {
        return GetPrincipalIdCore(user);
    }

    /// <summary>
    /// Checks whether the current principal can access a receipt by id.
    /// </summary>
    /// <param name="receiptId">Receipt identifier.</param>
    /// <param name="user">Authenticated principal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Access decision.</returns>
    public async Task<ReceiptAccessDecision> CheckReceiptAsync(
        Guid receiptId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var scope = GetScope(user);
        if (!scope.RestrictToCreatedReceipts)
        {
            return ReceiptAccessDecision.Allowed;
        }

        if (string.IsNullOrWhiteSpace(scope.PrincipalId))
        {
            return ReceiptAccessDecision.Forbidden;
        }

        var receipt = await _context.Receipts
            .Where(r => r.Id == receiptId)
            .Select(r => new { r.CreatedBy })
            .FirstOrDefaultAsync(cancellationToken);

        if (receipt == null)
        {
            return ReceiptAccessDecision.NotFound;
        }

        return string.Equals(receipt.CreatedBy, scope.PrincipalId, StringComparison.OrdinalIgnoreCase)
            ? ReceiptAccessDecision.Allowed
            : ReceiptAccessDecision.Forbidden;
    }

    private static string? GetPrincipalIdCore(ClaimsPrincipal user)
    {
        return user.FindFirst("user_id")?.Value
            ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private static HashSet<string> GetRoles(ClaimsPrincipal user)
    {
        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var claim in user.Claims.Where(c => RoleClaimTypes.Contains(c.Type)))
        {
            foreach (var value in claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                roles.Add(value);
            }
        }

        return roles;
    }

    private static bool IsCreatorRole(string role)
    {
        return string.Equals(role, "creator", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, ReceiptPredefinedRoles.Creator, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnrestrictedRole(string role)
    {
        return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, ReceiptPredefinedRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "manager", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, ReceiptPredefinedRoles.Manager, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "auditor", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, ReceiptPredefinedRoles.Auditor, StringComparison.OrdinalIgnoreCase);
    }
}
