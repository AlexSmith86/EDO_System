namespace EDO.Server.Models;

/// <summary>Кастомная цепочка согласования</summary>
public class WorkflowChain
{
    public int Id { get; set; }

    /// <summary>Название цепочки (напр. "Заявка на IT", "Хозрасходы")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Активна ли цепочка</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Шаги цепочки</summary>
    public List<WorkflowStep> Steps { get; set; } = new();
}
