using Behsazan.Domain.Common;

namespace Behsazan.Domain.Entities;

public class Customer : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? NationalCode { get; set; }

    #region Computed Properties
    public string FullName =>
        string.IsNullOrWhiteSpace(FirstName)
            ? LastName
            : $"{FirstName} {LastName}".Trim();
    #endregion

    #region Navigation Properties
    public virtual ICollection<CustomerPhoneNumber> PhoneNumbers { get; set; } = new List<CustomerPhoneNumber>();
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    public virtual User? User { get; set; }
    #endregion
}
