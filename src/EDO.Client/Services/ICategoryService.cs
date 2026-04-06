using System.Net.Http.Json;

namespace EDO.Client.Services;

public class TmcGroupDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName => $"{Code}. {Name}";
}

public class TmcSubgroupDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public bool IsHeader { get; set; }
    public string DisplayName => IsHeader ? Name : $"{Code}. {Name}";
}

public interface ICategoryService
{
    Task<List<TmcGroupDto>> GetGroupsAsync();
    Task<List<TmcSubgroupDto>> GetSubgroupsAsync(int groupId);
}

public class CategoryService : ICategoryService
{
    private readonly HttpClient _http;

    public CategoryService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<TmcGroupDto>> GetGroupsAsync()
    {
        return await _http.GetFromJsonAsync<List<TmcGroupDto>>("api/categories/groups") ?? new();
    }

    public async Task<List<TmcSubgroupDto>> GetSubgroupsAsync(int groupId)
    {
        return await _http.GetFromJsonAsync<List<TmcSubgroupDto>>($"api/categories/groups/{groupId}/subgroups") ?? new();
    }
}
