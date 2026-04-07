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
    private int _totalStages;
    private const string AnyPosition = "Любая должность";

    public TmcRequestsController(AppDbContext db, IWorkflowEngineService workflow)
    {
        _db = db;
        _workflow = workflow;
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

        return Ok(entities.Select(r => MapToDto(r)).ToList());
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

    /// <summary>Отправить заявку на согласование (из Draft или Rework → InApproval)</summary>
    [HttpPost("{id}/send")]
    public async Task<ActionResult<TmcRequestDto>> Send(int id)
    {
        var entity = await _db.TmcRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (entity is null)
            return NotFound(new { message = "Заявка не найдена." });

        if (entity.Status != TmcRequestStatus.Draft && entity.Status != TmcRequestStatus.Rework)
            return BadRequest(new { message = "Отправить можно только заявку в статусе 'Черновик' или 'На доработке'." });

        if (entity.Items.Count == 0)
            return BadRequest(new { message = "Нельзя отправить пустую заявку." });

        if (entity.WorkflowChainId.HasValue)
        {
            // Кастомная цепочка — первый шаг
            var firstStep = await _db.WorkflowSteps
                .Where(s => s.WorkflowChainId == entity.WorkflowChainId.Value)
                .OrderBy(s => s.Order)
                .FirstOrDefaultAsync();

            if (firstStep is null)
                return BadRequest(new { message = "Кастомная цепочка пуста. Обратитесь к администратору." });

            entity.Status = TmcRequestStatus.InApproval;
            entity.CurrentWorkflowStepId = firstStep.Id;
            entity.CurrentStageId = null;
        }
        else
        {
            // Стандартный маршрут — этап 1 (OrderSequence = 1)
            var firstApprovalStage = await _db.ApprovalStages
                .Where(s => s.OrderSequence == 1)
                .FirstOrDefaultAsync();

            if (firstApprovalStage is null)
                return BadRequest(new { message = "Цепочка согласования не настроена. Обратитесь к администратору." });

            entity.Status = TmcRequestStatus.InApproval;
            entity.CurrentStageId = firstApprovalStage.Id;
            entity.CurrentWorkflowStepId = null;
        }

        await _db.SaveChangesAsync();

        await EnsureTotalStagesLoaded();
        var updated = await LoadRequest(entity.Id);
        return Ok(MapToDto(updated!));
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

            if (r.WorkflowChainId.HasValue && r.CurrentWorkflowStep is not null)
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

        return Ok(result.OrderByDescending(r => r.CreatedAt).ToList());
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

        var result = await _workflow.ProcessDecisionAsync(new WorkflowDecisionRequest
        {
            DocumentId = entity.Id,
            UserId = userId.Value,
            CurrentStageId = entity.CurrentStageId ?? 0,
            Decision = decision,
            Comment = dto.Comment,
            WorkflowChainId = entity.WorkflowChainId,
            CurrentWorkflowStepId = entity.CurrentWorkflowStepId
        });

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        if (entity.WorkflowChainId.HasValue)
        {
            // Кастомная цепочка
            if (result.IsRejected)
            {
                entity.Status = TmcRequestStatus.Rework;
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
            // Стандартный маршрут
            if (result.IsRejected)
            {
                entity.Status = TmcRequestStatus.Rework;
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

        await _db.SaveChangesAsync();

        await EnsureTotalStagesLoaded();
        var updated = await LoadRequest(entity.Id);
        return Ok(MapToDto(updated!));
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
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new ApprovalHistoryDto
            {
                Id = h.Id,
                StageName = h.Stage.Name,
                StagePosition = h.Stage.RequiredPosition,
                Decision = h.Decision.ToString(),
                DecisionDisplay = DecisionToRussian(h.Decision),
                UserName = $"{h.User.LastName} {h.User.FirstName}",
                Comment = h.Comment,
                CreatedAt = h.CreatedAt
            })
            .ToListAsync();
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

        if (r.WorkflowChainId.HasValue && r.CurrentWorkflowStep is not null)
        {
            stageName = r.CurrentWorkflowStep.StepName;
            stagePosition = r.CurrentWorkflowStep.TargetPosition;
            stageDescription = null;
            stageOrder = r.CurrentWorkflowStep.Order;
            // TotalStages для кастомных цепочек — количество шагов в цепочке
            total = r.WorkflowChain?.Steps?.Count ?? 0;
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
