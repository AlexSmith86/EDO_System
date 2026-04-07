namespace EDO.Server.Models;

/// <summary>Строка табличной части заявки на ТМЦ</summary>
public class TmcRequestItem
{
    public int Id { get; set; }

    /// <summary>Заявка</summary>
    public int TmcRequestId { get; set; }

    public TmcRequest TmcRequest { get; set; } = null!;

    /// <summary>Группа ГКТСМ</summary>
    public int? GroupId { get; set; }

    public TmcGroup? Group { get; set; }

    /// <summary>Подгруппа ГКТСМ</summary>
    public int? SubgroupId { get; set; }

    public TmcSubgroup? Subgroup { get; set; }

    /// <summary>Наименование ТМЦ (детальное описание)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Количество</summary>
    public decimal Quantity { get; set; }

    /// <summary>Единица измерения</summary>
    public string? Unit { get; set; }

    /// <summary>Стоимость</summary>
    public decimal? Price { get; set; }

    /// <summary>Плановый срок поставки ТМЦ</summary>
    public DateTime? PlannedDeliveryDate { get; set; }

    /// <summary>Ссылка на счёт/КП</summary>
    public string? InvoiceLink { get; set; }

    /// <summary>Комментарий</summary>
    public string? Comment { get; set; }

    /// <summary>ФИО инициатора позиции</summary>
    public string? InitiatorName { get; set; }

    /// <summary>Должность инициатора позиции</summary>
    public string? InitiatorPosition { get; set; }
}
