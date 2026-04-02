using EDO.Server.Data;
using EDO.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace EDO.Server.Services;

public class WorkflowEngineService : IWorkflowEngineService
{
    private readonly AppDbContext _db;

    public WorkflowEngineService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<WorkflowDecisionResult> ProcessDecisionAsync(WorkflowDecisionRequest request)
    {
        var currentStage = await _db.ApprovalStages
            .FirstOrDefaultAsync(s => s.Id == request.CurrentStageId);

        if (currentStage is null)
        {
            return new WorkflowDecisionResult
            {
                Success = false,
                Message = $"Этап согласования с Id={request.CurrentStageId} не найден."
            };
        }

        // Записываем решение в историю
        var historyEntry = new ActionHistory
        {
            DocumentId = request.DocumentId,
            UserId = request.UserId,
            StageId = request.CurrentStageId,
            Decision = request.Decision,
            Comment = request.Comment,
            CreatedAt = DateTime.UtcNow
        };

        _db.ActionHistories.Add(historyEntry);
        await _db.SaveChangesAsync();

        // Обработка решения
        if (request.Decision == Decision.Rejected)
        {
            return new WorkflowDecisionResult
            {
                Success = true,
                IsRejected = true,
                IsCompleted = false,
                NextStageId = null,
                Message = "Документ отклонён и возвращён инициатору."
            };
        }

        // Approved или Reviewed — ищем следующий этап
        var nextStage = await _db.ApprovalStages
            .Where(s => s.OrderSequence > currentStage.OrderSequence)
            .OrderBy(s => s.OrderSequence)
            .FirstOrDefaultAsync();

        if (nextStage is null)
        {
            // Последний этап — процесс завершён
            return new WorkflowDecisionResult
            {
                Success = true,
                IsCompleted = true,
                IsRejected = false,
                NextStageId = null,
                Message = "Документ полностью согласован."
            };
        }

        return new WorkflowDecisionResult
        {
            Success = true,
            NextStageId = nextStage.Id,
            IsCompleted = false,
            IsRejected = false,
            Message = $"Документ передан на этап: {nextStage.Name}."
        };
    }
}
