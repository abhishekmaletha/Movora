namespace Core.CQRS.Common;
public class PagedListResponse<T>
{
    public IReadOnlyList<T> Items { get; }

    public long TotalItemCount { get; }

    public bool HasNextPage { get; }

    public PagedListResponse(IEnumerable<T> items, long totalItemCount, bool hasNextPage)
    {
        this.Items = items.ToList();
        this.TotalItemCount = totalItemCount;
        this.HasNextPage = hasNextPage;
    }
}