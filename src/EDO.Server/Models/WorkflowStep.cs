namespace EDO.Server.Models;

/// <summary>Шаг кастомной цепочки согласования</summary>
public class WorkflowStep
{
    public int Id { get; set; }

    /// <summary>Цепочка, к которой относится шаг</summary>
    public int WorkflowChainId { get; set; }

    public WorkflowChain WorkflowChain { get; set; } = null!;

    /// <summary>Название шага</summary>
    public string StepName { get; set; } = string.Empty;

    /// <summary>Требуемая должность (из User.Position)</summary>
    public string TargetPosition { get; set; } = string.Empty;

    /// <summary>Порядковый номер шага в цепочке (1, 2, 3...)</summary>
    public int Order { get; set; }
}
