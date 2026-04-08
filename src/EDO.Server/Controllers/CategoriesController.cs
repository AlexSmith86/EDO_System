using EDO.Server.Data;
using EDO.Server.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EDO.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("groups")]
    public async Task<ActionResult<List<TmcGroupDto>>> GetGroups()
    {
        var items = await _db.TmcGroups
            .OrderBy(g => g.Code)
            .Select(g => new TmcGroupDto
            {
                Id = g.Id,
                Code = g.Code,
                Name = g.Name
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("groups/{id}/subgroups")]
    public async Task<ActionResult<List<TmcSubgroupDto>>> GetSubgroups(int id)
    {
        var items = await _db.TmcSubgroups
            .Where(s => s.GroupId == id)
            .OrderBy(s => s.SortOrder)
            .Select(s => new TmcSubgroupDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                GroupId = s.GroupId,
                IsHeader = s.IsHeader
            })
            .ToListAsync();

        return Ok(items);
    }
}
