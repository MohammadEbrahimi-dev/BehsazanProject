namespace Behsazan.Application.DTOs;

public class CustomerFormDto
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string NationalCode { get; set; } = string.Empty;

    public List<CustomerPhoneNumberDto> PhoneNumbers { get; set; } = new();

    public bool IsNew => Id == 0;

    public CustomerFormDto Clone() => new()
    {
        Id = Id,
        FirstName = FirstName,
        LastName = LastName,
        NationalCode = NationalCode,
        PhoneNumbers = PhoneNumbers.Select(p => p.Clone()).ToList()
    };
}
