using System.Net.Http.Json;

namespace EDO.Client.Services;

public class TemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ProcessType { get; set; } = string.Empty;
}

public class CreateTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ProcessType { get; set; } = string.Empty;
}

public class UpdateTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ProcessType { get; set; } = string.Empty;
}

public interface ITemplateService
{
    Task<List<TemplateDto>> GetAllAsync();
    Task<TemplateDto?> CreateAsync(CreateTemplateDto dto);
    Task<TemplateDto?> UpdateAsync(int id, UpdateTemplateDto dto);
}

public class TemplateService : ITemplateService
{
    private readonly HttpClient _http;

    public TemplateService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<TemplateDto>> GetAllAsync()
    {
        return await _http.GetFromJsonAsync<List<TemplateDto>>("api/templates") ?? new();
    }

    public async Task<TemplateDto?> CreateAsync(CreateTemplateDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/templates", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TemplateDto>();
    }

    public async Task<TemplateDto?> UpdateAsync(int id, UpdateTemplateDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/templates/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TemplateDto>();
    }
}
