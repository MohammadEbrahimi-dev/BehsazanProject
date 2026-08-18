namespace Behsazan.Application.DTOs;

public class FinancialReportQueryDto
{
    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string? Period { get; set; }
}
