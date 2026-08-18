using Behsazan.Domain.Enums;

namespace Behsazan.Application.DTOs;

public class CustomerPhoneNumberDto
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public PhoneType PhoneType { get; set; } = PhoneType.Mobile;

    public bool IsBaseNumber { get; set; }

    public bool IsNew => Id == 0;

    public CustomerPhoneNumberDto Clone() => new()
    {
        Id = Id,
        CustomerId = CustomerId,
        PhoneNumber = PhoneNumber,
        PhoneType = PhoneType,
        IsBaseNumber = IsBaseNumber
    };
}
