namespace EDO.Server.Services;

/// <summary>Результат сохранения файла в локальное хранилище.</summary>
public record SavedFile(string OriginalName, string StoredName, string RelativeUrl, long Size);

/// <summary>Абстракция над локальным файловым хранилищем для вложений.
/// В текущей реализации файлы кладутся в {WebRootPath}/uploads/attachments,
/// имя генерируется как {Guid}{расширение} — чтобы два файла с одинаковыми именами
/// не затирали друг друга.</summary>
public interface IFileStorageService
{
    /// <summary>Сохранить загруженный файл и вернуть метаданные.
    /// Бросает <see cref="InvalidOperationException"/> при превышении лимита.</summary>
    Task<SavedFile> SaveAttachmentAsync(IFormFile file, CancellationToken ct = default);
}
