using Behsazan.Application.Enums;

namespace Behsazan.Application.DTOs;

public class ProjectLedgerEntryDto
{
    public ProjectLedgerEntryType EntryType { get; set; }

    public int RelatedId { get; set; }

    public DateTime Date { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }

    public decimal RunningBalance { get; set; }
}
