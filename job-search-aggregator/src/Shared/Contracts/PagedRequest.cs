namespace JobSearchAggregator.Shared.Contracts;

/// <summary>
/// Common request shape for paged, sorted list endpoints (Job Listing, Search, etc.).
/// </summary>
public class PagedRequest
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;

    public int PageNumber { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
}
