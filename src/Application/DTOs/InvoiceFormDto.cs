namespace Behsazan.Application.DTOs;

public class InvoiceFormDto
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public string CustomerFullName { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public int InvoiceNumber { get; set; }

    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    public string? Title { get; set; }

    public decimal? ShippingCost { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal TotalPrice { get; set; }

    public List<InvoiceItemFormDto> Items { get; set; } = [];

    public bool IsNew => Id == 0;

    public void RecalculateTotals()
    {
        TotalAmount = Math.Round(Items.Sum(i => i.TotalAmount), 5, MidpointRounding.AwayFromZero);

        var itemsPrice = Items.Sum(i => i.TotalPrice);
        var shipping = ShippingCost ?? 0m;
        TotalPrice = Math.Round(itemsPrice + shipping, 2, MidpointRounding.AwayFromZero);
    }

    public void RecalculateRowAndTotals(int index)
    {
        if (index < 0 || index >= Items.Count)
            return;

        Items[index].Recalculate();
        RecalculateTotals();
    }

    public void RecalculateAll()
    {
        foreach (var item in Items)
            item.Recalculate();

        RecalculateTotals();
    }

    public InvoiceFormDto Clone() => new()
    {
        Id = Id,
        ProjectId = ProjectId,
        ProjectName = ProjectName,
        CustomerFullName = CustomerFullName,
        CustomerId = CustomerId,
        InvoiceNumber = InvoiceNumber,
        InvoiceDate = InvoiceDate,
        Title = Title,
        ShippingCost = ShippingCost,
        TotalAmount = TotalAmount,
        TotalPrice = TotalPrice,
        Items = Items.Select(i => i.Clone()).ToList()
    };
}
