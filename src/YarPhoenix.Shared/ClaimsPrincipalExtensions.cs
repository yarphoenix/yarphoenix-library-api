using System.Security.Claims;

namespace YarPhoenix.Shared;

public static class ClaimsPrincipalExtensions
{
    // JWT собирается с MapInboundClaims=false — тип клейма остаётся "sub".
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("Authenticated principal has no sub claim."));
}
