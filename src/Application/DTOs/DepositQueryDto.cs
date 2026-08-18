namespace Behsazan.Application.DTOs;

public enum DepositSortBy
{
    DepositDate = 0,
    Amount = 1,
    ProjectName = 2,
    CustomerName = 3,
    TrackingNumber = 4,
    CreatedAt = 5
}

public class DepositQueryDto
{
    private const int MaxPageSize = 100;

    private int _pageNumber = 1;
    private int _pageSize = 10;

    public string? SearchTerm { get; set; }

    public int? ProjectId { get; set; }

    public DateTime? DepositDateFrom { get; set; }

    public DateTime? DepositDateTo { get; set; }

    public DepositSortBy SortBy { get; set; } = DepositSortBy.DepositDate;

    public bool SortDescending { get; set; } = true;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 10,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }
}
