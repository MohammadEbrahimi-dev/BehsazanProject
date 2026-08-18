namespace Behsazan.Application.DTOs;

public enum CustomerSortBy
{
    LastActivity = 0,
    FullName = 1,
    NationalCode = 2,
    ProjectsCount = 3,
    CreatedAt = 4
}

public class CustomerQueryDto
{
    private const int MaxPageSize = 100;

    private int _pageNumber = 1;
    private int _pageSize = 10;

    public string? SearchTerm { get; set; }

    public CustomerSortBy SortBy { get; set; } = CustomerSortBy.LastActivity;

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
