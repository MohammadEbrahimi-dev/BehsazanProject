namespace Behsazan.Application.DTOs;

public class ProjectLookupDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public string CustomerFullName { get; set; } = string.Empty;

    public override string ToString() =>
        string.IsNullOrWhiteSpace(CustomerFullName) ? Name : $"{Name} — {CustomerFullName}";
}
