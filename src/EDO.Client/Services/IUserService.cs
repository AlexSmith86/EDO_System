using System.Net.Http.Json;

namespace EDO.Client.Services;

public class UserDto
{
    public int Id { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Position { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? TelegramId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserDto
{
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Position { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string? TelegramId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateUserDto
{
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Position { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string? TelegramId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public bool IsActive { get; set; } = true;
}

public interface IUserService
{
    Task<List<UserDto>> GetAllAsync();
    Task<UserDto?> CreateAsync(CreateUserDto dto);
    Task<UserDto?> UpdateAsync(int id, UpdateUserDto dto);
}

public class UserService : IUserService
{
    private readonly HttpClient _http;

    public UserService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var result = await _http.GetFromJsonAsync<List<UserDto>>("api/users");
        return result ?? new List<UserDto>();
    }

    public async Task<UserDto?> CreateAsync(CreateUserDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/users", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>();
    }

    public async Task<UserDto?> UpdateAsync(int id, UpdateUserDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/users/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>();
    }
}
