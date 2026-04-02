using System.Net.Http.Json;

namespace EDO.Client.Services;

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public interface IRoleService
{
    Task<List<RoleDto>> GetAllAsync();
}

public class RoleService : IRoleService
{
    private readonly HttpClient _http;

    public RoleService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<RoleDto>> GetAllAsync()
    {
        var result = await _http.GetFromJsonAsync<List<RoleDto>>("api/roles");
        return result ?? new List<RoleDto>();
    }
}
