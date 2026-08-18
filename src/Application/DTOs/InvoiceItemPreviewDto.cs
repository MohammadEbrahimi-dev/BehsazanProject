namespace Behsazan.Application.DTOs;

public class InvoiceItemPreviewDto
{
    public int Id { get; set; }

    public decimal Length { get; set; }

    public int Count { get; set; }

    public int BottomRebar { get; set; }

    public int TopRebar { get; set; }

    public int? ReinforcementBar { get; set; }

    public int? ReinforcementPercent { get; set; }

    public int Zigzag { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal TotalPrice { get; set; }
}
