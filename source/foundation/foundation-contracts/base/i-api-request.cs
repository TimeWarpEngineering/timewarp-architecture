#region Purpose
// Makes a request contract self-describing (route + verb) so a generic API client can dispatch it without per-endpoint plumbing.
#endregion

namespace TimeWarp.Foundation.Features;

/// <summary>
/// Self-describing API request that carries its route and HTTP verb for generic client dispatch.
/// </summary>
public interface IApiRequest : IBaseRequest
{
  /// <summary>
  /// Absolute or app-relative route template for this request.
  /// </summary>
  string GetRoute();
  /// <summary>
  /// HTTP verb used when sending this request.
  /// </summary>
  HttpVerb GetHttpVerb();
}
