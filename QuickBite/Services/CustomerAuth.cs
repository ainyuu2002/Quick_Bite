using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace QuickBite.Services;

public static class CustomerAuth
{
    public const string Scheme = "CustomerAuth";
    public const string Policy = "CustomerOnly";

    public static int? GetCustomerId(ClaimsPrincipal user)
    {
        var identity = user.Identities
            .FirstOrDefault(i => i.IsAuthenticated && i.AuthenticationType == Scheme);
        return int.TryParse(
            identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
            ? id
            : null;
    }

    public static async Task<int?> GetCustomerIdAsync(HttpContext httpContext)
    {
        var fromUser = GetCustomerId(httpContext.User);
        if (fromUser is not null)
        {
            return fromUser;
        }

        var result = await httpContext.AuthenticateAsync(Scheme);
        return result.Succeeded ? GetCustomerId(result.Principal) : null;
    }
}
