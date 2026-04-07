using System.Net.Http.Json;

namespace EDO.Client.Services;

public class ContractorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Inn { get; set; }
    public string ContractorType { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
}

public class CreateContractorDto
{
    public string Name { get; set; } = string.Empty;
    public string? Inn { get; set; }
    public string ContractorType { get; set; } = "Supplier";
    public string? ExternalId { get; set; }
}

public class UpdateContractorDto
{
    public string Name { get; set; } = string.Empty;
    public string? Inn { get; set; }
    public string ContractorType { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
}

public interface IContractorService
{
    Task<List<ContractorDto>> GetAllAsync();
    Task<ContractorDto?> CreateAsync(CreateContractorDto dto);
    Task<ContractorDto?> UpdateAsync(int id, UpdateContractorDto dto);
    Task DeleteAsync(int id);
}

public class ContractorService : IContractorService
{
    private readonly HttpClient _http;

    public ContractorService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<ContractorDto>> GetAllAsync()
    {
        return await _http.GetFromJsonAsync<List<ContractorDto>>("api/contractors") ?? new();
    }

    public async Task<ContractorDto?> CreateAsync(CreateContractorDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/contractors", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContractorDto>();
    }

    public async Task<ContractorDto?> UpdateAsync(int id, UpdateContractorDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/contractors/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContractorDto>();
    }

    public async Task DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/contractors/{id}");
        response.EnsureSuccessStatusCode();
    }
}
