namespace Behsazan.Application.DTOs;

public class RecentProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int InvoicesCount { get; set; }
}
