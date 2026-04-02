using System.ComponentModel.DataAnnotations;

namespace EDO.Server.DTOs;

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
    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? MiddleName { get; set; }

    [Required, MaxLength(200)]
    public string Position { get; set; } = string.Empty;

    [Required]
    public int RoleId { get; set; }

    [MaxLength(100)]
    public string? TelegramId { get; set; }

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

public class UpdateUserDto
{
    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? MiddleName { get; set; }

    [Required, MaxLength(200)]
    public string Position { get; set; } = string.Empty;

    [Required]
    public int RoleId { get; set; }

    [MaxLength(100)]
    public string? TelegramId { get; set; }

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    public string? Password { get; set; }

    public bool IsActive { get; set; } = true;
}
