using System.Net.Http.Headers;

namespace EDO.Client.Services;

public class AuthHttpHandler : DelegatingHandler
{
    private readonly LocalStorageService _localStorage;

    public AuthHttpHandler(LocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? token = null;
        try
        {
            token = await _localStorage.GetItemAsync("authToken");
        }
        catch
        {
            // Ignore storage access issues; request will be sent without auth header.
        }
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await _localStorage.RemoveItemAsync("authToken");
        }

        return response;
    }
}
