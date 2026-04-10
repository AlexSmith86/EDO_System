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
public class WorkflowChainsController : ControllerBase
{
    private readonly AppDbContext _db;

    public WorkflowChainsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<WorkflowChainDto>>> GetAll()
    {
        var chains = await _db.WorkflowChains
            .Include(c => c.Steps.OrderBy(s => s.Order))
            .OrderBy(c => c.Name)
            .Select(c => new WorkflowChainDto
            {
                Id = c.Id,
                Name = c.Name,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                Steps = c.Steps.OrderBy(s => s.Order).Select(s => new WorkflowStepDto
                {
                    Id = s.Id,
                    WorkflowChainId = s.WorkflowChainId,
                    StepName = s.StepName,
                    TargetPosition = s.TargetPosition,
                    Order = s.Order
                }).ToList()
            })
            .ToListAsync();

        return Ok(chains);
    }

    [HttpGet("active")]
    public async Task<ActionResult<List<WorkflowChainDto>>> GetActive()
    {
        var chains = await _db.WorkflowChains
            .Where(c => c.IsActive)
            .Include(c => c.Steps.OrderBy(s => s.Order))
            .OrderBy(c => c.Name)
            .Select(c => new WorkflowChainDto
            {
                Id = c.Id,
                Name = c.Name,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                Steps = c.Steps.OrderBy(s => s.Order).Select(s => new WorkflowStepDto
                {
                    Id = s.Id,
                    WorkflowChainId = s.WorkflowChainId,
                    StepName = s.StepName,
                    TargetPosition = s.TargetPosition,
                    Order = s.Order
                }).ToList()
            })
            .ToListAsync();

        return Ok(chains);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkflowChainDto>> GetById(int id)
    {
        var chain = await _db.WorkflowChains
            .Include(c => c.Steps.OrderBy(s => s.Order))
            .FirstOrDefaultAsync(c => c.Id == id);

        if (chain is null)
            return NotFound(new { message = "Цепочка не найдена." });

        return Ok(MapToDto(chain));
    }

    [HttpPost]
    [Authorize(Roles = "Администратор")]
    public async Task<ActionResult<WorkflowChainDto>> Create([FromBody] CreateWorkflowChainDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Укажите название цепочки." });

        if (dto.Steps.Count == 0)
            return BadRequest(new { message = "Добавьте хотя бы один шаг." });

        if (dto.Steps.Any(s => s.Order < 0))
            return BadRequest(new { message = "Номер этапа не может быть меньше 0." });

        if (dto.Steps.GroupBy(s => s.Order).Any(g => g.Count() > 1))
            return BadRequest(new { message = "Номера этапов должны быть уникальными." });

        var chain = new WorkflowChain
        {
            Name = dto.Name,
            IsActive = true,
            Steps = dto.Steps.Select(s => new WorkflowStep
            {
                StepName = s.StepName,
                TargetPosition = s.TargetPosition,
                Order = s.Order
            }).ToList()
        };

        _db.WorkflowChains.Add(chain);
        await _db.SaveChangesAsync();

        return Created($"/api/workflowchains/{chain.Id}", MapToDto(chain));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Администратор")]
    public async Task<ActionResult<WorkflowChainDto>> Update(int id, [FromBody] UpdateWorkflowChainDto dto)
    {
        var chain = await _db.WorkflowChains
            .Include(c => c.Steps)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (chain is null)
            return NotFound(new { message = "Цепочка не найдена." });

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Укажите название цепочки." });

        if (dto.Steps.Count == 0)
            return BadRequest(new { message = "Добавьте хотя бы один шаг." });

        if (dto.Steps.Any(s => s.Order < 0))
            return BadRequest(new { message = "Номер этапа не может быть меньше 0." });

        if (dto.Steps.GroupBy(s => s.Order).Any(g => g.Count() > 1))
            return BadRequest(new { message = "Номера этапов должны быть уникальными." });

        chain.Name = dto.Name;
        chain.IsActive = dto.IsActive;

        _db.WorkflowSteps.RemoveRange(chain.Steps);
        chain.Steps = dto.Steps.Select(s => new WorkflowStep
        {
            StepName = s.StepName,
            TargetPosition = s.TargetPosition,
            Order = s.Order
        }).ToList();

        await _db.SaveChangesAsync();

        return Ok(MapToDto(chain));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Администратор")]
    public async Task<IActionResult> Delete(int id)
    {
        var chain = await _db.WorkflowChains
            .Include(c => c.Steps)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (chain is null)
            return NotFound(new { message = "Цепочка не найдена." });

        var linkedRequests = await _db.TmcRequests
            .Where(r => r.WorkflowChainId == id)
            .ToListAsync();

        var firstStandardStage = await _db.ApprovalStages
            .Where(s => s.OrderSequence == 1)
            .FirstOrDefaultAsync();

        var initiatorStage = await _db.ApprovalStages
            .Where(s => s.OrderSequence == 0)
            .FirstOrDefaultAsync();

        foreach (var r in linkedRequests)
        {
            var wasCustomInApproval = r.Status == TmcRequestStatus.InApproval
                                      && r.CurrentWorkflowStepId.HasValue;

            r.WorkflowChainId = null;
            r.CurrentWorkflowStepId = null;

            if (wasCustomInApproval)
            {
                if (firstStandardStage is not null)
                {
                    r.CurrentStageId = firstStandardStage.Id;
                }
                else
                {
                    r.Status = TmcRequestStatus.Rework;
                    r.CurrentStageId = initiatorStage?.Id;
                }
            }
        }

        // Обнуляем ссылки на шаги цепочки в истории действий —
        // ActionHistory.WorkflowStepId настроен на Restrict, иначе будет FK violation.
        var stepIds = chain.Steps.Select(s => s.Id).ToList();
        if (stepIds.Count > 0)
        {
            var historyLinks = await _db.ActionHistories
                .Where(h => h.WorkflowStepId.HasValue && stepIds.Contains(h.WorkflowStepId!.Value))
                .ToListAsync();

            foreach (var h in historyLinks)
                h.WorkflowStepId = null;
        }

        _db.WorkflowSteps.RemoveRange(chain.Steps);
        _db.WorkflowChains.Remove(chain);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static WorkflowChainDto MapToDto(WorkflowChain chain) => new()
    {
        Id = chain.Id,
        Name = chain.Name,
        IsActive = chain.IsActive,
        CreatedAt = chain.CreatedAt,
        Steps = chain.Steps.OrderBy(s => s.Order).Select(s => new WorkflowStepDto
        {
            Id = s.Id,
            WorkflowChainId = s.WorkflowChainId,
            StepName = s.StepName,
            TargetPosition = s.TargetPosition,
            Order = s.Order
        }).ToList()
    };
}
