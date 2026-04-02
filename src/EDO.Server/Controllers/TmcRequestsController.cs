using System.Security.Claims;
using EDO.Server.Data;
using EDO.Server.DTOs;
using EDO.Server.Models;
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

    public TmcRequestsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<TmcRequestDto>>> GetAll()
    {
        var items = await _db.TmcRequests
            .Include(r => r.InitiatorUser)
            .Include(r => r.CurrentStage)
            .Include(r => r.Items).ThenInclude(i => i.Tmc)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => MapToDto(r))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<TmcRequestDto>>> GetMy()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var items = await _db.TmcRequests
            .Include(r => r.InitiatorUser)
            .Include(r => r.CurrentStage)
            .Include(r => r.Items).ThenInclude(i => i.Tmc)
            .Where(r => r.InitiatorUserId == userId.Value)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => MapToDto(r))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TmcRequestDto>> GetById(int id)
    {
        var entity = await _db.TmcRequests
            .Include(r => r.InitiatorUser)
            .Include(r => r.CurrentStage)
            .Include(r => r.Items).ThenInclude(i => i.Tmc)
            .FirstOrDefaultAsync(r => r.Id == id);

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
            Status = TmcRequestStatus.Draft,
            Items = dto.Items.Select(i => new TmcRequestItem
            {
                TmcId = i.TmcId,
                Quantity = i.Quantity
            }).ToList()
        };

        _db.TmcRequests.Add(entity);
        await _db.SaveChangesAsync();

        // Перезагрузим с навигациями
        var created = await _db.TmcRequests
            .Include(r => r.InitiatorUser)
            .Include(r => r.CurrentStage)
            .Include(r => r.Items).ThenInclude(i => i.Tmc)
            .FirstAsync(r => r.Id == entity.Id);

        return Created($"/api/tmcrequests/{entity.Id}", MapToDto(created));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TmcRequestDto>> Update(int id, [FromBody] UpdateTmcRequestDto dto)
    {
        var entity = await _db.TmcRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (entity is null)
            return NotFound(new { message = "Заявка не найдена." });

        if (entity.Status != TmcRequestStatus.Draft)
            return BadRequest(new { message = "Редактировать можно только заявки в статусе 'Черновик'." });

        if (dto.Items.Count == 0)
            return BadRequest(new { message = "Заявка должна содержать хотя бы одну позицию." });

        // Удаляем старые строки и добавляем новые
        _db.TmcRequestItems.RemoveRange(entity.Items);
        entity.Items = dto.Items.Select(i => new TmcRequestItem
        {
            TmcId = i.TmcId,
            Quantity = i.Quantity
        }).ToList();

        await _db.SaveChangesAsync();

        var updated = await _db.TmcRequests
            .Include(r => r.InitiatorUser)
            .Include(r => r.CurrentStage)
            .Include(r => r.Items).ThenInclude(i => i.Tmc)
            .FirstAsync(r => r.Id == entity.Id);

        return Ok(MapToDto(updated));
    }

    [HttpPost("{id}/send")]
    public async Task<ActionResult<TmcRequestDto>> Send(int id)
    {
        var entity = await _db.TmcRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (entity is null)
            return NotFound(new { message = "Заявка не найдена." });

        if (entity.Status != TmcRequestStatus.Draft)
            return BadRequest(new { message = "Отправить можно только заявку в статусе 'Черновик'." });

        if (entity.Items.Count == 0)
            return BadRequest(new { message = "Нельзя отправить пустую заявку." });

        // Находим первый этап цепочки согласования
        var firstStage = await _db.ApprovalStages
            .OrderBy(s => s.OrderSequence)
            .FirstOrDefaultAsync();

        if (firstStage is null)
            return BadRequest(new { message = "Цепочка согласования не настроена. Обратитесь к администратору." });

        entity.Status = TmcRequestStatus.InApproval;
        entity.CurrentStageId = firstStage.Id;

        await _db.SaveChangesAsync();

        var updated = await _db.TmcRequests
            .Include(r => r.InitiatorUser)
            .Include(r => r.CurrentStage)
            .Include(r => r.Items).ThenInclude(i => i.Tmc)
            .FirstAsync(r => r.Id == entity.Id);

        return Ok(MapToDto(updated));
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static TmcRequestDto MapToDto(TmcRequest r) => new()
    {
        Id = r.Id,
        InitiatorUserId = r.InitiatorUserId,
        InitiatorName = $"{r.InitiatorUser.LastName} {r.InitiatorUser.FirstName}",
        Status = r.Status.ToString(),
        CurrentStageId = r.CurrentStageId,
        CurrentStageName = r.CurrentStage?.Name,
        CreatedAt = r.CreatedAt,
        Items = r.Items.Select(i => new TmcRequestItemDto
        {
            Id = i.Id,
            TmcId = i.TmcId,
            TmcName = i.Tmc.Name,
            Quantity = i.Quantity
        }).ToList()
    };
}
