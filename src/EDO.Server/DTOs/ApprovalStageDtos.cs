using System.ComponentModel.DataAnnotations;

namespace EDO.Server.DTOs;

public class ApprovalStageDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RequiredPosition { get; set; } = string.Empty;
    public int? RoleId { get; set; }
    public string? RoleName { get; set; }
    public int OrderSequence { get; set; }
}

public class CreateApprovalStageDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? RequiredPosition { get; set; }

    public int? RoleId { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int OrderSequence { get; set; }
}

public class UpdateApprovalStageDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? RequiredPosition { get; set; }

    public int? RoleId { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int OrderSequence { get; set; }
}
