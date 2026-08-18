using Behsazan.Domain.Enums;

namespace Behsazan.Application.DTOs;

public class ProjectFormDto
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public string CustomerFullName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public int? GeneralLedgerNumber { get; set; }

    public JoistType JoistType { get; set; } = JoistType.Concrete25;

    public bool IsNew => Id == 0;

    public ProjectFormDto Clone() => new()
    {
        Id = Id,
        CustomerId = CustomerId,
        CustomerFullName = CustomerFullName,
        Name = Name,
        Address = Address,
        GeneralLedgerNumber = GeneralLedgerNumber,
        JoistType = JoistType
    };
}
