namespace EDO.Server.Models;

/// <summary>Этап согласования</summary>
public class ApprovalStage
{
    public int Id { get; set; }

    /// <summary>Название этапа</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Требуемая должность (Position) для согласования.
    /// "Инициатор" — особое значение: проверяется по InitiatorUserId заявки.</summary>
    public string RequiredPosition { get; set; } = string.Empty;

    /// <summary>Описание действий на этапе</summary>
    public string? Description { get; set; }

    /// <summary>Порядковый номер этапа в цепочке (0-9)</summary>
    public int OrderSequence { get; set; }

    /// <summary>Устаревшее поле, не используется в маршрутизации</summary>
    public int? RoleId { get; set; }

    public Role? Role { get; set; }
}
