namespace EDO.Server.Models;

/// <summary>Заявка на ТМЦ</summary>
public class TmcRequest
{
    public int Id { get; set; }

    /// <summary>Инициатор заявки</summary>
    public int InitiatorUserId { get; set; }

    public User InitiatorUser { get; set; } = null!;

    /// <summary>Проект</summary>
    public string? ProjectName { get; set; }

    /// <summary>Статус заявки</summary>
    public TmcRequestStatus Status { get; set; } = TmcRequestStatus.Draft;

    /// <summary>Текущий этап согласования (null для Draft/Completed)</summary>
    public int? CurrentStageId { get; set; }

    public ApprovalStage? CurrentStage { get; set; }

    /// <summary>Кастомная цепочка согласования (null = стандартный маршрут ТМЦ)</summary>
    public int? WorkflowChainId { get; set; }

    public WorkflowChain? WorkflowChain { get; set; }

    /// <summary>Текущий шаг кастомной цепочки (null если стандартный маршрут)</summary>
    public int? CurrentWorkflowStepId { get; set; }

    public WorkflowStep? CurrentWorkflowStep { get; set; }

    /// <summary>Дата создания</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Табличная часть заявки</summary>
    public List<TmcRequestItem> Items { get; set; } = new();
}

public enum TmcRequestStatus
{
    /// <summary>Черновик</summary>
    Draft,
    /// <summary>На согласовании</summary>
    InApproval,
    /// <summary>На доработке (отклонено, возвращено инициатору)</summary>
    Rework,
    /// <summary>Выполнено / Закрыто</summary>
    Completed,
    /// <summary>Согласовано (legacy)</summary>
    Approved,
    /// <summary>Отклонено (legacy)</summary>
    Rejected
}
