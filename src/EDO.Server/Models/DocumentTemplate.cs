namespace EDO.Server.Models;

/// <summary>Шаблон документа</summary>
public class DocumentTemplate
{
    public int Id { get; set; }

    /// <summary>Название шаблона</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Путь к файлу-шаблону</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Тип процесса/документа, к которому привязан шаблон</summary>
    public string ProcessType { get; set; } = string.Empty;
}
