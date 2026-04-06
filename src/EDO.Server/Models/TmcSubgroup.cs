namespace EDO.Server.Models;

/// <summary>Подгруппа ГКТСМ</summary>
public class TmcSubgroup
{
    public int Id { get; set; }

    /// <summary>Код подгруппы (например "1.01.01")</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Наименование подгруппы</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Заголовок-разделитель (не выбираемый)</summary>
    public bool IsHeader { get; set; }

    /// <summary>Группа-родитель</summary>
    public int GroupId { get; set; }

    public TmcGroup Group { get; set; } = null!;
}
