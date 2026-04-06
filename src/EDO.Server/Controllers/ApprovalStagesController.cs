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
public class ApprovalStagesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ApprovalStagesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ApprovalStageDto>>> GetAll()
    {
        var stages = await _db.ApprovalStages
            .Include(s => s.Role)
            .OrderBy(s => s.OrderSequence)
            .Select(s => new ApprovalStageDto
            {
                Id = s.Id,
                Name = s.Name,
                RequiredPosition = s.RequiredPosition,
                RoleId = s.RoleId,
                RoleName = s.Role != null ? s.Role.Name : null,
                OrderSequence = s.OrderSequence
            })
            .ToListAsync();

        return Ok(stages);
    }

    [HttpPost]
    public async Task<ActionResult<ApprovalStageDto>> Create([FromBody] CreateApprovalStageDto dto)
    {
        if (await _db.ApprovalStages.AnyAsync(s => s.OrderSequence == dto.OrderSequence))
            return Conflict(new { message = $"Этап с порядковым номером {dto.OrderSequence} уже существует." });

        var entity = new ApprovalStage
        {
            Name = dto.Name,
            RequiredPosition = dto.RequiredPosition ?? "",
            RoleId = dto.RoleId,
            OrderSequence = dto.OrderSequence
        };

        _db.ApprovalStages.Add(entity);
        await _db.SaveChangesAsync();

        return Created($"/api/approvalstages/{entity.Id}", new ApprovalStageDto
        {
            Id = entity.Id,
            Name = entity.Name,
            RequiredPosition = entity.RequiredPosition,
            RoleId = entity.RoleId,
            OrderSequence = entity.OrderSequence
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApprovalStageDto>> Update(int id, [FromBody] UpdateApprovalStageDto dto)
    {
        var entity = await _db.ApprovalStages.FirstOrDefaultAsync(s => s.Id == id);
        if (entity is null)
            return NotFound(new { message = "Этап не найден." });

        if (await _db.ApprovalStages.AnyAsync(s => s.OrderSequence == dto.OrderSequence && s.Id != id))
            return Conflict(new { message = $"Этап с порядковым номером {dto.OrderSequence} уже существует." });

        entity.Name = dto.Name;
        entity.RequiredPosition = dto.RequiredPosition ?? "";
        entity.RoleId = dto.RoleId;
        entity.OrderSequence = dto.OrderSequence;

        await _db.SaveChangesAsync();

        return Ok(new ApprovalStageDto
        {
            Id = entity.Id,
            Name = entity.Name,
            RequiredPosition = entity.RequiredPosition,
            RoleId = entity.RoleId,
            OrderSequence = entity.OrderSequence
        });
    }
}
