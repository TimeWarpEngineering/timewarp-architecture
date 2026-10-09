#region Purpose
// Resolves the public origin (scheme, host, path base) used for absolute links in outbound mail.
#endregion

#region Design
// Order: (1) Mail:PublicBaseUrl when it is set to an absolute URL — the operator's explicit public
// origin, the same idea as Entra's PublicOrigin; it works without a request. (2) The forwarded
// public origin: the first X-Forwarded-Proto value (only "http"/"https" are honored) and the first
// X-Forwarded-Host value, port kept. Behind the YARP ingress Request.Scheme/Request.Host are the
// internal web-server hop (plain http, destination Host), so they would mail an unusable link; the
// Web.Server routes Set both headers (client values dropped), the same first-value rule as
// HttpRequestHostAccessor.GetPublicHost, which strips the port and so cannot be reused for a URL.
// (3) Request.Scheme and Request.Host when no forwarded header is present (web-server reached
// directly). Path base is appended in (2) and (3). Returns null when there is no request, no host,
// or the values do not form an absolute URL, so mail falls back to the relative permalink.
// Trust: where web-server is directly reachable a client can forge these headers, exactly as it can
// forge Host. The only recipient of the link is the filer's own profile address, so a forged origin
// only misleads the person who forged it. Set Mail:PublicBaseUrl to remove the header dependency.
#endregion

namespace TimeWarp.Architecture.Mail;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Primitives;

public sealed class HttpAppBaseUrlAccessor : IAppBaseUrlAccessor
{
  private readonly IHttpContextAccessor HttpContextAccessor;
  private readonly MailOptions Options;

  public HttpAppBaseUrlAccessor(IHttpContextAccessor httpContextAccessor, IOptions<MailOptions> options)
  {
    ArgumentNullException.ThrowIfNull(options);
    HttpContextAccessor = httpContextAccessor;
    Options = options.Value;
  }

  public Uri? GetBaseUrl()
  {
    if (Options.PublicBaseUrl is { IsAbsoluteUri: true } configured)
    {
      return configured;
    }

    if (HttpContextAccessor.HttpContext?.Request is not { } request)
    {
      return null;
    }

    string? forwardedProto = FirstValue(request.Headers[ForwardedHeadersDefaults.XForwardedProtoHeaderName]);
    string scheme = forwardedProto is not null
      && (string.Equals(forwardedProto, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
        || string.Equals(forwardedProto, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
      ? forwardedProto.ToLowerInvariant()
      : request.Scheme;

    string? host = FirstValue(request.Headers[ForwardedHeadersDefaults.XForwardedHostHeaderName])
      ?? (request.Host.HasValue ? request.Host.Value : null);
    if (string.IsNullOrEmpty(host))
    {
      return null;
    }

    string pathBase = request.PathBase.Value ?? string.Empty;
    return Uri.TryCreate($"{scheme}://{host}{pathBase}", UriKind.Absolute, out Uri? baseUrl) ? baseUrl : null;
  }

  private static string? FirstValue(StringValues values)
  {
    string? first = values.FirstOrDefault()?.Split(',')[0].Trim();
    return string.IsNullOrEmpty(first) ? null : first;
  }
}
