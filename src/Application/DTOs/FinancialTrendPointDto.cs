namespace Behsazan.Application.DTOs;

public class FinancialTrendPointDto
{
    public int Year { get; set; }

    public int Month { get; set; }

    public int Day { get; set; }

    public DateTime PeriodStart { get; set; }

    public string Label { get; set; } = string.Empty;

    public decimal Revenue { get; set; }

    public decimal Collections { get; set; }
}
