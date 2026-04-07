using System.ComponentModel.DataAnnotations;

namespace EDO.Server.DTOs;

public class TmcRequestDto
{
    public int Id { get; set; }
    public int InitiatorUserId { get; set; }
    public string InitiatorName { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public int? CurrentStageId { get; set; }
    public string? CurrentStageName { get; set; }
    public string? CurrentStagePosition { get; set; }
    public string? CurrentStageDescription { get; set; }
    public int CurrentStageOrder { get; set; }
    public int TotalStages { get; set; }
    public int? WorkflowChainId { get; set; }
    public string? WorkflowChainName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TmcRequestItemDto> Items { get; set; } = new();
    public List<ApprovalHistoryDto> ApprovalHistory { get; set; } = new();
}

public class ApprovalHistoryDto
{
    public int Id { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string StagePosition { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public string DecisionDisplay { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TmcRequestItemDto
{
    public int Id { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public int? SubgroupId { get; set; }
    public string? SubgroupName { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal? Price { get; set; }
    public DateTime? PlannedDeliveryDate { get; set; }
    public string? InvoiceLink { get; set; }
    public string? Comment { get; set; }
    public string? InitiatorName { get; set; }
    public string? InitiatorPosition { get; set; }
}

public class CreateTmcRequestDto
{
    public string? ProjectName { get; set; }
    public int? WorkflowChainId { get; set; }
    public List<CreateTmcRequestItemDto> Items { get; set; } = new();
}

public class CreateTmcRequestItemDto
{
    public int? GroupId { get; set; }
    public int? SubgroupId { get; set; }

    [Required, MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    [Range(0.0001, double.MaxValue, ErrorMessage = "Количество должно быть больше 0")]
    public decimal Quantity { get; set; }

    [MaxLength(50)]
    public string? Unit { get; set; }

    public decimal? Price { get; set; }

    public DateTime? PlannedDeliveryDate { get; set; }

    [MaxLength(1000)]
    public string? InvoiceLink { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    [MaxLength(300)]
    public string? InitiatorName { get; set; }

    [MaxLength(300)]
    public string? InitiatorPosition { get; set; }
}

public class UpdateTmcRequestDto
{
    public string? ProjectName { get; set; }
    public List<CreateTmcRequestItemDto> Items { get; set; } = new();
}

public class SubmitDecisionDto
{
    [Required]
    public string Decision { get; set; } = string.Empty;

    public string? Comment { get; set; }
}
