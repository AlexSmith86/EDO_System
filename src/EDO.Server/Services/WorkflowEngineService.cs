using EDO.Server.Data;
using EDO.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EDO.Server.Services;

public class WorkflowEngineService : IWorkflowEngineService
{
    private readonly AppDbContext _db;
    private readonly ILogger<WorkflowEngineService> _logger;

    private const string InitiatorPosition = "Инициатор";
    private const string AnyPosition = "Любая должность";

    public WorkflowEngineService(AppDbContext db, ILogger<WorkflowEngineService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> CanUserActAsync(int userId, int stageId, int documentId)
    {
        var stage = await _db.ApprovalStages.FindAsync(stageId);
        if (stage is null)
        {
            _logger.LogWarning("CanUserAct: stage {StageId} not found", stageId);
            return false;
        }

        var user = await _db.Users.FindAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("CanUserAct: user {UserId} not found", userId);
            return false;
        }

        if (string.Equals(stage.RequiredPosition, InitiatorPosition, StringComparison.OrdinalIgnoreCase))
        {
            var request = await _db.TmcRequests.FindAsync(documentId);
            var isInitiator = request is not null && request.InitiatorUserId == userId;
            _logger.LogInformation("CanUserAct: initiator check for user {UserId}, result={Result}", userId, isInitiator);
            return isInitiator;
        }

        var match = string.Equals(user.Position, stage.RequiredPosition, StringComparison.OrdinalIgnoreCase);
        if (!match)
        {
            _logger.LogWarning(
                "CanUserAct: position mismatch. User '{UserPosition}' vs Stage '{StagePosition}' (StageId={StageId})",
                user.Position, stage.RequiredPosition, stageId);
        }
        return match;
    }

    public async Task<WorkflowDecisionResult> ProcessDecisionAsync(WorkflowDecisionRequest request)
    {
        // Если задана кастомная цепочка — используем логику шагов
        if (request.WorkflowChainId.HasValue && request.CurrentWorkflowStepId.HasValue)
        {
            return await ProcessCustomChainDecisionAsync(request);
        }

        // Стандартный маршрут через ApprovalStages
        return await ProcessStandardDecisionAsync(request);
    }

    private async Task<WorkflowDecisionResult> ProcessStandardDecisionAsync(WorkflowDecisionRequest request)
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

        if (!await CanUserActAsync(request.UserId, request.CurrentStageId, request.DocumentId))
        {
            var user = await _db.Users.FindAsync(request.UserId);
            return new WorkflowDecisionResult
            {
                Success = false,
                Message = $"У вас нет прав для согласования на этапе «{currentStage.Name}». " +
                          $"Требуемая должность: «{currentStage.RequiredPosition}», " +
                          $"ваша должность: «{user?.Position ?? "не найдена"}»."
            };
        }

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

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save ActionHistory for DocumentId={DocId}, StageId={StageId}", request.DocumentId, request.CurrentStageId);
            return new WorkflowDecisionResult
            {
                Success = false,
                Message = $"Ошибка сохранения истории: {ex.InnerException?.Message ?? ex.Message}"
            };
        }

        // --- ОТКЛОНЕНИЕ ---
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
        var nextStage = await _db.ApprovalStages
            .Where(s => s.OrderSequence > currentStage.OrderSequence)
            .OrderBy(s => s.OrderSequence)
            .FirstOrDefaultAsync();

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

    private async Task<WorkflowDecisionResult> ProcessCustomChainDecisionAsync(WorkflowDecisionRequest request)
    {
        var currentStep = await _db.WorkflowSteps
            .FirstOrDefaultAsync(s => s.Id == request.CurrentWorkflowStepId!.Value);

        if (currentStep is null)
        {
            return new WorkflowDecisionResult
            {
                Success = false,
                Message = "Текущий шаг цепочки не найден."
            };
        }

        // Проверяем совпадение должности
        var user = await _db.Users.FindAsync(request.UserId);
        if (user is null)
        {
            return new WorkflowDecisionResult { Success = false, Message = "Пользователь не найден." };
        }

        if (!string.Equals(currentStep.TargetPosition, AnyPosition, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(user.Position, currentStep.TargetPosition, StringComparison.OrdinalIgnoreCase))
        {
            return new WorkflowDecisionResult
            {
                Success = false,
                Message = $"У вас нет прав для согласования на данном шаге. Требуемая должность: {currentStep.TargetPosition}."
            };
        }

        // Записываем в историю (используем StageId — ближайший ApprovalStage или создаём запись без него)
        // Для кастомных цепочек сохраняем StageId = currentStage (из заявки) если есть
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
        if (request.Decision == Decision.Rejected)
        {
            // Возврат на первый шаг цепочки
            var firstStep = await _db.WorkflowSteps
                .Where(s => s.WorkflowChainId == request.WorkflowChainId!.Value)
                .OrderBy(s => s.Order)
                .FirstOrDefaultAsync();

            return new WorkflowDecisionResult
            {
                Success = true,
                IsRejected = true,
                IsCompleted = false,
                NextWorkflowStepId = firstStep?.Id,
                Message = "Заявка отклонена и возвращена на первый шаг."
            };
        }

        // --- ОДОБРЕНИЕ ---
        var nextStep = await _db.WorkflowSteps
            .Where(s => s.WorkflowChainId == request.WorkflowChainId!.Value
                     && s.Order > currentStep.Order)
            .OrderBy(s => s.Order)
            .FirstOrDefaultAsync();

        if (nextStep is null)
        {
            return new WorkflowDecisionResult
            {
                Success = true,
                IsCompleted = true,
                IsRejected = false,
                NextWorkflowStepId = null,
                Message = "Заявка полностью согласована по кастомной цепочке."
            };
        }

        return new WorkflowDecisionResult
        {
            Success = true,
            NextWorkflowStepId = nextStep.Id,
            IsCompleted = false,
            IsRejected = false,
            Message = $"Заявка передана на шаг: {nextStep.StepName} ({nextStep.TargetPosition})."
        };
    }
}
