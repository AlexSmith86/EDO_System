namespace EDO.Server.Models;

/// <summary>ТМЦ — Товарно-материальные ценности</summary>
public class Tmc
{
    public int Id { get; set; }

    /// <summary>Наименование</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Артикул</summary>
    public string? Article { get; set; }

    /// <summary>Внешний идентификатор для синхронизации с 1С</summary>
    public string? ExternalId { get; set; }

    /// <summary>Текущий остаток (из 1С)</summary>
    public decimal StockBalance { get; set; }
}
