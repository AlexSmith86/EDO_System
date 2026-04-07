using EDO.Server.Data;
using EDO.Server.DTOs;
using EDO.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EDO.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        var users = await _db.Users
            .Include(u => u.Role)
            .Select(u => new UserDto
            {
                Id = u.Id,
                LastName = u.LastName,
                FirstName = u.FirstName,
                MiddleName = u.MiddleName,
                Position = u.Position,
                RoleId = u.RoleId,
                RoleName = u.Role.Name,
                Phone = u.Phone,
                TelegramId = u.TelegramId,
                Email = u.Email,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("positions")]
    public async Task<ActionResult<List<string>>> GetPositions()
    {
        var positions = await _db.Users
            .Where(u => u.IsActive && !string.IsNullOrEmpty(u.Position))
            .Select(u => u.Position)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();

        return Ok(positions);
    }

    [HttpPost]
    [Authorize(Roles = "Администратор")]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return Conflict(new { message = "Пользователь с таким Email уже существует." });

        var user = new User
        {
            LastName = dto.LastName,
            FirstName = dto.FirstName,
            MiddleName = dto.MiddleName,
            Position = dto.Position,
            RoleId = dto.RoleId,
            Phone = dto.Phone,
            TelegramId = dto.TelegramId,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await _db.Entry(user).Reference(u => u.Role).LoadAsync();

        return Created($"/api/users/{user.Id}", new UserDto
        {
            Id = user.Id,
            LastName = user.LastName,
            FirstName = user.FirstName,
            MiddleName = user.MiddleName,
            Position = user.Position,
            RoleId = user.RoleId,
            RoleName = user.Role.Name,
            TelegramId = user.TelegramId,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Администратор")]
    public async Task<ActionResult<UserDto>> Update(int id, [FromBody] UpdateUserDto dto)
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return NotFound(new { message = "Пользователь не найден." });

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id))
            return Conflict(new { message = "Пользователь с таким Email уже существует." });

        user.LastName = dto.LastName;
        user.FirstName = dto.FirstName;
        user.MiddleName = dto.MiddleName;
        user.Position = dto.Position;
        user.RoleId = dto.RoleId;
        user.Phone = dto.Phone;
        user.TelegramId = dto.TelegramId;
        user.Email = dto.Email;
        user.IsActive = dto.IsActive;

        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        await _db.SaveChangesAsync();

        await _db.Entry(user).Reference(u => u.Role).LoadAsync();

        return Ok(new UserDto
        {
            Id = user.Id,
            LastName = user.LastName,
            FirstName = user.FirstName,
            MiddleName = user.MiddleName,
            Position = user.Position,
            RoleId = user.RoleId,
            RoleName = user.Role.Name,
            TelegramId = user.TelegramId,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }
}
