namespace EDO.Server.Models;

/// <summary>История действий по документу</summary>
public class ActionHistory
{
    public int Id { get; set; }

    /// <summary>ID документа (универсальный, без привязки к конкретному типу)</summary>
    public int DocumentId { get; set; }

    /// <summary>Пользователь, принявший решение</summary>
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>Принятое решение</summary>
    public Decision Decision { get; set; }

    /// <summary>Комментарий к решению</summary>
    public string? Comment { get; set; }

    /// <summary>Этап стандартного маршрута (null для кастомных цепочек)</summary>
    public int? StageId { get; set; }

    public ApprovalStage? Stage { get; set; }

    /// <summary>Шаг кастомной цепочки (null для стандартного маршрута)</summary>
    public int? WorkflowStepId { get; set; }

    public WorkflowStep? WorkflowStep { get; set; }

    /// <summary>Оригинальное имя файла, прикреплённого к этому действию
    /// (например, «Счёт от поставщика.pdf»). Отображается в таймлайне истории.</summary>
    public string? AttachedFileName { get; set; }

    /// <summary>Относительный URL файла внутри wwwroot
    /// (например, «/uploads/attachments/{guid}.pdf»).
    /// По этому URL файл доступен для скачивания через статик-сервис ASP.NET.</summary>
    public string? AttachedFileUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum Decision
{
    Approved,
    Rejected,
    Reviewed
}
