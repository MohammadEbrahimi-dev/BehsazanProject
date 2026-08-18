namespace Behsazan.Application.DTOs;

public class CustomerLookupDto
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string NationalCode { get; set; } = string.Empty;

    public override string ToString() => FullName;
}
