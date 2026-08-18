using Behsazan.Domain.Common;
using Behsazan.Domain.Enums;

namespace Behsazan.Domain.Entities;

public class CustomerPhoneNumber : BaseEntity
{
    public int CustomerId { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public PhoneType PhoneType { get; set; }

    public bool IsBaseNumber { get; set; }

    #region Navigation Properties
    public virtual Customer Customer { get; set; } = null!;
    #endregion
}
