using System.Security.Claims;
using EDO.Server.Data;
using EDO.Server.DTOs;
using EDO.Server.Models;
using EDO.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EDO.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TmcRequestsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWorkflowEngineService _workflow;
    private readonly ILogger<TmcRequestsController> _logger;
    private int _totalStages;
    private const string AnyPosition = "Любая должность";

    public TmcRequestsController(AppDbContext db, IWorkflowEngineService workflow, ILogger<TmcRequestsController> logger)
    {
        _db = db;
        _workflow = workflow;
        _logger = logger;
    }

    private async Task EnsureTotalStagesLoaded()
    {
        if (_totalStages == 0)
            _totalStages = await _db.ApprovalStages.CountAsync();
    }

    [HttpGet]
    public async Task<ActionResult<List<TmcRequestDto>>> GetAll()
    {
        await EnsureTotalStagesLoaded();
        var entities = await LoadAllRequests()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(entities.Select(r => MapToDto(r)).ToList());
    }

    [HttpGet("projects")]
    public async Task<ActionResult<List<string>>> GetProjects()
    {
        var projects = await _db.TmcRequests
            .Where(r => !string.IsNullOrEmpty(r.ProjectName))
            .Select(r => r.ProjectName!)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();
        return Ok(projects);
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<TmcRequestDto>>> GetMy()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        await EnsureTotalStagesLoaded();
        var entities = await LoadAllRequests()
            .Where(r => r.InitiatorUserId == userId.Value)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var histories = await LoadApprovalHistoriesForRequests(entities.Select(e => e.Id).ToList());
        return Ok(entities
            .Select(r => MapToDto(r, _totalStages,
                histories.TryGetValue(r.Id, out var h) ? h : new List<ApprovalHistoryDto>()))
            .ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TmcRequestDto>> GetById(int id)
    {
        await EnsureTotalStagesLoaded();
        var entity = await LoadRequest(id);
        if (entity is null)
            return NotFound(new { message = "Заявка не найдена." });

        var history = await LoadApprovalHistory(id);
        return Ok(MapToDto(entity, _totalStages, history));
    }

    [HttpPost]
    public async Task<ActionResult<TmcRequestDto>> Create([FromBody] CreateTmcRequestDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        if (dto.Items.Count == 0)
            return BadRequest(new { message = "Заявка должна содержать хотя бы одну позицию." });

        var entity = new TmcRequest
        {
            InitiatorUserId = userId.Value,
            ProjectName = dto.ProjectName,
            WorkflowChainId = dto.WorkflowChainId,
            Status = TmcRequestStatus.Draft,
            Items = dto.Items.Select(i => new TmcRequestItem
            {
                GroupId = i.GroupId,
                SubgroupId = i.SubgroupId,
                Name = i.Name,
                Quantity = i.Quantity,
                Unit = i.Unit,
                Price = i.Price,
                PlannedDeliveryDate = i.PlannedDeliveryDate,
                InvoiceLink = i.InvoiceLink,
                Comment = i.Comment,
                InitiatorName = i.InitiatorName,
                InitiatorPosition = i.InitiatorPosition
            }).ToList()
        };

        _db.TmcRequests.Add(entity);
        await _db.SaveChangesAsync();

        await EnsureTotalStagesLoaded();
        var created = await LoadRequest(entity.Id);
        return Created($"/api/tmcrequests/{entity.Id}", MapToDto(created!));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Администратор")]
    public async Task<ActionResult<TmcRequestDto>> Update(int id, [FromBody] UpdateTmcRequestDto dto)
    {
        var entity = await _db.TmcRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (entity is null)
            return NotFound(new { message = "Заявка не найдена." });

        // Редактировать можно черновики и заявки на доработке
        if (entity.Status != TmcRequestStatus.Draft && entity.Status != TmcRequestStatus.Rework)
            return BadRequest(new { message = "Редактировать можно только заявки в статусе 'Черновик' или 'На доработке'." });

        if (dto.Items.Count == 0)
            return BadRequest(new { message = "Заявка должна содержать хотя бы одну позицию." });

        entity.ProjectName = dto.ProjectName;
        _db.TmcRequestItems.RemoveRange(entity.Items);
        entity.Items = dto.Items.Select(i => new TmcRequestItem
        {
            GroupId = i.GroupId,
            SubgroupId = i.SubgroupId,
            Name = i.Name,
            Quantity = i.Quantity,
            Unit = i.Unit,
            Price = i.Price,
            PlannedDeliveryDate = i.PlannedDeliveryDate,
            InvoiceLink = i.InvoiceLink,
            Comment = i.Comment,
            InitiatorName = i.InitiatorName,
            InitiatorPosition = i.InitiatorPosition
        }).ToList();

        await _db.SaveChangesAsync();

        await EnsureTotalStagesLoaded();
        var updated = await LoadRequest(entity.Id);
        return Ok(MapToDto(updated!));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Администратор")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.TmcRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (entity is null)
            return NotFound(new { message = "Заявка не найдена." });

        var relatedHistory = await _db.ActionHistories
            .Where(h => h.DocumentId == id)
            .ToListAsync();

        if (relatedHistory.Count > 0)
            _db.ActionHistories.RemoveRange(relatedHistory);

        _db.TmcRequestItems.RemoveRange(entity.Items);
        _db.TmcRequests.Remove(entity);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Отправить заявку на согласование (из Draft или Rework → InApproval).
    /// Инициатор может указать стартовый этап/шаг через тело запроса (гибкая маршрутизация вперёд).</summary>
    [HttpPost("{id}/send")]
    public async Task<ActionResult<TmcRequestDto>> Send(int id, [FromBody] SendRequestDto? dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var entity = await _db.TmcRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (entity is null)
            return NotFound(new { message = "Заявка не найдена." });

        var isAdmin = User.IsInRole("Администратор");
        if (entity.InitiatorUserId != userId.Value && !isAdmin)
            return Forbid();

        if (entity.Status != TmcRequestStatus.Draft && entity.Status != TmcRequestStatus.Rework)
            return BadRequest(new { message = "Отправить можно только заявку в статусе 'Черновик' или 'На доработке'." });

        if (entity.Items.Count == 0)
            return BadRequest(new { message = "Нельзя отправить пустую заявку." });

        string? systemNote = null;

        if (entity.WorkflowChainId.HasValue)
        {
            // --- Кастомная цепочка ---
            WorkflowStep? startStep;

            if (dto?.TargetWorkflowStepId is int targetStepId)
            {
                startStep = await _db.WorkflowSteps
                    .FirstOrDefaultAsync(s => s.Id == targetStepId);

                if (startStep is null)
                    return BadRequest(new { message = $"Целевой шаг (Id={targetStepId}) не найден." });

                if (startStep.WorkflowChainId != entity.WorkflowChainId.Value)
                    return BadRequest(new { message = "Целевой шаг принадлежит другой цепочке." });

                // Проверка «пропуска»: если startStep.Order > первого шага — это прыжок вперёд
                var firstStepOrder = await _db.WorkflowSteps
                    .Where(s => s.WorkflowChainId == entity.WorkflowChainId.Value)
                    .MinAsync(s => (int?)s.Order) ?? startStep.Order;

                if (startStep.Order > firstStepOrder)
                {
                    systemNote = $"Отправлено с пропуском — старт с шага «{startStep.StepName}» " +
                                 $"({startStep.TargetPosition}).";
                }
            }
            else
            {
                startStep = await _db.WorkflowSteps
                    .Where(s => s.WorkflowChainId == entity.WorkflowChainId.Value)
                    .OrderBy(s => s.Order)
                    .FirstOrDefaultAsync();

                if (startStep is null)
                    return BadRequest(new { message = "Кастомная цепочка пуста. Обратитесь к администратору." });
            }

            entity.Status = TmcRequestStatus.InApproval;
            entity.CurrentWorkflowStepId = startStep.Id;
            entity.CurrentStageId = null;
        }
        else
        {
            // --- Стандартный маршрут ---
            ApprovalStage? startStage;

            if (dto?.TargetStageId is int targetStageId)
            {
                startStage = await _db.ApprovalStages
                    .FirstOrDefaultAsync(s => s.Id == targetStageId);

                if (startStage is null)
                    return BadRequest(new { message = $"Целевой этап (Id={targetStageId}) не найден." });

                if (startStage.OrderSequence <= 0)
                    return BadRequest(new { message = "Нельзя стартовать с этапа «Инициатор». " +
                                                        "Выберите этап согласования (OrderSequence > 0)." });

                if (startStage.OrderSequence > 1)
                {
                    systemNote = $"Отправлено с пропуском — старт с этапа «{startStage.Name}» " +
                                 $"({startStage.RequiredPosition}).";
                }
            }
            else
            {
                startStage = await _db.ApprovalStages
                    .Where(s => s.OrderSequence == 1)
                    .FirstOrDefaultAsync();

                if (startStage is null)
                    return BadRequest(new { message = "Цепочка согласования не настроена. Обратитесь к администратору." });
            }

            entity.Status = TmcRequestStatus.InApproval;
            entity.CurrentStageId = startStage.Id;
            entity.CurrentWorkflowStepId = null;
        }

        // Запись в историю факта отправки с учётом возможного пропуска
        if (!string.IsNullOrEmpty(systemNote))
        {
            _db.ActionHistories.Add(new ActionHistory
            {
                DocumentId = entity.Id,
                UserId = userId.Value,
                StageId = entity.CurrentStageId,
                WorkflowStepId = entity.CurrentWorkflowStepId,
                Decision = Decision.Reviewed,
                Comment = $"[Система] {systemNote}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        await EnsureTotalStagesLoaded();
        var updated = await LoadRequest(entity.Id);
        return Ok(MapToDto(updated!));
    }

    /// <summary>Согласованные заявки (Completed / Approved) с историей</summary>
    [HttpGet("approved")]
    public async Task<ActionResult<List<TmcRequestDto>>> GetApproved()
    {
        await EnsureTotalStagesLoaded();
        var entities = await LoadAllRequests()
            .Where(r => r.Status == TmcRequestStatus.Completed || r.Status == TmcRequestStatus.Approved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var result = new List<TmcRequestDto>();
        foreach (var r in entities)
        {
            var history = await LoadApprovalHistory(r.Id);
            result.Add(MapToDto(r, _totalStages, history));
        }

        return Ok(result);
    }

    /// <summary>Получить заявки, ожидающие действия текущего пользователя</summary>
    [HttpGet("pending")]
    public async Task<ActionResult<List<TmcRequestDto>>> GetPending()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var user = await _db.Users.FindAsync(userId.Value);
        if (user is null) return Unauthorized();

        await EnsureTotalStagesLoaded();
        // Загружаем все заявки на согласовании / на доработке
        var candidates = await LoadAllRequests()
            .Where(r => (r.Status == TmcRequestStatus.InApproval || r.Status == TmcRequestStatus.Rework)
                     && (r.CurrentStageId != null || r.CurrentWorkflowStepId != null))
            .ToListAsync();

        // Фильтруем по должности
        var result = new List<TmcRequestDto>();
        foreach (var r in candidates)
        {
            bool canAct = false;

            // Делегирование: назначенный ответственный всегда видит заявку на текущем этапе
            if (r.ResponsibleUserId == userId.Value)
            {
                canAct = true;
            }
            else if (r.WorkflowChainId.HasValue && r.CurrentWorkflowStep is not null)
            {
                // Кастомная цепочка — проверяем TargetPosition
                canAct = string.Equals(r.CurrentWorkflowStep.TargetPosition, AnyPosition, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(user.Position, r.CurrentWorkflowStep.TargetPosition, StringComparison.OrdinalIgnoreCase);
            }
            else if (r.CurrentStage is not null)
            {
                // Стандартный маршрут
                if (r.CurrentStage.RequiredPosition == "Инициатор")
                {
                    canAct = r.InitiatorUserId == userId.Value;
                }
                else
                {
                    canAct = string.Equals(user.Position, r.CurrentStage.RequiredPosition,
                        StringComparison.OrdinalIgnoreCase);
                }
            }

            if (canAct)
                result.Add(MapToDto(r));
        }

        var histories = await LoadApprovalHistoriesForRequests(result.Select(r => r.Id).ToList());
        foreach (var dto in result)
        {
            if (histories.TryGetValue(dto.Id, out var h))
                dto.ApprovalHistory = h;
        }

        return Ok(result.OrderByDescending(r => r.CreatedAt).ToList());
    }

    /// <summary>Список этапов/шагов, доступных для движения заявки ВПЕРЁД.
    /// Для Draft/Rework (без текущего этапа) — все стартовые этапы маршрута.
    /// Для InApproval — все последующие этапы (Order > current).</summary>
    [HttpGet("{id}/forward-targets")]
    public async Task<ActionResult<List<ForwardTargetDto>>> GetForwardTargets(int id)
    {
        var entity = await _db.TmcRequests
            .Include(r => r.CurrentStage)
            .Include(r => r.CurrentWorkflowStep)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (entity is null)
            return NotFound(new { message = "Заявка не найдена." });

        var result = new List<ForwardTargetDto>();

        if (entity.WorkflowChainId.HasValue)
        {
            // --- Кастомная цепочка ---
            // В кастомных цепочках шаг с Order == 0 ("Создание заявки") считается
            // уже пройденным в момент создания черновика: сам факт сохранения черновика
            // его закрывает. Поэтому в список доступных стартовых/следующих этапов
            // его не включаем — иначе при отправке из Draft/Rework пользователь видит
            // бесполезный пункт «0. Создание заявки». Для стандартного маршрута этой
            // проблемы нет (там Order == 0 отсутствует как концепт).
            var stepsQuery = _db.WorkflowSteps
                .Where(s => s.WorkflowChainId == entity.WorkflowChainId.Value
                         && s.Order > 0);

            // Если заявка уже на каком-то шаге — показываем только последующие
            if (entity.CurrentWorkflowStep is not null)
            {
                var currentOrder = entity.CurrentWorkflowStep.Order;
                stepsQuery = stepsQuery.Where(s => s.Order > currentOrder);
            }

            var steps = await stepsQuery.OrderBy(s => s.Order).ToListAsync();

            for (var i = 0; i < steps.Count; i++)
            {
                var s = steps[i];
                result.Add(new ForwardTargetDto
                {
                    WorkflowStepId = s.Id,
                    Name = s.StepName,
                    Position = s.TargetPosition,
                    Description = null,
                    Order = s.Order,
                    IsDefault = i == 0
                });
            }
        }
        else
        {
            // --- Стандартный маршрут ---
            var currentOrder = entity.CurrentStage?.OrderSequence ?? 0;

            var stages = await _db.ApprovalStages
                .Where(s => s.OrderSequence > currentOrder)
                .OrderBy(s => s.OrderSequence)
                .ToListAsync();

            for (var i = 0; i < stages.Count; i++)
            {
                var s = stages[i];
                result.Add(new ForwardTargetDto
                {
                    StageId = s.Id,
                    Name = s.Name,
                    Position = s.RequiredPosition,
                    Description = s.Description,
                    Order = s.OrderSequence,
                    IsDefault = i == 0
                });
            }
        }

        return Ok(result);
    }

    /// <summary>Список этапов/шагов, на которые можно вернуть заявку при отклонении.
    /// Возвращает только пройденные этапы. Для кастомных цепочек последним пунктом
    /// идёт синтетический «Инициатор» (оба Id = null).</summary>
    [HttpGet("{id}/return-targets")]
    public async Task<ActionResult<List<ReturnTargetDto>>> GetReturnTargets(int id)
    {
        var entity = await _db.TmcRequests
            .Include(r => r.CurrentStage)
            .Include(r => r.CurrentWorkflowStep)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (entity is null)
            return NotFound(new { message = "Заявка не найдена." });

        if ((entity.Status != TmcRequestStatus.InApproval && entity.Status != TmcRequestStatus.Rework)
            || (entity.CurrentStageId is null && entity.CurrentWorkflowStepId is null))
            return BadRequest(new { message = "Заявка не находится на согласовании." });

        var result = new List<ReturnTargetDto>();

        if (entity.WorkflowChainId.HasValue && entity.CurrentWorkflowStep is not null)
        {
            // Кастомная цепочка: все шаги с Order < текущего
            var steps = await _db.WorkflowSteps
                .Where(s => s.WorkflowChainId == entity.WorkflowChainId.Value
                         && s.Order < entity.CurrentWorkflowStep.Order)
                .OrderBy(s => s.Order)
                .ToListAsync();

            result.AddRange(steps.Select(s => new ReturnTargetDto
            {
                WorkflowStepId = s.Id,
                Name = s.StepName,
                Position = s.TargetPosition,
                Order = s.Order,
                IsInitiator = false
            }));

            // Синтетический пункт «Инициатор» — оба Id null
            result.Add(new ReturnTargetDto
            {
                StageId = null,
                WorkflowStepId = null,
                Name = "Инициатор",
                Position = "Инициатор",
                Order = int.MaxValue,
                IsInitiator = true
            });
        }
        else if (entity.CurrentStage is not null)
        {
            // Стандартный маршрут: все этапы с OrderSequence < текущего
            var stages = await _db.ApprovalStages
                .Where(s => s.OrderSequence < entity.CurrentStage.OrderSequence)
                .OrderBy(s => s.OrderSequence)
                .ToListAsync();

            result.AddRange(stages.Select(s => new ReturnTargetDto
            {
                StageId = s.Id,
                Name = s.Name,
                Position = s.RequiredPosition,
                Order = s.OrderSequence,
                IsInitiator = s.OrderSequence == 0
            }));
        }

        return Ok(result);
    }

    /// <summary>Принять решение по заявке (Approve / Reject)</summary>
    [HttpPost("{id}/decision")]
    public async Task<ActionResult<TmcRequestDto>> SubmitDecision(int id, [FromBody] SubmitDecisionDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var entity = await _db.TmcRequests
            .Include(r => r.CurrentStage)
            .Include(r => r.CurrentWorkflowStep)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (entity is null)
            return NotFound(new { message = "Заявка не найдена." });

        if ((entity.Status != TmcRequestStatus.InApproval && entity.Status != TmcRequestStatus.Rework)
            || (entity.CurrentStageId is null && entity.CurrentWorkflowStepId is null))
            return BadRequest(new { message = "Заявка не находится на согласовании." });

        if (!Enum.TryParse<Decision>(dto.Decision, out var decision) ||
            (decision != Decision.Approved && decision != Decision.Rejected))
            return BadRequest(new { message = "Допустимые решения: Approved, Rejected." });

        if (decision == Decision.Rejected && string.IsNullOrWhiteSpace(dto.Comment))
            return BadRequest(new { message = "При отклонении необходимо указать комментарий." });

        try
        {
            var user = await _db.Users.FindAsync(userId.Value);
            _logger.LogInformation(
                "SubmitDecision: RequestId={RequestId}, UserId={UserId}, UserPosition={Position}, " +
                "StageId={StageId}, StageName={StageName}, StageRequired={Required}, Decision={Decision}",
                id, userId.Value, user?.Position,
                entity.CurrentStageId, entity.CurrentStage?.Name, entity.CurrentStage?.RequiredPosition,
                dto.Decision);

            var result = await _workflow.ProcessDecisionAsync(new WorkflowDecisionRequest
            {
                DocumentId = entity.Id,
                UserId = userId.Value,
                CurrentStageId = entity.CurrentStageId ?? 0,
                Decision = decision,
                Comment = dto.Comment,
                WorkflowChainId = entity.WorkflowChainId,
                CurrentWorkflowStepId = entity.CurrentWorkflowStepId,
                TargetStageId = dto.TargetStageId,
                TargetWorkflowStepId = dto.TargetWorkflowStepId
            });

            if (!result.Success)
            {
                _logger.LogWarning("SubmitDecision failed: {Message}", result.Message);
                return BadRequest(new { message = result.Message });
            }

            if (entity.WorkflowChainId.HasValue)
            {
                if (result.IsRejected)
                {
                    // Rework — только при возврате к инициатору; иначе остаёмся на согласовании у предыдущего шага
                    entity.Status = result.IsReturnToInitiator
                        ? TmcRequestStatus.Rework
                        : TmcRequestStatus.InApproval;
                    entity.CurrentWorkflowStepId = result.NextWorkflowStepId;
                }
                else if (result.IsCompleted)
                {
                    entity.Status = TmcRequestStatus.Completed;
                    entity.CurrentWorkflowStepId = null;
                }
                else
                {
                    entity.CurrentWorkflowStepId = result.NextWorkflowStepId;
                }
            }
            else
            {
                if (result.IsRejected)
                {
                    entity.Status = result.IsReturnToInitiator
                        ? TmcRequestStatus.Rework
                        : TmcRequestStatus.InApproval;
                    entity.CurrentStageId = result.NextStageId;
                }
                else if (result.IsCompleted)
                {
                    entity.Status = TmcRequestStatus.Completed;
                    entity.CurrentStageId = null;
                }
                else
                {
                    entity.CurrentStageId = result.NextStageId;
                }
            }

            // Делегирование действует только на тот этап, на котором было назначено.
            // При любом движении заявки сбрасываем ответственного.
            entity.ResponsibleUserId = null;

            await _db.SaveChangesAsync();

            await EnsureTotalStagesLoaded();
            var updated = await LoadRequest(entity.Id);
            return Ok(MapToDto(updated!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SubmitDecision exception for RequestId={RequestId}", id);
            return StatusCode(500, new { message = $"Внутренняя ошибка: {ex.Message}" });
        }
    }

    /// <summary>Назначить ответственного за заявку (делегирование).
    /// Право назначить ответственного имеет тот, кто сейчас может работать с заявкой
    /// на текущем этапе (согласующий по должности или администратор).
    /// Делегирование действует до движения заявки на следующий этап.</summary>
    [HttpPost("{id}/assign")]
    public async Task<ActionResult<TmcRequestDto>> AssignResponsible(int id, [FromBody] AssignResponsibleDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var entity = await _db.TmcRequests
            .Include(r => r.CurrentStage)
            .Include(r => r.CurrentWorkflowStep)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (entity is null)
            return NotFound(new { message = "Заявка не найдена." });

        if (entity.Status != TmcRequestStatus.InApproval
            || (entity.CurrentStageId is null && entity.CurrentWorkflowStepId is null))
            return BadRequest(new { message = "Назначить ответственного можно только по заявке, находящейся на согласовании." });

        var caller = await _db.Users.FindAsync(userId.Value);
        if (caller is null) return Unauthorized();

        var isAdmin = User.IsInRole("Администратор");

        // Право делегировать имеют: текущий ответственный, согласующий по должности, администратор
        bool callerCanAct = isAdmin
                            || entity.ResponsibleUserId == userId.Value
                            || await CanCallerActOnCurrentStage(entity, caller);

        if (!callerCanAct)
            return Forbid();

        // Снять ответственного (TargetUserId == null)
        if (dto.TargetUserId is null)
        {
            if (entity.ResponsibleUserId is null)
            {
                await EnsureTotalStagesLoaded();
                var noopReload = await LoadRequest(entity.Id);
                return Ok(MapToDto(noopReload!));
            }

            entity.ResponsibleUserId = null;

            _db.ActionHistories.Add(new ActionHistory
            {
                DocumentId = entity.Id,
                UserId = userId.Value,
                StageId = entity.CurrentStageId,
                WorkflowStepId = entity.CurrentWorkflowStepId,
                Decision = Decision.Reviewed,
                Comment = "[Система] Ответственный снят.",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            await EnsureTotalStagesLoaded();
            var cleared = await LoadRequest(entity.Id);
            return Ok(MapToDto(cleared!));
        }

        var target = await _db.Users.FindAsync(dto.TargetUserId.Value);
        if (target is null || !target.IsActive)
            return BadRequest(new { message = "Назначаемый пользователь не найден или неактивен." });

        entity.ResponsibleUserId = target.Id;

        var fullName = $"{target.LastName} {target.FirstName} {target.MiddleName}".Trim();
        _db.ActionHistories.Add(new ActionHistory
        {
            DocumentId = entity.Id,
            UserId = userId.Value,
            StageId = entity.CurrentStageId,
            WorkflowStepId = entity.CurrentWorkflowStepId,
            Decision = Decision.Reviewed,
            Comment = $"[Система] Назначен ответственный: {fullName}, {target.Position}.",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        await EnsureTotalStagesLoaded();
        var updated = await LoadRequest(entity.Id);
        return Ok(MapToDto(updated!));
    }

    /// <summary>Проверка: может ли пользователь работать с заявкой на её текущем этапе
    /// по должности (без учёта делегирования).</summary>
    private static Task<bool> CanCallerActOnCurrentStage(TmcRequest entity, Models.User caller)
    {
        bool result = false;

        if (entity.WorkflowChainId.HasValue && entity.CurrentWorkflowStep is not null)
        {
            var pos = entity.CurrentWorkflowStep.TargetPosition;
            result = string.Equals(pos, AnyPosition, StringComparison.OrdinalIgnoreCase)
                  || string.Equals(caller.Position, pos, StringComparison.OrdinalIgnoreCase);
        }
        else if (entity.CurrentStage is not null)
        {
            if (entity.CurrentStage.RequiredPosition == "Инициатор")
                result = entity.InitiatorUserId == caller.Id;
            else
                result = string.Equals(caller.Position, entity.CurrentStage.RequiredPosition,
                    StringComparison.OrdinalIgnoreCase);
        }

        return Task.FromResult(result);
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private IQueryable<TmcRequest> LoadAllRequests()
    {
        return _db.TmcRequests
            .Include(r => r.InitiatorUser)
            .Include(r => r.ResponsibleUser)
            .Include(r => r.CurrentStage)
            .Include(r => r.WorkflowChain).ThenInclude(c => c!.Steps)
            .Include(r => r.CurrentWorkflowStep)
            .Include(r => r.Items).ThenInclude(i => i.Group)
            .Include(r => r.Items).ThenInclude(i => i.Subgroup);
    }

    private async Task<TmcRequest?> LoadRequest(int id)
    {
        return await LoadAllRequests()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    private static string StatusToRussian(TmcRequestStatus status) => status switch
    {
        TmcRequestStatus.Draft => "Черновик",
        TmcRequestStatus.InApproval => "На согласовании",
        TmcRequestStatus.Rework => "На доработке",
        TmcRequestStatus.Completed => "Выполнено",
        TmcRequestStatus.Approved => "Согласовано",
        TmcRequestStatus.Rejected => "Отклонено",
        _ => status.ToString()
    };

    private static string DecisionToRussian(Decision decision) => decision switch
    {
        Decision.Approved => "Согласовано",
        Decision.Rejected => "Отклонено",
        Decision.Reviewed => "Проверено",
        _ => decision.ToString()
    };

    private async Task<List<ApprovalHistoryDto>> LoadApprovalHistory(int requestId)
    {
        return await _db.ActionHistories
            .Where(h => h.DocumentId == requestId)
            .Include(h => h.User)
            .Include(h => h.Stage)
            .Include(h => h.WorkflowStep)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new ApprovalHistoryDto
            {
                Id = h.Id,
                StageName = h.Stage != null ? h.Stage.Name
                          : h.WorkflowStep != null ? h.WorkflowStep.StepName
                          : "—",
                StagePosition = h.Stage != null ? h.Stage.RequiredPosition
                              : h.WorkflowStep != null ? h.WorkflowStep.TargetPosition
                              : "—",
                Decision = h.Decision.ToString(),
                DecisionDisplay = DecisionToRussian(h.Decision),
                UserName = $"{h.User.LastName} {h.User.FirstName}",
                UserPosition = h.User.Position,
                Comment = h.Comment,
                CreatedAt = h.CreatedAt
            })
            .ToListAsync();
    }

    /// <summary>Батч-загрузка истории сразу для нескольких заявок (избегаем N+1).</summary>
    private async Task<Dictionary<int, List<ApprovalHistoryDto>>> LoadApprovalHistoriesForRequests(
        List<int> requestIds)
    {
        if (requestIds.Count == 0)
            return new Dictionary<int, List<ApprovalHistoryDto>>();

        var rows = await _db.ActionHistories
            .Where(h => requestIds.Contains(h.DocumentId))
            .Include(h => h.User)
            .Include(h => h.Stage)
            .Include(h => h.WorkflowStep)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new
            {
                h.DocumentId,
                Dto = new ApprovalHistoryDto
                {
                    Id = h.Id,
                    StageName = h.Stage != null ? h.Stage.Name
                              : h.WorkflowStep != null ? h.WorkflowStep.StepName
                              : "—",
                    StagePosition = h.Stage != null ? h.Stage.RequiredPosition
                                  : h.WorkflowStep != null ? h.WorkflowStep.TargetPosition
                                  : "—",
                    Decision = h.Decision.ToString(),
                    DecisionDisplay = DecisionToRussian(h.Decision),
                    UserName = $"{h.User.LastName} {h.User.FirstName}",
                    UserPosition = h.User.Position,
                    Comment = h.Comment,
                    CreatedAt = h.CreatedAt
                }
            })
            .ToListAsync();

        return rows
            .GroupBy(x => x.DocumentId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Dto).ToList());
    }

    private TmcRequestDto MapToDto(TmcRequest r) => MapToDto(r, _totalStages);

    private static TmcRequestDto MapToDto(TmcRequest r, int totalStages)
    {
        // Для кастомных цепочек переопределяем stage-данные из WorkflowStep
        string? stageName = r.CurrentStage?.Name;
        string? stagePosition = r.CurrentStage?.RequiredPosition;
        string? stageDescription = r.CurrentStage?.Description;
        int stageOrder = r.CurrentStage?.OrderSequence ?? 0;
        int total = totalStages;

        if (r.WorkflowChainId.HasValue)
        {
            // Общий размер цепочки — количество шагов в WorkflowChain
            total = r.WorkflowChain?.Steps?.Count ?? 0;

            if (r.CurrentWorkflowStep is not null)
            {
                stageName = r.CurrentWorkflowStep.StepName;
                stagePosition = r.CurrentWorkflowStep.TargetPosition;
                stageDescription = null;
                stageOrder = r.CurrentWorkflowStep.Order;
            }
            else
            {
                // Кастомная цепочка, шаг = null → возврат инициатору (после Rejected)
                stageName = "Инициатор";
                stagePosition = "Инициатор";
                stageDescription = null;
                stageOrder = 0;
            }
        }

        return new TmcRequestDto
        {
            Id = r.Id,
            InitiatorUserId = r.InitiatorUserId,
            InitiatorName = r.InitiatorUser != null
                ? $"{r.InitiatorUser.LastName} {r.InitiatorUser.FirstName}"
                : "Неизвестный пользователь",
            ProjectName = r.ProjectName,
            Status = r.Status.ToString(),
            StatusDisplay = StatusToRussian(r.Status),
            CurrentStageId = r.CurrentStageId,
            CurrentStageName = stageName,
            CurrentStagePosition = stagePosition,
            CurrentStageDescription = stageDescription,
            CurrentStageOrder = stageOrder,
            TotalStages = total,
            WorkflowChainId = r.WorkflowChainId,
            WorkflowChainName = r.WorkflowChain?.Name,
            ResponsibleUserId = r.ResponsibleUserId,
            ResponsibleUserName = r.ResponsibleUser != null
                ? $"{r.ResponsibleUser.LastName} {r.ResponsibleUser.FirstName}".Trim()
                : null,
            ResponsibleUserPosition = r.ResponsibleUser?.Position,
            CreatedAt = r.CreatedAt,
            Items = r.Items.Select(i => new TmcRequestItemDto
            {
                Id = i.Id,
                GroupId = i.GroupId,
                GroupName = i.Group != null ? $"{i.Group.Code}. {i.Group.Name}" : null,
                SubgroupId = i.SubgroupId,
                SubgroupName = i.Subgroup != null ? $"{i.Subgroup.Code}. {i.Subgroup.Name}" : null,
                Name = i.Name,
                Quantity = i.Quantity,
                Unit = i.Unit,
                Price = i.Price,
                PlannedDeliveryDate = i.PlannedDeliveryDate,
                InvoiceLink = i.InvoiceLink,
                Comment = i.Comment,
                InitiatorName = i.InitiatorName,
                InitiatorPosition = i.InitiatorPosition
            }).ToList()
        };
    }

    private static TmcRequestDto MapToDto(
        TmcRequest r,
        int totalStages,
        List<ApprovalHistoryDto> history)
    {
        var dto = MapToDto(r, totalStages);
        dto.ApprovalHistory = history;
        return dto;
    }
}
