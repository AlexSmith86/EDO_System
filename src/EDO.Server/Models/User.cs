namespace EDO.Server.Models;

public class User
{
    public int Id { get; set; }

    /// <summary>Фамилия</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Имя</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Отчество</summary>
    public string? MiddleName { get; set; }

    /// <summary>Должность</summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>Права доступа (определяются через роль)</summary>
    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;

    /// <summary>Telegram ID для уведомлений</summary>
    public string? TelegramId { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>Хэш пароля</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
