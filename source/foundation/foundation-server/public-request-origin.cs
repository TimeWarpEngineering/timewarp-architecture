#region Purpose
// One forwarded public-origin rule for OpenAPI, mail, and link previews.
#endregion

#region Design
// Behind the YARP ingress, Request.Scheme and Request.Host are the internal hop. The browser
// origin arrives on X-Forwarded-Proto and X-Forwarded-Host ("X-Forwarded": "Set" on the web
// routes). The first header value, then the first comma-separated entry, is the candidate.
// Proto is honored only for http and https. A forwarded host that contains "/", "\", "@",
// "://", whitespace, or a control character is refused, and the method returns null instead of
// falling back to the listen host — a slash or user-info would otherwise become part of the
// URL. When no forwarded host is present, the request host is used. When no http(s) proto is
// present, the request scheme is used. Path base is included. OpenAPI replaces its servers
// entry only when a forwarded value was present; mail and link previews always call Resolve,
// so a direct request still yields the listening origin.
#endregion

namespace TimeWarp.Foundation;

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Primitives;

/// <summary>
/// The public origin of one request: forwarded proto and host when they are usable, otherwise
/// the request scheme and host, plus path base.
/// </summary>
public static class PublicRequestOrigin
{
  /// <summary>
  /// Resolves the public origin. Null when the forwarded host is not a host, no host is
  /// available, or the values do not form an absolute http(s) URL.
  /// </summary>
  public static Uri? Resolve(HttpRequest request)
  {
    ArgumentNullException.ThrowIfNull(request);
    string? url = BuildUrl(request);
    return url is null ? null : new Uri(url, UriKind.Absolute);
  }

  /// <summary>
  /// The OpenAPI servers URL when a forwarded proto or host is present. Null leaves the
  /// framework listen address in place, including when the forwarded host is not a host.
  /// </summary>
  internal static string? ForwardedServerUrl(HttpRequest request)
  {
    ArgumentNullException.ThrowIfNull(request);
    return HasForwardedOrigin(request) ? BuildUrl(request) : null;
  }

  private static bool HasForwardedOrigin(HttpRequest request)
  {
    string? forwardedHost = FirstValue(request.Headers[ForwardedHeadersDefaults.XForwardedHostHeaderName]);
    string? forwardedProto = FirstValue(request.Headers[ForwardedHeadersDefaults.XForwardedProtoHeaderName]);
    return forwardedHost is not null || IsHttpScheme(forwardedProto);
  }

  private static string? BuildUrl(HttpRequest request)
  {
    string? forwardedHost = FirstValue(request.Headers[ForwardedHeadersDefaults.XForwardedHostHeaderName]);
    string? forwardedProto = FirstValue(request.Headers[ForwardedHeadersDefaults.XForwardedProtoHeaderName]);
    bool protoIsHttp = IsHttpScheme(forwardedProto);

    if (forwardedHost is not null && !IsHost(forwardedHost))
    {
      return null;
    }

    string scheme = protoIsHttp ? forwardedProto! : request.Scheme;
    if (!IsHttpScheme(scheme))
    {
      return null;
    }

    scheme = scheme.ToLowerInvariant();
    HostString host = forwardedHost is not null ? new HostString(forwardedHost) : request.Host;
    if (!host.HasValue)
    {
      return null;
    }

    string serverUrl = UriHelper.BuildAbsolute(scheme, host, request.PathBase);
    return Uri.TryCreate(serverUrl, UriKind.Absolute, out Uri? uri) && IsHttpScheme(uri.Scheme)
      ? serverUrl
      : null;
  }

  private static bool IsHttpScheme(string? scheme) =>
    scheme is not null
    && (string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
      || string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase));

  private static bool IsHost(string host)
  {
    if (host.Contains("://", StringComparison.Ordinal)
      || host.Contains('/', StringComparison.Ordinal)
      || host.Contains('\\', StringComparison.Ordinal)
      || host.Contains('@', StringComparison.Ordinal))
    {
      return false;
    }

    foreach (char character in host)
    {
      if (char.IsWhiteSpace(character) || char.IsControl(character))
      {
        return false;
      }
    }

    return true;
  }

  private static string? FirstValue(StringValues values)
  {
    string? first = values.FirstOrDefault()?.Split(',')[0].Trim();
    return string.IsNullOrEmpty(first) ? null : first;
  }
}
