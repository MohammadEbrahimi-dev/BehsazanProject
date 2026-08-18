namespace Behsazan.Application.DTOs;

public enum InvoiceSortBy
{
    LastActivity = 0,
    InvoiceNumber = 1,
    InvoiceDate = 2,
    CustomerName = 3,
    ProjectName = 4,
    TotalPrice = 5,
    ItemsCount = 6,
    CreatedAt = 7
}

public class InvoiceQueryDto
{
    private const int MaxPageSize = 100;

    private int _pageNumber = 1;
    private int _pageSize = 10;

    public string? SearchTerm { get; set; }

    public int? CustomerId { get; set; }

    public int? ProjectId { get; set; }

    public DateTime? InvoiceDateFrom { get; set; }

    public DateTime? InvoiceDateTo { get; set; }

    public InvoiceSortBy SortBy { get; set; } = InvoiceSortBy.LastActivity;

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
