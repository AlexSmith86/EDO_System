using EDO.Server.Data;
using EDO.Server.DTOs;
using EDO.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EDO.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AppDbContext db,
        IMemoryCache cache,
        JwtService jwtService,
        ILogger<AuthController> logger)
    {
        _db = db;
        _cache = cache;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// Шаг 1 авторизации: проверка Email + пароль, отправка 2FA-кода.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user is null)
            return Unauthorized(new { message = "Пользователь не найден или деактивирован." });

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Неверный пароль." });

        var code = "111111";

        var cacheKey = $"2fa_{request.Email}";
        _cache.Set(cacheKey, code, TimeSpan.FromMinutes(5));

        // Пока бота нет — выводим код в консоль
        _logger.LogInformation(
            "2FA-код для {Email}: {Code} (TelegramId: {TelegramId})",
            request.Email, code, user.TelegramId ?? "не указан");

        return Ok(new { message = "Код подтверждения отправлен. Проверьте консоль/Telegram." });
    }

    /// <summary>
    /// Шаг 2 авторизации: проверка 2FA-кода, выдача JWT-токена.
    /// </summary>
    [HttpPost("verify-2fa")]
    public async Task<IActionResult> Verify2Fa([FromBody] Verify2FaRequest request)
    {
        var cacheKey = $"2fa_{request.Email}";

        if (!_cache.TryGetValue(cacheKey, out string? cachedCode))
            return BadRequest(new { message = "Код истёк или не был запрошен." });

        if (cachedCode != request.Code)
            return Unauthorized(new { message = "Неверный код подтверждения." });

        _cache.Remove(cacheKey);

        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user is null)
            return Unauthorized(new { message = "Пользователь не найден." });

        var token = _jwtService.GenerateToken(user);

        return Ok(new TokenResponse { Token = token });
    }
}
