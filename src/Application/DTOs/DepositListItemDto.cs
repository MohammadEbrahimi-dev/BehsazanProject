namespace Behsazan.Application.DTOs;

public class DepositListItemDto
{
    public int Id { get; set; }

    public DateTime DepositDate { get; set; }

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public string? TrackingNumber { get; set; }

    public string? ReferenceNumber { get; set; }

    public string FromAccountNo { get; set; } = string.Empty;

    public string ToAccountNo { get; set; } = string.Empty;

    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public string CustomerFullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }
}
