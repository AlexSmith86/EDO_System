using System.Net.Http.Json;

namespace EDO.Client.Services;

public class TmcRequestDto
{
    public int Id { get; set; }
    public int InitiatorUserId { get; set; }
    public string InitiatorName { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public int? CurrentStageId { get; set; }
    public string? CurrentStageName { get; set; }
    public string? CurrentStagePosition { get; set; }
    public string? CurrentStageDescription { get; set; }
    public int CurrentStageOrder { get; set; }
    public int TotalStages { get; set; }
    public int? WorkflowChainId { get; set; }
    public string? WorkflowChainName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TmcRequestItemDto> Items { get; set; } = new();
    public List<ApprovalHistoryDto> ApprovalHistory { get; set; } = new();
}

public class ApprovalHistoryDto
{
    public int Id { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string StagePosition { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public string DecisionDisplay { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TmcRequestItemDto
{
    public int Id { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public int? SubgroupId { get; set; }
    public string? SubgroupName { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public DateTime? PlannedDeliveryDate { get; set; }
    public string? InvoiceLink { get; set; }
    public string? Comment { get; set; }
    public string? InitiatorName { get; set; }
    public string? InitiatorPosition { get; set; }
}

public class CreateTmcRequestDto
{
    public string? ProjectName { get; set; }
    public int? WorkflowChainId { get; set; }
    public List<CreateTmcRequestItemDto> Items { get; set; } = new();
}

public class CreateTmcRequestItemDto
{
    public int? GroupId { get; set; }
    public int? SubgroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public DateTime? PlannedDeliveryDate { get; set; }
    public string? InvoiceLink { get; set; }
    public string? Comment { get; set; }
    public string? InitiatorName { get; set; }
    public string? InitiatorPosition { get; set; }
}

public class UpdateTmcRequestDto
{
    public string? ProjectName { get; set; }
    public List<CreateTmcRequestItemDto> Items { get; set; } = new();
}

public class SubmitDecisionDto
{
    public string Decision { get; set; } = string.Empty;
    public string? Comment { get; set; }
}

public interface ITmcRequestService
{
    Task<List<TmcRequestDto>> GetMyAsync();
    Task<List<TmcRequestDto>> GetPendingAsync();
    Task<TmcRequestDto?> GetByIdAsync(int id);
    Task<TmcRequestDto?> CreateAsync(CreateTmcRequestDto dto);
    Task<TmcRequestDto?> UpdateAsync(int id, UpdateTmcRequestDto dto);
    Task<TmcRequestDto?> SendAsync(int id);
    Task<TmcRequestDto?> SubmitDecisionAsync(int id, SubmitDecisionDto dto);
    Task DeleteAsync(int id);
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

    public async Task<List<TmcRequestDto>> GetPendingAsync()
    {
        return await _http.GetFromJsonAsync<List<TmcRequestDto>>("api/tmcrequests/pending") ?? new();
    }

    public async Task<TmcRequestDto?> SubmitDecisionAsync(int id, SubmitDecisionDto dto)
    {
        var response = await _http.PostAsJsonAsync($"api/tmcrequests/{id}/decision", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TmcRequestDto>();
    }

    public async Task DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/tmcrequests/{id}");
        response.EnsureSuccessStatusCode();
    }
}
