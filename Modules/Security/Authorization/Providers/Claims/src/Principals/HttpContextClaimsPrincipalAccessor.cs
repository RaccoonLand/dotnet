using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace RaccoonLand.Modules.Security.Authorization.Claims.Principals;

/// <summary>
/// Default <see cref="IClaimsPrincipalAccessor"/> for ASP.NET Core hosts: returns
/// <c>HttpContext.User</c> from the ambient <see cref="IHttpContextAccessor"/>.
/// </summary>
public sealed class HttpContextClaimsPrincipalAccessor(IHttpContextAccessor httpContextAccessor)
    : IClaimsPrincipalAccessor
{
    public ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;
}
