namespace Behsazan.Application.DTOs;

public class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    public int TotalCount { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalPages =>
        PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public static PagedResultDto<T> Empty(int pageNumber, int pageSize) => new()
    {
        Items = Array.Empty<T>(),
        TotalCount = 0,
        PageNumber = pageNumber,
        PageSize = pageSize
    };
}
