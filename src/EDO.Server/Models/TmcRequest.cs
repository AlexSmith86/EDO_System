namespace EDO.Server.Models;

/// <summary>Заявка на ТМЦ</summary>
public class TmcRequest
{
    public int Id { get; set; }

    /// <summary>Инициатор заявки</summary>
    public int InitiatorUserId { get; set; }

    public User InitiatorUser { get; set; } = null!;

    /// <summary>Статус заявки</summary>
    public TmcRequestStatus Status { get; set; } = TmcRequestStatus.Draft;

    /// <summary>Текущий этап согласования (null для Draft)</summary>
    public int? CurrentStageId { get; set; }

    public ApprovalStage? CurrentStage { get; set; }

    /// <summary>Дата создания</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Табличная часть заявки</summary>
    public List<TmcRequestItem> Items { get; set; } = new();
}

public enum TmcRequestStatus
{
    Draft,
    InApproval,
    Approved,
    Rejected
}
