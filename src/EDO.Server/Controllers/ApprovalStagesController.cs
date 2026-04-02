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
                RoleId = s.RoleId,
                RoleName = s.Role.Name,
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
            RoleId = dto.RoleId,
            OrderSequence = dto.OrderSequence
        };

        _db.ApprovalStages.Add(entity);
        await _db.SaveChangesAsync();

        await _db.Entry(entity).Reference(s => s.Role).LoadAsync();

        return Created($"/api/approvalstages/{entity.Id}", new ApprovalStageDto
        {
            Id = entity.Id,
            Name = entity.Name,
            RoleId = entity.RoleId,
            RoleName = entity.Role.Name,
            OrderSequence = entity.OrderSequence
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApprovalStageDto>> Update(int id, [FromBody] UpdateApprovalStageDto dto)
    {
        var entity = await _db.ApprovalStages.Include(s => s.Role).FirstOrDefaultAsync(s => s.Id == id);
        if (entity is null)
            return NotFound(new { message = "Этап не найден." });

        if (await _db.ApprovalStages.AnyAsync(s => s.OrderSequence == dto.OrderSequence && s.Id != id))
            return Conflict(new { message = $"Этап с порядковым номером {dto.OrderSequence} уже существует." });

        entity.Name = dto.Name;
        entity.RoleId = dto.RoleId;
        entity.OrderSequence = dto.OrderSequence;

        await _db.SaveChangesAsync();

        await _db.Entry(entity).Reference(s => s.Role).LoadAsync();

        return Ok(new ApprovalStageDto
        {
            Id = entity.Id,
            Name = entity.Name,
            RoleId = entity.RoleId,
            RoleName = entity.Role.Name,
            OrderSequence = entity.OrderSequence
        });
    }
}
