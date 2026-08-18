namespace Behsazan.Application.DTOs;

public class InvoiceItemFormDto
{
    public int Id { get; set; }

    public Guid ClientKey { get; set; } = Guid.NewGuid();

    public decimal Length { get; set; }

    public int Count { get; set; } = 1;

    public int BottomRebar { get; set; }

    public int TopRebar { get; set; }

    public int? ReinforcementBar { get; set; }

    public int? ReinforcementPercent { get; set; }

    public int Zigzag { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal TotalPrice { get; set; }

    public bool IsNew => Id == 0;

    public void Recalculate()
    {
        TotalAmount = Math.Round(Length * Count, 5, MidpointRounding.AwayFromZero);
        TotalPrice = Math.Round(TotalAmount * UnitPrice, 2, MidpointRounding.AwayFromZero);
    }

    public InvoiceItemFormDto Clone() => new()
    {
        Id = Id,
        ClientKey = ClientKey,
        Length = Length,
        Count = Count,
        BottomRebar = BottomRebar,
        TopRebar = TopRebar,
        ReinforcementBar = ReinforcementBar,
        ReinforcementPercent = ReinforcementPercent,
        Zigzag = Zigzag,
        UnitPrice = UnitPrice,
        TotalAmount = TotalAmount,
        TotalPrice = TotalPrice
    };

    public InvoiceItemFormDto Duplicate()
    {
        var copy = Clone();
        copy.Id = 0;
        copy.ClientKey = Guid.NewGuid();
        copy.Recalculate();
        return copy;
    }
}
