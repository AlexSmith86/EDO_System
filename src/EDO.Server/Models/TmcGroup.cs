namespace EDO.Server.Models;

/// <summary>Группа ГКТСМ (справочник №1)</summary>
public class TmcGroup
{
    public int Id { get; set; }

    /// <summary>Код группы (например "1.01")</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Наименование группы</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Подгруппы</summary>
    public List<TmcSubgroup> Subgroups { get; set; } = new();
}
