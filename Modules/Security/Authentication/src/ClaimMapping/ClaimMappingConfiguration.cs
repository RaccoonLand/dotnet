using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.JsonWebTokens;

namespace RaccoonLand.Modules.Security.Authentication.ClaimMapping;

internal static class ClaimMappingConfiguration
{
    /// <summary>
    /// Clears process-wide default JWT claim type maps. Call only when the application explicitly opts in.
    /// The change is irreversible for the process lifetime and affects other JWT handlers in the process.
    /// </summary>
    public static void ClearGlobalJwtClaimTypeMaps()
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();
        JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();
    }
}
