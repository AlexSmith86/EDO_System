using System.Net.Http.Json;

namespace EDO.Client.Services;

public class TmcRequestDto
{
    public int Id { get; set; }
    public int InitiatorUserId { get; set; }
    public string InitiatorName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? CurrentStageId { get; set; }
    public string? CurrentStageName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TmcRequestItemDto> Items { get; set; } = new();
}

public class TmcRequestItemDto
{
    public int Id { get; set; }
    public int TmcId { get; set; }
    public string TmcName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

public class CreateTmcRequestDto
{
    public List<CreateTmcRequestItemDto> Items { get; set; } = new();
}

public class CreateTmcRequestItemDto
{
    public int TmcId { get; set; }
    public decimal Quantity { get; set; }
}

public class UpdateTmcRequestDto
{
    public List<CreateTmcRequestItemDto> Items { get; set; } = new();
}

public interface ITmcRequestService
{
    Task<List<TmcRequestDto>> GetMyAsync();
    Task<TmcRequestDto?> GetByIdAsync(int id);
    Task<TmcRequestDto?> CreateAsync(CreateTmcRequestDto dto);
    Task<TmcRequestDto?> UpdateAsync(int id, UpdateTmcRequestDto dto);
    Task<TmcRequestDto?> SendAsync(int id);
}

public class TmcRequestService : ITmcRequestService
{
    private readonly HttpClient _http;

    public TmcRequestService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<TmcRequestDto>> GetMyAsync()
    {
        return await _http.GetFromJsonAsync<List<TmcRequestDto>>("api/tmcrequests/my") ?? new();
    }

    public async Task<TmcRequestDto?> GetByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<TmcRequestDto>($"api/tmcrequests/{id}");
    }

    public async Task<TmcRequestDto?> CreateAsync(CreateTmcRequestDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/tmcrequests", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TmcRequestDto>();
    }

    public async Task<TmcRequestDto?> UpdateAsync(int id, UpdateTmcRequestDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/tmcrequests/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TmcRequestDto>();
    }

    public async Task<TmcRequestDto?> SendAsync(int id)
    {
        var response = await _http.PostAsync($"api/tmcrequests/{id}/send", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TmcRequestDto>();
    }
}
