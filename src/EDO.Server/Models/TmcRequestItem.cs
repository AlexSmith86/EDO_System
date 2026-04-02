namespace EDO.Server.Models;

/// <summary>Строка табличной части заявки на ТМЦ</summary>
public class TmcRequestItem
{
    public int Id { get; set; }

    /// <summary>Заявка</summary>
    public int TmcRequestId { get; set; }

    public TmcRequest TmcRequest { get; set; } = null!;

    /// <summary>ТМЦ</summary>
    public int TmcId { get; set; }

    public Tmc Tmc { get; set; } = null!;

    /// <summary>Количество</summary>
    public decimal Quantity { get; set; }
}
