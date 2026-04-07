using System.Net.Http.Json;

namespace EDO.Client.Services;

public class TmcDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Article { get; set; }
    public string? ExternalId { get; set; }
    public decimal StockBalance { get; set; }
}

public class CreateTmcDto
{
    public string Name { get; set; } = string.Empty;
    public string? Article { get; set; }
    public string? ExternalId { get; set; }
    public decimal StockBalance { get; set; }
}

public class UpdateTmcDto
{
    public string Name { get; set; } = string.Empty;
    public string? Article { get; set; }
    public string? ExternalId { get; set; }
    public decimal StockBalance { get; set; }
}

public interface ITmcService
{
    Task<List<TmcDto>> GetAllAsync();
    Task<TmcDto?> CreateAsync(CreateTmcDto dto);
    Task<TmcDto?> UpdateAsync(int id, UpdateTmcDto dto);
    Task DeleteAsync(int id);
}

public class TmcService : ITmcService
{
    private readonly HttpClient _http;

    public TmcService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<TmcDto>> GetAllAsync()
    {
        return await _http.GetFromJsonAsync<List<TmcDto>>("api/tmcs") ?? new();
    }

    public async Task<TmcDto?> CreateAsync(CreateTmcDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/tmcs", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TmcDto>();
    }

    public async Task<TmcDto?> UpdateAsync(int id, UpdateTmcDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/tmcs/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TmcDto>();
    }

    public async Task DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/tmcs/{id}");
        response.EnsureSuccessStatusCode();
    }
}
