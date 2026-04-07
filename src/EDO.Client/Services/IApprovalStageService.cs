using System.Net.Http.Json;

namespace EDO.Client.Services;

public class ApprovalStageDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? RequiredPosition { get; set; }
    public int? RoleId { get; set; }
    public string? RoleName { get; set; }
    public int OrderSequence { get; set; }
}

public class CreateApprovalStageDto
{
    public string Name { get; set; } = string.Empty;
    public string? RequiredPosition { get; set; }
    public int? RoleId { get; set; }
    public int OrderSequence { get; set; }
}

public class UpdateApprovalStageDto
{
    public string Name { get; set; } = string.Empty;
    public string? RequiredPosition { get; set; }
    public int? RoleId { get; set; }
    public int OrderSequence { get; set; }
}

public interface IApprovalStageService
{
    Task<List<ApprovalStageDto>> GetAllAsync();
    Task<ApprovalStageDto?> CreateAsync(CreateApprovalStageDto dto);
    Task<ApprovalStageDto?> UpdateAsync(int id, UpdateApprovalStageDto dto);
}

public class ApprovalStageService : IApprovalStageService
{
    private readonly HttpClient _http;

    public ApprovalStageService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<ApprovalStageDto>> GetAllAsync()
    {
        return await _http.GetFromJsonAsync<List<ApprovalStageDto>>("api/approvalstages") ?? new();
    }

    public async Task<ApprovalStageDto?> CreateAsync(CreateApprovalStageDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/approvalstages", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ApprovalStageDto>();
    }

    public async Task<ApprovalStageDto?> UpdateAsync(int id, UpdateApprovalStageDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/approvalstages/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ApprovalStageDto>();
    }
}
