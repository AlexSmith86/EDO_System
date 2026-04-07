using System.ComponentModel.DataAnnotations;

namespace EDO.Server.DTOs;

public class WorkflowChainDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<WorkflowStepDto> Steps { get; set; } = new();
}

public class WorkflowStepDto
{
    public int Id { get; set; }
    public int WorkflowChainId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string TargetPosition { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class CreateWorkflowChainDto
{
    [Required, MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    public List<CreateWorkflowStepDto> Steps { get; set; } = new();
}

public class CreateWorkflowStepDto
{
    [Required, MaxLength(300)]
    public string StepName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string TargetPosition { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Order { get; set; }
}

public class UpdateWorkflowChainDto
{
    [Required, MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<CreateWorkflowStepDto> Steps { get; set; } = new();
}
