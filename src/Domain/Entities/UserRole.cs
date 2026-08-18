using Behsazan.Domain.Common;

namespace Behsazan.Domain.Entities;

public class UserRole : BaseEntity
{
    public int UserId { get; set; }

    public int RoleId { get; set; }

    #region Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
    #endregion
}
