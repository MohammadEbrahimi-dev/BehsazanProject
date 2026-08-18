using Behsazan.Domain.Enums;

namespace Behsazan.Application.DTOs;

public class ProjectListItemDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public int? GeneralLedgerNumber { get; set; }

    public JoistType JoistType { get; set; }

    public int CustomerId { get; set; }

    public string CustomerFullName { get; set; } = string.Empty;

    public int InvoicesCount { get; set; }

    public int DepositsCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public DateTime LastActivityAt { get; set; }
}
