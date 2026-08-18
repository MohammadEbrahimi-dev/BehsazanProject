namespace Behsazan.Application.DTOs;

public class DepositFormDto
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public string CustomerFullName { get; set; } = string.Empty;

    public DateTime DepositDate { get; set; } = DateTime.Today;

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public string? TrackingNumber { get; set; }

    public string? ReferenceNumber { get; set; }

    public string FromAccountNo { get; set; } = string.Empty;

    public string ToAccountNo { get; set; } = string.Empty;

    public bool IsNew => Id == 0;

    public DepositFormDto Clone() => new()
    {
        Id = Id,
        ProjectId = ProjectId,
        ProjectName = ProjectName,
        CustomerFullName = CustomerFullName,
        DepositDate = DepositDate,
        Amount = Amount,
        Description = Description,
        TrackingNumber = TrackingNumber,
        ReferenceNumber = ReferenceNumber,
        FromAccountNo = FromAccountNo,
        ToAccountNo = ToAccountNo
    };
}
