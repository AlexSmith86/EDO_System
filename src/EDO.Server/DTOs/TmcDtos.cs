using System.ComponentModel.DataAnnotations;

namespace EDO.Server.DTOs;

public class TmcDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Article { get; set; }
    public string? ExternalId { get; set; }
    public decimal StockBalance { get; set; }
}

public class CreateTmcDto
{
    [Required, MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Article { get; set; }

    [MaxLength(100)]
    public string? ExternalId { get; set; }

    public decimal StockBalance { get; set; }
}

public class UpdateTmcDto
{
    [Required, MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Article { get; set; }

    [MaxLength(100)]
    public string? ExternalId { get; set; }

    public decimal StockBalance { get; set; }
}
