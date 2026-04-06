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
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var user = await _db.Users.FindAsync(userId.Value);
        if (user is null) return Unauthorized();

        // Активные статусы (в работе)
        var activeStatuses = new[]
        {
            TmcRequestStatus.Draft,
            TmcRequestStatus.InApproval,
            TmcRequestStatus.Rework
        };

        // 1. Мои активные заявки
        var myActiveCount = await _db.TmcRequests
            .CountAsync(r => r.InitiatorUserId == userId.Value
                          && activeStatuses.Contains(r.Status));

        // 2. Всего заявок в работе
        var totalActiveCount = await _db.TmcRequests
            .CountAsync(r => activeStatuses.Contains(r.Status));

        // 3. Сумма заявок в работе (сумма Quantity * кол-во позиций как метрика)
        var totalActiveSum = await _db.TmcRequests
            .Where(r => activeStatuses.Contains(r.Status))
            .SelectMany(r => r.Items)
            .SumAsync(i => i.Quantity);

        // 4. Ожидают моего согласования
        var awaitingCount = 0;
        var pendingRequests = await _db.TmcRequests
            .Include(r => r.CurrentStage)
            .Where(r => (r.Status == TmcRequestStatus.InApproval || r.Status == TmcRequestStatus.Rework)
                     && r.CurrentStageId != null)
            .ToListAsync();

        foreach (var r in pendingRequests)
        {
            if (r.CurrentStage is null) continue;

            if (r.CurrentStage.RequiredPosition == "Инициатор")
            {
                if (r.InitiatorUserId == userId.Value)
                    awaitingCount++;
            }
            else if (string.Equals(user.Position, r.CurrentStage.RequiredPosition,
                         StringComparison.OrdinalIgnoreCase))
            {
                awaitingCount++;
            }
        }

        // 5. Распределение по статусам
        var statusGroups = await _db.TmcRequests
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var requestsByStatus = statusGroups.Select(g => new StatusCountDto
        {
            Status = g.Status.ToString(),
            StatusDisplay = StatusToRussian(g.Status),
            Count = g.Count
        }).ToList();

        return Ok(new DashboardStatsDto
        {
            AwaitingMyApprovalCount = awaitingCount,
            MyActiveRequestsCount = myActiveCount,
            TotalActiveRequestsCount = totalActiveCount,
            TotalActiveRequestsSum = totalActiveSum,
            RequestsByStatus = requestsByStatus
        });
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
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
}
