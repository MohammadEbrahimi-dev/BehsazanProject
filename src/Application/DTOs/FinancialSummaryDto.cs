namespace Behsazan.Application.DTOs;

public class FinancialSummaryDto
{
    public decimal TotalRevenue { get; set; }

    public decimal TotalCollections { get; set; }

    public decimal NetCashFlow { get; set; }

    public decimal OutstandingReceivables { get; set; }

    public int InvoiceCount { get; set; }

    public int DepositCount { get; set; }
}
