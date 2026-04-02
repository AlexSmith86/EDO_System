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
public class TmcsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TmcsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<TmcDto>>> GetAll()
    {
        var items = await _db.Tmcs
            .Select(t => new TmcDto
            {
                Id = t.Id,
                Name = t.Name,
                Article = t.Article,
                ExternalId = t.ExternalId,
                StockBalance = t.StockBalance
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<TmcDto>> Create([FromBody] CreateTmcDto dto)
    {
        var entity = new Tmc
        {
            Name = dto.Name,
            Article = dto.Article,
            ExternalId = dto.ExternalId,
            StockBalance = dto.StockBalance
        };

        _db.Tmcs.Add(entity);
        await _db.SaveChangesAsync();

        return Created($"/api/tmcs/{entity.Id}", new TmcDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Article = entity.Article,
            ExternalId = entity.ExternalId,
            StockBalance = entity.StockBalance
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TmcDto>> Update(int id, [FromBody] UpdateTmcDto dto)
    {
        var entity = await _db.Tmcs.FindAsync(id);
        if (entity is null)
            return NotFound(new { message = "ТМЦ не найдена." });

        entity.Name = dto.Name;
        entity.Article = dto.Article;
        entity.ExternalId = dto.ExternalId;
        entity.StockBalance = dto.StockBalance;

        await _db.SaveChangesAsync();

        return Ok(new TmcDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Article = entity.Article,
            ExternalId = entity.ExternalId,
            StockBalance = entity.StockBalance
        });
    }
}
