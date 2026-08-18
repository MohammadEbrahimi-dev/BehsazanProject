using Behsazan.Domain.Enums;

namespace Behsazan.Application.DTOs;

public enum ProjectSortBy
{
    LastActivity = 0,
    Name = 1,
    CustomerName = 2,
    JoistType = 3,
    CreatedAt = 4,
    InvoicesCount = 5
}

public class ProjectQueryDto
{
    private const int MaxPageSize = 100;

    private int _pageNumber = 1;
    private int _pageSize = 10;

    public string? SearchTerm { get; set; }

    public int? CustomerId { get; set; }

    public JoistType? JoistType { get; set; }

    public DateTime? CreatedFrom { get; set; }

    public DateTime? CreatedTo { get; set; }

    public ProjectSortBy SortBy { get; set; } = ProjectSortBy.LastActivity;

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
