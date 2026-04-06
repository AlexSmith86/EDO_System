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

    public TmcRequestsController(AppDbContext db, IWorkflowEngineService workflow)
    {
        _db = db;
        _workflow = workflow;
    }

    [HttpGet]
    public async Task<ActionResult<List<TmcRequestDto>>> GetAll()
    {
        var entities = await LoadAllRequests()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(entities.Select(MapToDto).ToList());
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<TmcRequestDto>>> GetMy()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var entities = await LoadAllRequests()
            .Where(r => r.InitiatorUserId == userId.Value)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(entities.Select(MapToDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TmcRequestDto>> GetById(int id)
    {
        var entity = await LoadRequest(id);
        if (entity is null)
            return NotFound(new { message = "Заявка не найдена." });

        return Ok(MapToDto(entity));
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

        var created = await LoadRequest(entity.Id);
        return Created($"/api/tmcrequests/{entity.Id}", MapToDto(created!));
    }

    [HttpPut("{id}")]
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

        var updated = await LoadRequest(entity.Id);
        return Ok(MapToDto(updated!));
    }

    /// <summary>Отправить заявку на согласование (из Draft или Rework → InApproval, этап 1)</summary>
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

        // Этап 1 — ПТО (OrderSequence = 1)
        var firstApprovalStage = await _db.ApprovalStages
            .Where(s => s.OrderSequence == 1)
            .FirstOrDefaultAsync();

        if (firstApprovalStage is null)
            return BadRequest(new { message = "Цепочка согласования не настроена. Обратитесь к администратору." });

        entity.Status = TmcRequestStatus.InApproval;
        entity.CurrentStageId = firstApprovalStage.Id;
        await _db.SaveChangesAsync();

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

        // Загружаем все заявки на согласовании / на доработке с текущим этапом
        var candidates = await LoadAllRequests()
            .Where(r => (r.Status == TmcRequestStatus.InApproval || r.Status == TmcRequestStatus.Rework)
                     && r.CurrentStageId != null)
            .ToListAsync();

        // Фильтруем по должности
        var result = new List<TmcRequestDto>();
        foreach (var r in candidates)
        {
            if (r.CurrentStage is null) continue;

            bool canAct;
            if (r.CurrentStage.RequiredPosition == "Инициатор")
            {
                // Этапы "Инициатор" — только создатель заявки
                canAct = r.InitiatorUserId == userId.Value;
            }
            else
            {
                // Остальные этапы — совпадение должности
                canAct = string.Equals(user.Position, r.CurrentStage.RequiredPosition,
                    StringComparison.OrdinalIgnoreCase);
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
            .FirstOrDefaultAsync(r => r.Id == id);

        if (entity is null)
            return NotFound(new { message = "Заявка не найдена." });

        if ((entity.Status != TmcRequestStatus.InApproval && entity.Status != TmcRequestStatus.Rework)
            || entity.CurrentStageId is null)
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
            CurrentStageId = entity.CurrentStageId.Value,
            Decision = decision,
            Comment = dto.Comment
        });

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        if (result.IsRejected)
        {
            // Отклонение → На доработку, возврат к этапу 0 (Инициатор)
            entity.Status = TmcRequestStatus.Rework;
            entity.CurrentStageId = result.NextStageId; // этап 0
        }
        else if (result.IsCompleted)
        {
            // Последний этап пройден → Выполнено
            entity.Status = TmcRequestStatus.Completed;
            entity.CurrentStageId = null;
        }
        else
        {
            // Переход к следующему этапу
            entity.CurrentStageId = result.NextStageId;
        }

        await _db.SaveChangesAsync();

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

    private static TmcRequestDto MapToDto(TmcRequest r) => new()
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
        CurrentStageName = r.CurrentStage?.Name,
        CurrentStagePosition = r.CurrentStage?.RequiredPosition,
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
