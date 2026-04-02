using EDO.Server.Models;

namespace EDO.Server.Services;

public class WorkflowDecisionRequest
{
    public int DocumentId { get; set; }
    public int UserId { get; set; }
    public int CurrentStageId { get; set; }
    public Decision Decision { get; set; }
    public string? Comment { get; set; }
}

public class WorkflowDecisionResult
{
    public bool Success { get; set; }
    public int? NextStageId { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsRejected { get; set; }
    public string Message { get; set; } = string.Empty;
}

public interface IWorkflowEngineService
{
    Task<WorkflowDecisionResult> ProcessDecisionAsync(WorkflowDecisionRequest request);
}
