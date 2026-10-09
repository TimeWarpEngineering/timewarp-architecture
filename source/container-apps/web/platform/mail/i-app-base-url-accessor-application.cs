#region Purpose
// Absolute origin (scheme, host, path base) of the current HTTP request, or null when there is none.
#endregion

#region Design
// Mail needs a clickable permalink. IRequestHostAccessor returns only the host, which is not a URL.
// The accessor returns null outside a request so a sender still emits the relative path.
#endregion

namespace TimeWarp.Architecture.Mail;

public interface IAppBaseUrlAccessor
{
  Uri? GetBaseUrl();
}
