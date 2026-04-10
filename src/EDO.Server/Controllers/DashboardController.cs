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

    // Должно совпадать с TmcRequestsController.AnyPosition,
    // чтобы счётчик «Ожидают моего согласования» на дашборде
    // давал такое же число, как страница /documents/approvals.
    private const string AnyPosition = "Любая должность";

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

        // 3. Согласованные заявки
        var approvedCount = await _db.TmcRequests
            .CountAsync(r => r.Status == TmcRequestStatus.Completed || r.Status == TmcRequestStatus.Approved);

        // 4. Ожидают моего согласования
        // Логика должна 1-в-1 совпадать с TmcRequestsController.GetPending:
        //  - поддерживаются И стандартный маршрут (CurrentStage),
        //    И кастомные цепочки (CurrentWorkflowStep);
        //  - делегирование: назначенный ответственный видит заявку всегда;
        //  - «Любая должность» на шаге кастомной цепочки открывает доступ всем.
        var awaitingCount = 0;
        var pendingRequests = await _db.TmcRequests
            .Include(r => r.CurrentStage)
            .Include(r => r.CurrentWorkflowStep)
            .Where(r => (r.Status == TmcRequestStatus.InApproval || r.Status == TmcRequestStatus.Rework)
                     && (r.CurrentStageId != null || r.CurrentWorkflowStepId != null))
            .ToListAsync();

        foreach (var r in pendingRequests)
        {
            bool canAct = false;

            // Делегирование: назначенный ответственный всегда видит заявку на текущем этапе
            if (r.ResponsibleUserId == userId.Value)
            {
                canAct = true;
            }
            else if (r.WorkflowChainId.HasValue && r.CurrentWorkflowStep is not null)
            {
                // Кастомная цепочка — проверяем TargetPosition шага
                canAct = string.Equals(r.CurrentWorkflowStep.TargetPosition, AnyPosition, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(user.Position, r.CurrentWorkflowStep.TargetPosition, StringComparison.OrdinalIgnoreCase);
            }
            else if (r.CurrentStage is not null)
            {
                // Стандартный маршрут
                if (r.CurrentStage.RequiredPosition == "Инициатор")
                {
                    canAct = r.InitiatorUserId == userId.Value;
                }
                else
                {
                    canAct = string.Equals(user.Position, r.CurrentStage.RequiredPosition,
                        StringComparison.OrdinalIgnoreCase);
                }
            }

            if (canAct)
                awaitingCount++;
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
            ApprovedCount = approvedCount,
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
