using Behsazan.Domain.Common;

namespace Behsazan.Domain.Entities;

public class RolePermission : BaseEntity
{
    public int RoleId { get; set; }

    public int PermissionId { get; set; }

    #region Navigation Properties
    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
    #endregion
}
