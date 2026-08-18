namespace Behsazan.Application.DTOs;

public class ProjectLedgerDto
{
    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public string ProjectAddress { get; set; } = string.Empty;

    public int? GeneralLedgerNumber { get; set; }

    public int CustomerId { get; set; }

    public string CustomerFullName { get; set; } = string.Empty;

    public int InvoiceCount { get; set; }

    public decimal InvoiceTotal { get; set; }

    public int DepositCount { get; set; }

    public decimal DepositTotal { get; set; }

    public decimal OutstandingBalance { get; set; }

    public IReadOnlyList<ProjectLedgerEntryDto> Entries { get; set; } = Array.Empty<ProjectLedgerEntryDto>();
}
