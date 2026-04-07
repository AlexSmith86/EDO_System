using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace EDO.Client.Services;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly LocalStorageService _localStorage;
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public JwtAuthenticationStateProvider(LocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync("authToken");

            if (string.IsNullOrWhiteSpace(token))
                return Anonymous;

            var claims = ParseClaimsFromJwt(token);
            if (claims is null || IsExpired(claims))
            {
                await _localStorage.RemoveItemAsync("authToken");
                return Anonymous;
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            // Any browser storage/interoperability issue should not crash app startup.
            return Anonymous;
        }
    }

    public async Task MarkUserAsAuthenticated(string token)
    {
        await _localStorage.SetItemAsync("authToken", token);
        var claims = ParseClaimsFromJwt(token);
        if (claims is null)
        {
            await MarkUserAsLoggedOut();
            return;
        }

        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task MarkUserAsLoggedOut()
    {
        await _localStorage.RemoveItemAsync("authToken");
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private static IEnumerable<Claim>? ParseClaimsFromJwt(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.Claims;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsExpired(IEnumerable<Claim> claims)
    {
        var exp = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        if (string.IsNullOrWhiteSpace(exp))
            return false;

        if (!long.TryParse(exp, out var unix))
            return false;

        try
        {
            // Some JWT providers store exp in milliseconds.
            if (unix > 9_999_999_999)
                unix /= 1000;

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(unix);
            return expiresAt <= DateTimeOffset.UtcNow;
        }
        catch
        {
            // If exp is malformed/out of range, treat token as invalid/expired.
            return true;
        }
    }
}
