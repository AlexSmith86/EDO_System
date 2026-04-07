using System.Net.Http.Json;
using System.Text.Json;

namespace EDO.Client.Services;

public class WorkflowChainDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<WorkflowStepDto> Steps { get; set; } = new();
}

public class WorkflowStepDto
{
    public int Id { get; set; }
    public int WorkflowChainId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string TargetPosition { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class CreateWorkflowChainDto
{
    public string Name { get; set; } = string.Empty;
    public List<CreateWorkflowStepDto> Steps { get; set; } = new();
}

public class CreateWorkflowStepDto
{
    public string StepName { get; set; } = string.Empty;
    public string TargetPosition { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class UpdateWorkflowChainDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<CreateWorkflowStepDto> Steps { get; set; } = new();
}

public interface IWorkflowChainService
{
    Task<List<WorkflowChainDto>> GetAllAsync();
    Task<List<WorkflowChainDto>> GetActiveAsync();
    Task<WorkflowChainDto?> GetByIdAsync(int id);
    Task<WorkflowChainDto?> CreateAsync(CreateWorkflowChainDto dto);
    Task<WorkflowChainDto?> UpdateAsync(int id, UpdateWorkflowChainDto dto);
    Task DeleteAsync(int id);
    Task<List<string>> GetPositionsAsync();
}

public class WorkflowChainService : IWorkflowChainService
{
    private readonly HttpClient _http;

    public WorkflowChainService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<WorkflowChainDto>> GetAllAsync()
    {
        return await _http.GetFromJsonAsync<List<WorkflowChainDto>>("api/workflowchains") ?? new();
    }

    public async Task<List<WorkflowChainDto>> GetActiveAsync()
    {
        return await _http.GetFromJsonAsync<List<WorkflowChainDto>>("api/workflowchains/active") ?? new();
    }

    public async Task<WorkflowChainDto?> GetByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<WorkflowChainDto>($"api/workflowchains/{id}");
    }

    public async Task<WorkflowChainDto?> CreateAsync(CreateWorkflowChainDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/workflowchains", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkflowChainDto>();
    }

    public async Task<WorkflowChainDto?> UpdateAsync(int id, UpdateWorkflowChainDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/workflowchains/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkflowChainDto>();
    }

    public async Task DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/workflowchains/{id}");
        if (response.IsSuccessStatusCode)
            return;

        var msg = await ReadApiErrorMessageAsync(response);
        throw new HttpRequestException(msg ?? $"{(int)response.StatusCode} {response.ReasonPhrase}");
    }

    private static async Task<string?> ReadApiErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            if (doc.RootElement.TryGetProperty("message", out var m))
                return m.GetString();
        }
        catch
        {
            // ignore
        }

        return null;
    }

    public async Task<List<string>> GetPositionsAsync()
    {
        return await _http.GetFromJsonAsync<List<string>>("api/users/positions") ?? new();
    }
}
