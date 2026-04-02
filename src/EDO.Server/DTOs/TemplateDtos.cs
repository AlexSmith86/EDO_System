using System.ComponentModel.DataAnnotations;

namespace EDO.Server.DTOs;

public class TemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ProcessType { get; set; } = string.Empty;
}

public class CreateTemplateDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ProcessType { get; set; } = string.Empty;
}

public class UpdateTemplateDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ProcessType { get; set; } = string.Empty;
}
