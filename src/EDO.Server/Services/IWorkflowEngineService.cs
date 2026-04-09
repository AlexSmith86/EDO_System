using EDO.Server.Models;

namespace EDO.Server.Services;

public class WorkflowDecisionRequest
{
    public int DocumentId { get; set; }
    public int UserId { get; set; }
    public int CurrentStageId { get; set; }
    public Decision Decision { get; set; }
    public string? Comment { get; set; }

    /// <summary>Если задан — используется кастомная цепочка</summary>
    public int? WorkflowChainId { get; set; }

    /// <summary>Текущий шаг кастомной цепочки</summary>
    public int? CurrentWorkflowStepId { get; set; }

    /// <summary>Целевой ApprovalStage для возврата при отклонении (стандартный маршрут).
    /// Если null — возврат к инициатору (OrderSequence = 0).</summary>
    public int? TargetStageId { get; set; }

    /// <summary>Целевой WorkflowStep для возврата при отклонении (кастомная цепочка).
    /// Если null — возврат к инициатору (CurrentWorkflowStepId = null).</summary>
    public int? TargetWorkflowStepId { get; set; }
}

public class WorkflowDecisionResult
{
    public bool Success { get; set; }
    public int? NextStageId { get; set; }
    public int? NextWorkflowStepId { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsRejected { get; set; }

    /// <summary>true — при отклонении возврат направлен к инициатору
    /// (OrderSequence = 0 для стандарта либо CurrentWorkflowStepId = null для кастома).</summary>
    public bool IsReturnToInitiator { get; set; }

    public string Message { get; set; } = string.Empty;
}

public interface IWorkflowEngineService
{
    Task<WorkflowDecisionResult> ProcessDecisionAsync(WorkflowDecisionRequest request);

    /// <summary>Проверяет, может ли пользователь действовать на текущем этапе заявки</summary>
    Task<bool> CanUserActAsync(int userId, int stageId, int documentId);
}
