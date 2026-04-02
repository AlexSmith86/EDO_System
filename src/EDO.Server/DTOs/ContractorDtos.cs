using System.ComponentModel.DataAnnotations;

namespace EDO.Server.DTOs;

public class ContractorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Inn { get; set; }
    public string ContractorType { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
}

public class CreateContractorDto
{
    [Required, MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(12)]
    public string? Inn { get; set; }

    [Required]
    public string ContractorType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ExternalId { get; set; }
}

public class UpdateContractorDto
{
    [Required, MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(12)]
    public string? Inn { get; set; }

    [Required]
    public string ContractorType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ExternalId { get; set; }
}
