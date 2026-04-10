using Microsoft.Extensions.Logging;

namespace EDO.Server.Services;

/// <summary>Локальное файловое хранилище. Пишет в
/// {WebRootPath}/uploads/attachments и возвращает относительный URL
/// «/uploads/attachments/{guid}{ext}», по которому файл отдаётся
/// через <c>app.UseStaticFiles()</c>.</summary>
public class LocalFileStorageService : IFileStorageService
{
    // Жёсткий потолок размера одного вложения — 50 МБ.
    // Должен совпадать с лимитами Kestrel/FormOptions в Program.cs.
    public const long MaxFileSizeBytes = 50L * 1024 * 1024;

    private const string RelativeFolder = "uploads/attachments";

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IWebHostEnvironment env, ILogger<LocalFileStorageService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<SavedFile> SaveAttachmentAsync(IFormFile file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            throw new InvalidOperationException("Пустой файл.");

        if (file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException(
                $"Файл слишком большой: {file.Length / (1024 * 1024)} МБ. Максимум — 50 МБ.");

        // WebRootPath может быть null, если в проекте физически нет папки wwwroot.
        // В этом случае используем ContentRootPath/wwwroot.
        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
        }

        var targetDir = Path.Combine(webRoot, RelativeFolder);
        Directory.CreateDirectory(targetDir);

        // Безопасное имя: Guid + расширение из оригинала.
        // Никогда не доверяем имени файла от пользователя в имени на диске.
        var extension = Path.GetExtension(file.FileName);
        if (!string.IsNullOrEmpty(extension) && extension.Length > 20)
            extension = extension[..20]; // защита от экстремально длинных «расширений»

        var storedName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(targetDir, storedName);

        await using (var stream = new FileStream(
            fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            bufferSize: 81920, useAsync: true))
        {
            await file.CopyToAsync(stream, ct);
        }

        // Возвращаем относительный URL (именно со слэшем в начале),
        // чтобы клиент мог открыть/скачать без знания абсолютного хоста.
        var relativeUrl = $"/{RelativeFolder}/{storedName}";

        _logger.LogInformation(
            "File saved: original={Original}, stored={Stored}, size={Size} bytes, url={Url}",
            file.FileName, storedName, file.Length, relativeUrl);

        return new SavedFile(
            OriginalName: Path.GetFileName(file.FileName),
            StoredName: storedName,
            RelativeUrl: relativeUrl,
            Size: file.Length);
    }
}
