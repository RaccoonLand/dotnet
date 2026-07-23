using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using RaccoonLand.Modules.Security.Authentication.Tests.Support;

namespace RaccoonLand.Modules.Security.Authentication.Tests.ClaimMapping;

[Collection("ClaimMapIsolation")]
public sealed class ClearGlobalJwtClaimTypeMapsTests
{
    [Fact]
    public void AddRaccoonLandAuthentication_WhenClearGlobalMapsFalse_DoesNotClearMaps()
    {
        var restore = GlobalClaimMapSnapshot.Capture();

        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap["seed-in"] = "mapped-in";
        JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap["seed-out"] = "mapped-out";
        JsonWebTokenHandler.DefaultInboundClaimTypeMap["seed-json"] = "mapped-json";

        try
        {
            var config = AuthenticationTestHelpers.MinimalJwtConfig();
            config["Authentication:ClearGlobalJwtClaimTypeMaps"] = "false";

            _ = AuthenticationTestHelpers.CreateServices(config).BuildServiceProvider();

            Assert.Equal("mapped-in", JwtSecurityTokenHandler.DefaultInboundClaimTypeMap["seed-in"]);
            Assert.Equal("mapped-out", JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap["seed-out"]);
            Assert.Equal("mapped-json", JsonWebTokenHandler.DefaultInboundClaimTypeMap["seed-json"]);
        }
        finally
        {
            restore();
        }
    }

    [Fact]
    public void AddRaccoonLandAuthentication_WhenClearGlobalMapsTrue_ClearsMaps()
    {
        var restore = GlobalClaimMapSnapshot.Capture();

        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap["seed-in"] = "mapped-in";
        JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap["seed-out"] = "mapped-out";
        JsonWebTokenHandler.DefaultInboundClaimTypeMap["seed-json"] = "mapped-json";

        try
        {
            var config = AuthenticationTestHelpers.MinimalJwtConfig();
            config["Authentication:ClearGlobalJwtClaimTypeMaps"] = "true";

            _ = AuthenticationTestHelpers.CreateServices(config).BuildServiceProvider();

            Assert.Empty(JwtSecurityTokenHandler.DefaultInboundClaimTypeMap);
            Assert.Empty(JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap);
            Assert.Empty(JsonWebTokenHandler.DefaultInboundClaimTypeMap);
        }
        finally
        {
            // Restore the full process-wide maps so clearing them does not leak into
            // other tests regardless of run order.
            restore();
        }
    }

    private static class GlobalClaimMapSnapshot
    {
        public static Action Capture()
        {
            var inbound = new Dictionary<string, string>(JwtSecurityTokenHandler.DefaultInboundClaimTypeMap);
            var outbound = new Dictionary<string, string>(JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap);
            var jsonInbound = new Dictionary<string, string>(JsonWebTokenHandler.DefaultInboundClaimTypeMap);

            return () =>
            {
                Replace(JwtSecurityTokenHandler.DefaultInboundClaimTypeMap, inbound);
                Replace(JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap, outbound);
                Replace(JsonWebTokenHandler.DefaultInboundClaimTypeMap, jsonInbound);
            };
        }

        private static void Replace(IDictionary<string, string> target, IDictionary<string, string> source)
        {
            target.Clear();
            foreach (var (key, value) in source)
            {
                target[key] = value;
            }
        }
    }
}
