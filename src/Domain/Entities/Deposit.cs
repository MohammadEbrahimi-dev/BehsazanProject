using Behsazan.Domain.Common;

namespace Behsazan.Domain.Entities;

public class Deposit : BaseEntity
{
    public int ProjectId { get; set; }

    public DateTime DepositDate { get; set; }

    public string FromAccountNo { get; set; } = string.Empty;

    public string ToAccountNo { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public string? TrackingNumber { get; set; }

    public string? ReferenceNumber { get; set; }

    #region Navigation Properties
    public virtual Project Project { get; set; } = null!;
    #endregion
}
