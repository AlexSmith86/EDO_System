namespace EDO.Server.Models;

/// <summary>Контрагент</summary>
public class Contractor
{
    public int Id { get; set; }

    /// <summary>Наименование</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>ИНН</summary>
    public string? Inn { get; set; }

    /// <summary>Тип контрагента</summary>
    public ContractorType ContractorType { get; set; }

    /// <summary>Внешний идентификатор для синхронизации с 1С</summary>
    public string? ExternalId { get; set; }
}

public enum ContractorType
{
    Supplier,  // Поставщик
    Client     // Клиент
}
