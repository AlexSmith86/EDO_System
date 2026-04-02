using System.ComponentModel.DataAnnotations;

namespace EDO.Server.DTOs;

public class TmcRequestDto
{
    public int Id { get; set; }
    public int InitiatorUserId { get; set; }
    public string InitiatorName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? CurrentStageId { get; set; }
    public string? CurrentStageName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TmcRequestItemDto> Items { get; set; } = new();
}

public class TmcRequestItemDto
{
    public int Id { get; set; }
    public int TmcId { get; set; }
    public string TmcName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

public class CreateTmcRequestDto
{
    public List<CreateTmcRequestItemDto> Items { get; set; } = new();
}

public class CreateTmcRequestItemDto
{
    public int TmcId { get; set; }

    [Range(0.0001, double.MaxValue, ErrorMessage = "Количество должно быть больше 0")]
    public decimal Quantity { get; set; }
}

public class UpdateTmcRequestDto
{
    public List<CreateTmcRequestItemDto> Items { get; set; } = new();
}
