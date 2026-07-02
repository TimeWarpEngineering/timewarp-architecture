#region Purpose
// Base for list-style requests, supplying Page/PageSize so paging is uniform across endpoints.
#endregion

namespace TimeWarp.Foundation.Features;

public abstract class BasePagedRequest : BaseRequest
{
  public int Page { get; set; } = 1;
  public int PageSize { get; set; } = 10;
}
