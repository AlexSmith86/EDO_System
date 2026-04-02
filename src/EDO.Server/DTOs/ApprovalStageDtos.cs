using System.ComponentModel.DataAnnotations;

namespace EDO.Server.DTOs;

public class ApprovalStageDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int OrderSequence { get; set; }
}

public class CreateApprovalStageDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int RoleId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int OrderSequence { get; set; }
}

public class UpdateApprovalStageDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int RoleId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int OrderSequence { get; set; }
}
