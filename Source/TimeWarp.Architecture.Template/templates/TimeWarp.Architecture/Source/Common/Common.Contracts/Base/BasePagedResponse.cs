namespace TimeWarp.Architecture.Features;

public abstract record BasePagedResponse : BaseResponse
{
  public PaginationInfo Pagination { get; init; } = new();

  public record PaginationInfo
  {
    [UsedImplicitly]
    public int CurrentPage { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
  }
}
