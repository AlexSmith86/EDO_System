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
public class ContractorsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ContractorsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ContractorDto>>> GetAll()
    {
        var items = await _db.Contractors
            .Select(c => new ContractorDto
            {
                Id = c.Id,
                Name = c.Name,
                Inn = c.Inn,
                ContractorType = c.ContractorType.ToString(),
                ExternalId = c.ExternalId
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<ContractorDto>> Create([FromBody] CreateContractorDto dto)
    {
        if (!Enum.TryParse<ContractorType>(dto.ContractorType, true, out var contractorType))
            return BadRequest(new { message = "Неверный тип контрагента. Допустимые: Supplier, Client." });

        var entity = new Contractor
        {
            Name = dto.Name,
            Inn = dto.Inn,
            ContractorType = contractorType,
            ExternalId = dto.ExternalId
        };

        _db.Contractors.Add(entity);
        await _db.SaveChangesAsync();

        return Created($"/api/contractors/{entity.Id}", new ContractorDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Inn = entity.Inn,
            ContractorType = entity.ContractorType.ToString(),
            ExternalId = entity.ExternalId
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ContractorDto>> Update(int id, [FromBody] UpdateContractorDto dto)
    {
        var entity = await _db.Contractors.FindAsync(id);
        if (entity is null)
            return NotFound(new { message = "Контрагент не найден." });

        if (!Enum.TryParse<ContractorType>(dto.ContractorType, true, out var contractorType))
            return BadRequest(new { message = "Неверный тип контрагента. Допустимые: Supplier, Client." });

        entity.Name = dto.Name;
        entity.Inn = dto.Inn;
        entity.ContractorType = contractorType;
        entity.ExternalId = dto.ExternalId;

        await _db.SaveChangesAsync();

        return Ok(new ContractorDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Inn = entity.Inn,
            ContractorType = entity.ContractorType.ToString(),
            ExternalId = entity.ExternalId
        });
    }
}
