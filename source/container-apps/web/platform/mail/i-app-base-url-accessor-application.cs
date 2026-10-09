#region Purpose
// Absolute public origin (scheme, host, path base) for links in mail, or null when none can be resolved.
#endregion

#region Design
// Mail needs a clickable permalink. IRequestHostAccessor returns only the host, which is not a URL.
// The server implementation prefers Mail:PublicBaseUrl, then the forwarded public origin, then the
// request; it returns null when none resolves so a sender still emits the relative path.
#endregion

namespace TimeWarp.Architecture.Mail;

public interface IAppBaseUrlAccessor
{
  Uri? GetBaseUrl();
}
