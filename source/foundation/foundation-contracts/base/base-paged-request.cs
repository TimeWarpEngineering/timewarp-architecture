#region Purpose
// Base for list-style requests, supplying Page/PageSize so paging is uniform across endpoints.
#endregion

namespace TimeWarp.Foundation.Features;

/// <summary>
/// Base request that carries uniform <see cref="Page"/> / <see cref="PageSize"/> paging fields.
/// </summary>
public abstract class BasePagedRequest : BaseRequest
{
  /// <summary>
  /// 1-based page index to return.
  /// </summary>
  public int Page { get; set; } = 1;
  /// <summary>
  /// Maximum number of items per page.
  /// </summary>
  public int PageSize { get; set; } = 10;
}
