using Behsazan.Domain.Common;
using Behsazan.Domain.Enums;

namespace Behsazan.Domain.Entities;

public class Project : BaseEntity
{
    public int CustomerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public int? GeneralLedgerNumber { get; set; }

    public JoistType JoistType { get; set; }

    #region Navigation Properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual ICollection<Invoice> Invoices { get; set; } = [];
    public virtual ICollection<Deposit> Deposits { get; set; } = [];
    #endregion
}
