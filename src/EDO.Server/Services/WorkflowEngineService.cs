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

    private static string BuildReturnComment(string targetName, string targetPosition, string? userComment)
    {
        var header = string.IsNullOrEmpty(targetPosition)
            ? $"[Возврат на этап: «{targetName}»]"
            : $"[Возврат на этап: «{targetName}» ({targetPosition})]";

        return string.IsNullOrWhiteSpace(userComment)
            ? header
            : $"{header} {userComment}";
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

        // --- ОТКЛОНЕНИЕ: сначала определяем и валидируем целевой этап возврата ---
        ApprovalStage? targetStage = null;
        bool isReturnToInitiator = false;

        if (request.Decision == Decision.Rejected)
        {
            if (request.TargetStageId.HasValue)
            {
                targetStage = await _db.ApprovalStages
                    .FirstOrDefaultAsync(s => s.Id == request.TargetStageId.Value);

                if (targetStage is null)
                {
                    return new WorkflowDecisionResult
                    {
                        Success = false,
                        Message = $"Целевой этап возврата (Id={request.TargetStageId}) не найден."
                    };
                }

                // Целевой этап должен быть ПРОЙДЕННЫМ (OrderSequence строго меньше текущего)
                if (targetStage.OrderSequence >= currentStage.OrderSequence)
                {
                    return new WorkflowDecisionResult
                    {
                        Success = false,
                        Message = "Нельзя вернуть заявку на текущий или последующий этап — " +
                                  "доступны только уже пройденные этапы."
                    };
                }
            }
            else
            {
                // TargetStageId не задан — возврат к инициатору (OrderSequence = 0)
                targetStage = await _db.ApprovalStages
                    .Where(s => s.OrderSequence == 0)
                    .FirstOrDefaultAsync();

                if (targetStage is null)
                {
                    return new WorkflowDecisionResult
                    {
                        Success = false,
                        Message = "Не найден этап инициатора (OrderSequence = 0)."
                    };
                }
            }

            isReturnToInitiator = targetStage.OrderSequence == 0;
        }

        // Формируем комментарий для истории — для отклонения указываем целевой этап
        var historyComment = (request.Decision == Decision.Rejected && targetStage is not null)
            ? BuildReturnComment(targetStage.Name, targetStage.RequiredPosition, request.Comment)
            : request.Comment;

        var historyEntry = new ActionHistory
        {
            DocumentId = request.DocumentId,
            UserId = request.UserId,
            StageId = request.CurrentStageId,
            Decision = request.Decision,
            Comment = historyComment,
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

        // --- ОТКЛОНЕНИЕ: возврат результата ---
        if (request.Decision == Decision.Rejected)
        {
            return new WorkflowDecisionResult
            {
                Success = true,
                IsRejected = true,
                IsReturnToInitiator = isReturnToInitiator,
                IsCompleted = false,
                NextStageId = targetStage!.Id,
                Message = isReturnToInitiator
                    ? "Заявка отклонена и возвращена инициатору на доработку."
                    : $"Заявка возвращена на этап: «{targetStage.Name}» ({targetStage.RequiredPosition})."
            };
        }

        // --- ОДОБРЕНИЕ ---
        // Если задан TargetStageId — используем его как «следующий» (с валидацией)
        ApprovalStage? nextStage;
        bool skippedStages = false;

        if (request.TargetStageId.HasValue)
        {
            nextStage = await _db.ApprovalStages
                .FirstOrDefaultAsync(s => s.Id == request.TargetStageId.Value);

            if (nextStage is null)
            {
                return new WorkflowDecisionResult
                {
                    Success = false,
                    Message = $"Целевой этап (Id={request.TargetStageId}) не найден."
                };
            }

            // Целевой этап должен быть ВПЕРЕДИ (OrderSequence строго больше текущего)
            if (nextStage.OrderSequence <= currentStage.OrderSequence)
            {
                return new WorkflowDecisionResult
                {
                    Success = false,
                    Message = "Нельзя перейти на текущий или пройденный этап — " +
                              "выберите один из последующих этапов."
                };
            }

            // Если пропустили этапы — отмечаем
            skippedStages = nextStage.OrderSequence > currentStage.OrderSequence + 1;
        }
        else
        {
            nextStage = await _db.ApprovalStages
                .Where(s => s.OrderSequence > currentStage.OrderSequence)
                .OrderBy(s => s.OrderSequence)
                .FirstOrDefaultAsync();
        }

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

        // Если были пропуски — добавляем системную запись в историю
        if (skippedStages)
        {
            _db.ActionHistories.Add(new ActionHistory
            {
                DocumentId = request.DocumentId,
                UserId = request.UserId,
                StageId = request.CurrentStageId,
                Decision = Decision.Reviewed,
                Comment = $"[Система] Пропуск этапов — переход на «{nextStage.Name}» " +
                          $"({nextStage.RequiredPosition}).",
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        return new WorkflowDecisionResult
        {
            Success = true,
            NextStageId = nextStage.Id,
            IsCompleted = false,
            IsRejected = false,
            Message = skippedStages
                ? $"Заявка передана на этап «{nextStage.Name}» ({nextStage.RequiredPosition}) с пропуском промежуточных этапов."
                : $"Заявка передана на этап: {nextStage.Name} ({nextStage.RequiredPosition})."
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
                Message = $"У вас нет прав для согласования на шаге «{currentStep.StepName}». " +
                          $"Требуемая должность: «{currentStep.TargetPosition}», " +
                          $"ваша должность: «{user.Position}»."
            };
        }

        // --- ОТКЛОНЕНИЕ: определяем и валидируем целевой шаг возврата ---
        WorkflowStep? targetStep = null;
        bool isReturnToInitiator = false;
        string? returnTargetDisplayName = null;
        string? returnTargetPosition = null;

        if (request.Decision == Decision.Rejected)
        {
            if (request.TargetWorkflowStepId.HasValue)
            {
                targetStep = await _db.WorkflowSteps
                    .FirstOrDefaultAsync(s => s.Id == request.TargetWorkflowStepId.Value);

                if (targetStep is null)
                {
                    return new WorkflowDecisionResult
                    {
                        Success = false,
                        Message = $"Целевой шаг возврата (Id={request.TargetWorkflowStepId}) не найден."
                    };
                }

                // Должен принадлежать той же цепочке
                if (targetStep.WorkflowChainId != request.WorkflowChainId!.Value)
                {
                    return new WorkflowDecisionResult
                    {
                        Success = false,
                        Message = "Целевой шаг принадлежит другой цепочке согласования."
                    };
                }

                // Должен быть ПРОЙДЕННЫМ (Order строго меньше текущего)
                if (targetStep.Order >= currentStep.Order)
                {
                    return new WorkflowDecisionResult
                    {
                        Success = false,
                        Message = "Нельзя вернуть заявку на текущий или последующий шаг — " +
                                  "доступны только уже пройденные шаги."
                    };
                }

                returnTargetDisplayName = targetStep.StepName;
                returnTargetPosition = targetStep.TargetPosition;
            }
            else
            {
                // TargetWorkflowStepId не задан — возврат к инициатору
                isReturnToInitiator = true;
                returnTargetDisplayName = "Инициатор";
                returnTargetPosition = "Инициатор";
            }
        }

        var historyComment = (request.Decision == Decision.Rejected && returnTargetDisplayName is not null)
            ? BuildReturnComment(returnTargetDisplayName, returnTargetPosition ?? "", request.Comment)
            : request.Comment;

        var historyEntry = new ActionHistory
        {
            DocumentId = request.DocumentId,
            UserId = request.UserId,
            StageId = null,
            WorkflowStepId = currentStep.Id,
            Decision = request.Decision,
            Comment = historyComment,
            CreatedAt = DateTime.UtcNow
        };

        _db.ActionHistories.Add(historyEntry);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save ActionHistory for custom chain DocumentId={DocId}, StepId={StepId}",
                request.DocumentId, currentStep.Id);
            return new WorkflowDecisionResult
            {
                Success = false,
                Message = $"Ошибка сохранения истории: {ex.InnerException?.Message ?? ex.Message}"
            };
        }

        if (request.Decision == Decision.Rejected)
        {
            return new WorkflowDecisionResult
            {
                Success = true,
                IsRejected = true,
                IsReturnToInitiator = isReturnToInitiator,
                IsCompleted = false,
                NextWorkflowStepId = targetStep?.Id,
                Message = isReturnToInitiator
                    ? "Заявка отклонена и возвращена инициатору на доработку."
                    : $"Заявка возвращена на шаг: «{targetStep!.StepName}» ({targetStep.TargetPosition})."
            };
        }

        // --- ОДОБРЕНИЕ: возможен прыжок вперёд по TargetWorkflowStepId ---
        WorkflowStep? nextStep;
        bool skippedSteps = false;

        if (request.TargetWorkflowStepId.HasValue)
        {
            nextStep = await _db.WorkflowSteps
                .FirstOrDefaultAsync(s => s.Id == request.TargetWorkflowStepId.Value);

            if (nextStep is null)
            {
                return new WorkflowDecisionResult
                {
                    Success = false,
                    Message = $"Целевой шаг (Id={request.TargetWorkflowStepId}) не найден."
                };
            }

            if (nextStep.WorkflowChainId != request.WorkflowChainId!.Value)
            {
                return new WorkflowDecisionResult
                {
                    Success = false,
                    Message = "Целевой шаг принадлежит другой цепочке согласования."
                };
            }

            if (nextStep.Order <= currentStep.Order)
            {
                return new WorkflowDecisionResult
                {
                    Success = false,
                    Message = "Нельзя перейти на текущий или пройденный шаг — " +
                              "выберите один из последующих шагов."
                };
            }

            // Был ли пропуск «соседнего» следующего шага?
            var immediateNextOrder = await _db.WorkflowSteps
                .Where(s => s.WorkflowChainId == request.WorkflowChainId.Value
                         && s.Order > currentStep.Order)
                .MinAsync(s => (int?)s.Order);

            skippedSteps = immediateNextOrder.HasValue && nextStep.Order > immediateNextOrder.Value;
        }
        else
        {
            nextStep = await _db.WorkflowSteps
                .Where(s => s.WorkflowChainId == request.WorkflowChainId!.Value
                         && s.Order > currentStep.Order)
                .OrderBy(s => s.Order)
                .FirstOrDefaultAsync();
        }

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

        if (skippedSteps)
        {
            _db.ActionHistories.Add(new ActionHistory
            {
                DocumentId = request.DocumentId,
                UserId = request.UserId,
                StageId = null,
                WorkflowStepId = currentStep.Id,
                Decision = Decision.Reviewed,
                Comment = $"[Система] Пропуск шагов — переход на «{nextStep.StepName}» " +
                          $"({nextStep.TargetPosition}).",
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        return new WorkflowDecisionResult
        {
            Success = true,
            NextWorkflowStepId = nextStep.Id,
            IsCompleted = false,
            IsRejected = false,
            Message = skippedSteps
                ? $"Заявка передана на шаг «{nextStep.StepName}» ({nextStep.TargetPosition}) с пропуском промежуточных шагов."
                : $"Заявка передана на шаг: {nextStep.StepName} ({nextStep.TargetPosition})."
        };
    }
}
