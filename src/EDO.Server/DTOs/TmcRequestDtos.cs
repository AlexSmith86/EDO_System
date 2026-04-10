using System.ComponentModel.DataAnnotations;

namespace EDO.Server.DTOs;

public class TmcRequestDto
{
    public int Id { get; set; }
    public int InitiatorUserId { get; set; }
    public string InitiatorName { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public int? CurrentStageId { get; set; }
    public string? CurrentStageName { get; set; }
    public string? CurrentStagePosition { get; set; }
    public string? CurrentStageDescription { get; set; }
    public int CurrentStageOrder { get; set; }
    public int TotalStages { get; set; }
    public int? WorkflowChainId { get; set; }
    public string? WorkflowChainName { get; set; }

    /// <summary>Назначенный ответственный (делегирование) — Id пользователя.</summary>
    public int? ResponsibleUserId { get; set; }

    /// <summary>ФИО назначенного ответственного.</summary>
    public string? ResponsibleUserName { get; set; }

    /// <summary>Должность назначенного ответственного.</summary>
    public string? ResponsibleUserPosition { get; set; }

    public DateTime CreatedAt { get; set; }
    public List<TmcRequestItemDto> Items { get; set; } = new();
    public List<ApprovalHistoryDto> ApprovalHistory { get; set; } = new();
}

/// <summary>Назначить ответственного за заявку (делегирование на текущем этапе).</summary>
public class AssignResponsibleDto
{
    /// <summary>Id пользователя, которому делегируется работа с заявкой.
    /// Передайте null, чтобы снять текущего ответственного.</summary>
    public int? TargetUserId { get; set; }
}

public class ApprovalHistoryDto
{
    public int Id { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string StagePosition { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public string DecisionDisplay { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    /// <summary>Должность пользователя, совершившего действие.</summary>
    public string? UserPosition { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TmcRequestItemDto
{
    public int Id { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public int? SubgroupId { get; set; }
    public string? SubgroupName { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal? Price { get; set; }
    public DateTime? PlannedDeliveryDate { get; set; }
    public string? InvoiceLink { get; set; }
    public string? Comment { get; set; }
    public string? InitiatorName { get; set; }
    public string? InitiatorPosition { get; set; }
}

public class CreateTmcRequestDto
{
    public string? ProjectName { get; set; }
    public int? WorkflowChainId { get; set; }
    public List<CreateTmcRequestItemDto> Items { get; set; } = new();
}

public class CreateTmcRequestItemDto
{
    public int? GroupId { get; set; }
    public int? SubgroupId { get; set; }

    [Required, MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    [Range(0.0001, double.MaxValue, ErrorMessage = "Количество должно быть больше 0")]
    public decimal Quantity { get; set; }

    [MaxLength(50)]
    public string? Unit { get; set; }

    public decimal? Price { get; set; }

    public DateTime? PlannedDeliveryDate { get; set; }

    [MaxLength(1000)]
    public string? InvoiceLink { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    [MaxLength(300)]
    public string? InitiatorName { get; set; }

    [MaxLength(300)]
    public string? InitiatorPosition { get; set; }
}

public class UpdateTmcRequestDto
{
    public string? ProjectName { get; set; }
    public List<CreateTmcRequestItemDto> Items { get; set; } = new();
}

public class SubmitDecisionDto
{
    [Required]
    public string Decision { get; set; } = string.Empty;

    public string? Comment { get; set; }

    /// <summary>Целевой ApprovalStage для возврата при отклонении (стандартный маршрут).
    /// Если null — возврат к инициатору (OrderSequence = 0).</summary>
    public int? TargetStageId { get; set; }

    /// <summary>Целевой WorkflowStep для возврата при отклонении (кастомная цепочка).
    /// Если null — возврат к инициатору (CurrentWorkflowStepId = null, статус Rework).</summary>
    public int? TargetWorkflowStepId { get; set; }
}

/// <summary>Параметры отправки заявки на согласование. Позволяет инициатору
/// выбрать стартовый этап/шаг (перепрыгнуть вперёд).</summary>
public class SendRequestDto
{
    /// <summary>Целевой ApprovalStage для старта (стандартный маршрут).
    /// Null — стандартное поведение (OrderSequence = 1).</summary>
    public int? TargetStageId { get; set; }

    /// <summary>Целевой WorkflowStep для старта (кастомная цепочка).
    /// Null — стандартное поведение (первый шаг цепочки по Order).</summary>
    public int? TargetWorkflowStepId { get; set; }
}

/// <summary>Возможный целевой этап/шаг для движения заявки ВПЕРЁД
/// (инициатор выбирает стартовый этап либо согласующий выбирает следующий).</summary>
public class ForwardTargetDto
{
    /// <summary>Id ApprovalStage (стандартный маршрут)</summary>
    public int? StageId { get; set; }

    /// <summary>Id WorkflowStep (кастомная цепочка)</summary>
    public int? WorkflowStepId { get; set; }

    /// <summary>Название этапа/шага</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Требуемая должность</summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>Описание этапа (только для стандартного маршрута)</summary>
    public string? Description { get; set; }

    /// <summary>Порядковый номер этапа/шага</summary>
    public int Order { get; set; }

    /// <summary>true — этот этап/шаг является «следующим по умолчанию» для текущего состояния</summary>
    public bool IsDefault { get; set; }
}

/// <summary>Возможный целевой этап/шаг для возврата заявки при отклонении</summary>
public class ReturnTargetDto
{
    /// <summary>Id ApprovalStage (стандартный маршрут). Null для шагов кастомной цепочки и
    /// для синтетической записи «Инициатор» в кастомной цепочке.</summary>
    public int? StageId { get; set; }

    /// <summary>Id WorkflowStep (кастомная цепочка). Null для стандартного маршрута и для
    /// синтетической записи «Инициатор» в кастомной цепочке.</summary>
    public int? WorkflowStepId { get; set; }

    /// <summary>Название этапа/шага</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Требуемая должность</summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>Порядковый номер этапа/шага (для сортировки)</summary>
    public int Order { get; set; }

    /// <summary>true — это возврат к инициатору (пункт «Инициатор»)</summary>
    public bool IsInitiator { get; set; }
}
