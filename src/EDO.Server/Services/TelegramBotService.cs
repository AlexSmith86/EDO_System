using EDO.Server.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace EDO.Server.Services;

public class TelegramBotService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly TelegramBotClient _bot;

    public TelegramBotService(IServiceProvider sp, IConfiguration config, ILogger<TelegramBotService> logger)
    {
        _sp = sp;
        _logger = logger;
        var token = config["Telegram:BotToken"]
            ?? throw new InvalidOperationException("Telegram:BotToken not configured");
        _bot = new TelegramBotClient(token);
    }

    /// <summary>Send a message and return its ID (so it can be deleted later).</summary>
    public async Task<int> SendMessageAsync(long chatId, string text)
    {
        var msg = await _bot.SendMessage(chatId, text, parseMode: ParseMode.Html);
        return msg.Id;
    }

    /// <summary>Delete a previously sent message.</summary>
    public async Task DeleteMessageAsync(long chatId, int messageId)
    {
        try { await _bot.DeleteMessage(chatId, messageId); }
        catch { /* message may already be deleted or too old */ }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var me = await _bot.GetMe(stoppingToken);
        _logger.LogInformation("Telegram bot @{BotUsername} started", me.Username);

        var opts = new ReceiverOptions { AllowedUpdates = [UpdateType.Message] };
        _bot.StartReceiving(HandleUpdate, HandleError, opts, stoppingToken);
    }

    private async Task HandleUpdate(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Message is not { Text: { } text } message) return;

        var chatId = message.Chat.Id;

        if (text.StartsWith("/start"))
        {
            await bot.SendMessage(chatId,
                "Добро пожаловать в ЭДО-бот!\n\n" +
                "Для привязки аккаунта отправьте свой <b>Email</b>, указанный в системе.",
                parseMode: ParseMode.Html, cancellationToken: ct);
            return;
        }

        if (text.Contains('@'))
        {
            await TryLinkAccount(bot, chatId, text.Trim(), ct);
            return;
        }

        await bot.SendMessage(chatId,
            "Отправьте свой Email для привязки аккаунта,\nили дождитесь кода при входе в систему.",
            cancellationToken: ct);
    }

    private async Task TryLinkAccount(ITelegramBotClient bot, long chatId, string email, CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);
        if (user is null)
        {
            await bot.SendMessage(chatId,
                "Пользователь с таким Email не найден в системе.\nПроверьте правильность ввода.",
                cancellationToken: ct);
            return;
        }

        user.TelegramId = chatId.ToString();
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Linked Telegram chatId {ChatId} to user {Email}", chatId, email);

        await bot.SendMessage(chatId,
            $"Аккаунт <b>{user.LastName} {user.FirstName}</b> привязан.\n" +
            "Теперь при входе в систему код подтверждения будет приходить сюда.",
            parseMode: ParseMode.Html, cancellationToken: ct);
    }

    private Task HandleError(ITelegramBotClient bot, Exception ex, HandleErrorSource source, CancellationToken ct)
    {
        _logger.LogError(ex, "Telegram bot error ({Source})", source);
        return Task.CompletedTask;
    }
}
