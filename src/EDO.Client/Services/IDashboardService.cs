using System.Net.Http.Json;

namespace EDO.Client.Services;

public class DashboardStatsDto
{
    public int AwaitingMyApprovalCount { get; set; }
    public int MyActiveRequestsCount { get; set; }
    public int TotalActiveRequestsCount { get; set; }
    public int ApprovedCount { get; set; }
    public List<StatusCountDto> RequestsByStatus { get; set; } = new();
}

public class StatusCountDto
{
    public string Status { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public int Count { get; set; }
}

public interface IDashboardService
{
    Task<DashboardStatsDto?> GetStatsAsync();
}

public class DashboardService : IDashboardService
{
    private readonly HttpClient _http;

    public DashboardService(HttpClient http)
    {
        _http = http;
    }

    public async Task<DashboardStatsDto?> GetStatsAsync()
    {
        return await _http.GetFromJsonAsync<DashboardStatsDto>("api/dashboard/stats");
    }
}
