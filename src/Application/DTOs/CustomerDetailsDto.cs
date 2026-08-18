namespace Behsazan.Application.DTOs;

public class CustomerDetailsDto
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string NationalCode { get; set; } = string.Empty;

    public int ProjectsCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public IReadOnlyList<CustomerPhoneNumberDto> PhoneNumbers { get; set; } =
        Array.Empty<CustomerPhoneNumberDto>();
}
