using System.Security.Cryptography;
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
    private readonly TelegramBotService _telegramBot;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AppDbContext db,
        IMemoryCache cache,
        JwtService jwtService,
        TelegramBotService telegramBot,
        ILogger<AuthController> logger)
    {
        _db = db;
        _cache = cache;
        _jwtService = jwtService;
        _telegramBot = telegramBot;
        _logger = logger;
    }

    /// <summary>
    /// Шаг 1 авторизации: проверка Email + пароль, отправка 2FA-кода в Telegram.
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

        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        var codeKey = $"2fa_{request.Email}";
        var msgKey = $"2fa_msg_{request.Email}";
        _cache.Set(codeKey, code, TimeSpan.FromMinutes(5));

        if (!string.IsNullOrWhiteSpace(user.TelegramId) && long.TryParse(user.TelegramId, out var chatId))
        {
            try
            {
                var messageId = await _telegramBot.SendMessageAsync(chatId,
                    $"🔐 <b>Код входа в ЭДО:</b> <code>{code}</code>\n\n" +
                    "Код действителен 5 минут.");
                _cache.Set(msgKey, (chatId, messageId), TimeSpan.FromMinutes(5));
                _logger.LogInformation("2FA code sent to Telegram for {Email}", request.Email);
                return Ok(new { message = "Код подтверждения отправлен в Telegram." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send Telegram message to {Email}, falling back to log", request.Email);
            }
        }

        _logger.LogInformation("2FA-код для {Email}: {Code} (Telegram не привязан)", request.Email, code);
        return Ok(new { message = "Telegram не привязан. Код: " + code });
    }

    /// <summary>
    /// Шаг 2 авторизации: проверка 2FA-кода, выдача JWT-токена.
    /// </summary>
    [HttpPost("verify-2fa")]
    public async Task<IActionResult> Verify2Fa([FromBody] Verify2FaRequest request)
    {
        var codeKey = $"2fa_{request.Email}";
        var msgKey = $"2fa_msg_{request.Email}";

        if (!_cache.TryGetValue(codeKey, out string? cachedCode))
            return BadRequest(new { message = "Код истёк или не был запрошен." });

        if (cachedCode != request.Code)
            return Unauthorized(new { message = "Неверный код подтверждения." });

        _cache.Remove(codeKey);

        // Delete the OTP message from Telegram
        if (_cache.TryGetValue(msgKey, out (long chatId, int messageId) msg))
        {
            _cache.Remove(msgKey);
            _ = _telegramBot.DeleteMessageAsync(msg.chatId, msg.messageId);
        }

        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user is null)
            return Unauthorized(new { message = "Пользователь не найден." });

        var token = _jwtService.GenerateToken(user);

        return Ok(new TokenResponse { Token = token });
    }
}
