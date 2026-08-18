namespace Behsazan.Application.DTOs;

public class DashboardStatsDto
{
    public int TotalCustomers { get; set; }
    public int TotalProjects { get; set; }
    public int TotalInvoices { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal OutstandingBalance { get; set; }
}
