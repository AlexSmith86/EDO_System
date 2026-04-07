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
public class TemplatesController : ControllerBase
{
    private readonly AppDbContext _db;

    public TemplatesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<TemplateDto>>> GetAll()
    {
        var items = await _db.DocumentTemplates
            .Select(t => new TemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                FilePath = t.FilePath,
                ProcessType = t.ProcessType
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<TemplateDto>> Create([FromBody] CreateTemplateDto dto)
    {
        var entity = new DocumentTemplate
        {
            Name = dto.Name,
            FilePath = dto.FilePath,
            ProcessType = dto.ProcessType
        };

        _db.DocumentTemplates.Add(entity);
        await _db.SaveChangesAsync();

        return Created($"/api/templates/{entity.Id}", new TemplateDto
        {
            Id = entity.Id,
            Name = entity.Name,
            FilePath = entity.FilePath,
            ProcessType = entity.ProcessType
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TemplateDto>> Update(int id, [FromBody] UpdateTemplateDto dto)
    {
        var entity = await _db.DocumentTemplates.FindAsync(id);
        if (entity is null)
            return NotFound(new { message = "Шаблон не найден." });

        entity.Name = dto.Name;
        entity.FilePath = dto.FilePath;
        entity.ProcessType = dto.ProcessType;

        await _db.SaveChangesAsync();

        return Ok(new TemplateDto
        {
            Id = entity.Id,
            Name = entity.Name,
            FilePath = entity.FilePath,
            ProcessType = entity.ProcessType
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Администратор")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.DocumentTemplates.FindAsync(id);
        if (entity is null)
            return NotFound(new { message = "Шаблон не найден." });

        _db.DocumentTemplates.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
