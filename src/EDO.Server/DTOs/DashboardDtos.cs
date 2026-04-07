namespace EDO.Server.DTOs;

public class DashboardStatsDto
{
    /// <summary>Заявки, ожидающие согласования текущего пользователя</summary>
    public int AwaitingMyApprovalCount { get; set; }

    /// <summary>Активные заявки текущего пользователя</summary>
    public int MyActiveRequestsCount { get; set; }

    /// <summary>Всего заявок в работе по компании</summary>
    public int TotalActiveRequestsCount { get; set; }

    /// <summary>Количество согласованных заявок</summary>
    public int ApprovedCount { get; set; }

    /// <summary>Распределение заявок по статусам</summary>
    public List<StatusCountDto> RequestsByStatus { get; set; } = new();
}

public class StatusCountDto
{
    public string Status { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public int Count { get; set; }
}
