namespace TimeWarp.Architecture.Features;

public abstract record BasePagedRequest : BaseRequest
{
  public int Page { get; set; } = 1;
  public int PageSize { get; set; } = 10;
}
