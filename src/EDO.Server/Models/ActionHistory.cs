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

    /// <summary>Этап, на котором было принято решение</summary>
    public int StageId { get; set; }

    public ApprovalStage Stage { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum Decision
{
    Approved,
    Rejected,
    Reviewed
}
