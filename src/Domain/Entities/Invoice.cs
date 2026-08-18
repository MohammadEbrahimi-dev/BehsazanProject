using Behsazan.Domain.Common;

namespace Behsazan.Domain.Entities;

public class Invoice : BaseEntity
{
    public int ProjectId { get; set; }

    public int InvoiceNumber { get; set; }

    public DateTime InvoiceDate { get; set; }

    public string? Title { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal TotalPrice { get; set; }

    public decimal? ShippingCost { get; set; }

    #region Navigation Properties
    public virtual Project Project { get; set; } = null!;
    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = [];
    #endregion
}
