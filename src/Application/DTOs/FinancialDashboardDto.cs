namespace Behsazan.Application.DTOs;

public class FinancialDashboardDto
{
    public FinancialSummaryDto Summary { get; set; } = new();

    public IReadOnlyList<FinancialTrendPointDto> Trends { get; set; } = Array.Empty<FinancialTrendPointDto>();

    public IReadOnlyList<ProjectRevenueDto> RevenueByProject { get; set; } = Array.Empty<ProjectRevenueDto>();

    public ReceivablesStatusDto ReceivablesStatus { get; set; } = new();

    public IReadOnlyList<MonthlyAmountDto> MonthlyInvoices { get; set; } = Array.Empty<MonthlyAmountDto>();

    public IReadOnlyList<MonthlyAmountDto> MonthlyCollections { get; set; } = Array.Empty<MonthlyAmountDto>();
}
