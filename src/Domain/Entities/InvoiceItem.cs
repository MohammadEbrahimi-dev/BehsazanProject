using Behsazan.Domain.Common;

namespace Behsazan.Domain.Entities;

public class InvoiceItem : BaseEntity
{
    public int InvoiceId { get; set; }

    public decimal Length { get; set; }

    public int Count { get; set; }

    public int BottomRebar { get; set; }

    public int TopRebar { get; set; }

    public int? ReinforcementBar { get; set; }

    public int? ReinforcementPercent { get; set; }

    public int Zigzag { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public decimal TotalAmount { get; set; }

    #region Navigation Properties
    public virtual Invoice Invoice { get; set; } = null!;
    #endregion
}
