using Behsazan.Domain.Common;

namespace Behsazan.Domain.Entities;

public class Permission : BaseEntity
{
    public string Key { get; set; } = string.Empty;

    public string NameFa { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    #region Navigation Properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = [];
    #endregion
}
