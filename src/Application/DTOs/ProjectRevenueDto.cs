namespace Behsazan.Application.DTOs;

public class ProjectRevenueDto
{
    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public decimal Revenue { get; set; }

    public decimal Collections { get; set; }

    public decimal Outstanding { get; set; }
}
