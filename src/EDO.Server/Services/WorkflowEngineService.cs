using EDO.Server.Data;
using EDO.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace EDO.Server.Services;

public class WorkflowEngineService : IWorkflowEngineService
{
    private readonly AppDbContext _db;

    /// <summary>Особое значение RequiredPosition — означает инициатора заявки</summary>
    private const string InitiatorPosition = "Инициатор";

    public WorkflowEngineService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> CanUserActAsync(int userId, int stageId, int documentId)
    {
        var stage = await _db.ApprovalStages.FindAsync(stageId);
        if (stage is null) return false;

        var user = await _db.Users.FindAsync(userId);
        if (user is null) return false;

        // Для этапов "Инициатор" — проверяем, что пользователь является инициатором заявки
        if (stage.RequiredPosition == InitiatorPosition)
        {
            var request = await _db.TmcRequests.FindAsync(documentId);
            return request is not null && request.InitiatorUserId == userId;
        }

        // Для остальных этапов — проверяем совпадение должности
        return string.Equals(user.Position, stage.RequiredPosition, StringComparison.OrdinalIgnoreCase);
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

        // Проверяем право пользователя действовать на этом этапе
        if (!await CanUserActAsync(request.UserId, request.CurrentStageId, request.DocumentId))
        {
            return new WorkflowDecisionResult
            {
                Success = false,
                Message = "У вас нет прав для согласования на данном этапе. " +
                          $"Требуемая должность: {currentStage.RequiredPosition}."
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

        // --- ОТКЛОНЕНИЕ ---
        // Заявка возвращается на Этап 0 (к Инициатору) со статусом "На доработку"
        if (request.Decision == Decision.Rejected)
        {
            var stageZero = await _db.ApprovalStages
                .Where(s => s.OrderSequence == 0)
                .FirstOrDefaultAsync();

            return new WorkflowDecisionResult
            {
                Success = true,
                IsRejected = true,
                IsCompleted = false,
                NextStageId = stageZero?.Id,
                Message = "Заявка отклонена и возвращена инициатору на доработку."
            };
        }

        // --- ОДОБРЕНИЕ ---
        // Ищем следующий этап по OrderSequence
        var nextStage = await _db.ApprovalStages
            .Where(s => s.OrderSequence > currentStage.OrderSequence)
            .OrderBy(s => s.OrderSequence)
            .FirstOrDefaultAsync();

        // Если следующего этапа нет — процесс завершён (после этапа 9)
        if (nextStage is null)
        {
            return new WorkflowDecisionResult
            {
                Success = true,
                IsCompleted = true,
                IsRejected = false,
                NextStageId = null,
                Message = "Заявка полностью согласована. Статус: Выполнено."
            };
        }

        return new WorkflowDecisionResult
        {
            Success = true,
            NextStageId = nextStage.Id,
            IsCompleted = false,
            IsRejected = false,
            Message = $"Заявка передана на этап: {nextStage.Name} ({nextStage.RequiredPosition})."
        };
    }
}
