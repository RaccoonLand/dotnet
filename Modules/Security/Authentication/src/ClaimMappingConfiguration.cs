using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.JsonWebTokens;

namespace RaccoonLand.Modules.Security.Authentication;

internal static class ClaimMappingConfiguration
{
    public static void DisableDefaultClaimMapping()
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();
        JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();
    }
}
