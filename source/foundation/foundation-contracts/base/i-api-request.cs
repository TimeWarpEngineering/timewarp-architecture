#region Purpose
// Makes a request contract self-describing (route + verb) so a generic API client can dispatch it without per-endpoint plumbing.
#endregion

namespace TimeWarp.Foundation.Features;

public interface IApiRequest : IBaseRequest
{
  string GetRoute();
  HttpVerb GetHttpVerb();
}
