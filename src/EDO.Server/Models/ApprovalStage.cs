namespace EDO.Server.Models;

/// <summary>Этап согласования</summary>
public class ApprovalStage
{
    public int Id { get; set; }

    /// <summary>Название этапа</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Роль, которая должна согласовать на этом этапе</summary>
    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;

    /// <summary>Порядковый номер этапа в цепочке</summary>
    public int OrderSequence { get; set; }
}
