using Behsazan.Domain.Common;

namespace Behsazan.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int? CustomerId { get; set; }

    #region Navigation Properties
    public virtual Customer? Customer { get; set; }
    public virtual ICollection<UserRole> UserRoles { get; set; } = [];
    #endregion
}
