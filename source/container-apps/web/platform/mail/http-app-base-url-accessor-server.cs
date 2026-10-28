#region Purpose
// Resolves the public origin (scheme, host, path base) used for absolute links in outbound mail.
#endregion

#region Design
// Order: (1) Mail:PublicBaseUrl when it is set to an absolute URL — the operator's explicit public
// origin, the same idea as Entra's PublicOrigin; it works without a request. (2) The forwarded
// public origin from PublicRequestOrigin: the first X-Forwarded-Proto value (only "http"/"https"
// are honored) and the first X-Forwarded-Host value, port kept. A forwarded host that is not a host
// is refused. Behind the YARP ingress Request.Scheme and Request.Host are the internal web-server
// hop (plain http, destination Host), so they would mail an unusable link; the
// Web.Server routes Set both headers from the ingress request. The ingress honors
// X-Forwarded-Proto and X-Forwarded-Host only from a trusted proxy before that Set (task 305),
// so a client-facing https origin survives the plain-http hop. The same first-value rule as
// HttpRequestHostAccessor.GetPublicHost, which strips the port and so cannot be reused for a URL.
// (3) Request.Scheme and Request.Host when no forwarded header is present (web-server reached
// directly). Path base is appended in (2) and (3). Returns null when there is no request, no host,
// a malformed forwarded host, or the values do not form an absolute URL, so mail falls back to the
// relative permalink. GetRequestOrigin delegates to PublicRequestOrigin so LinkPreview and OpenAPI
// share that policy.
// Trust: where web-server is directly reachable a client can forge these headers, exactly as it can
// forge Host. The only recipient of the link is the filer's own profile address, so a forged origin
// only misleads the person who forged it. Set Mail:PublicBaseUrl to remove the header dependency.
#endregion

namespace TimeWarp.Architecture.Mail;

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

    return HttpContextAccessor.HttpContext?.Request is { } request ? GetRequestOrigin(request) : null;
  }

  /// <summary>
  /// The public origin of one request: forwarded proto and host when present, else the request's
  /// scheme and host, plus path base. Null when the forwarded host is not a host, no host is
  /// available, or the values do not form an absolute URL.
  /// Shared with link-preview tags so mail links and canonical URLs agree.
  /// </summary>
  public static Uri? GetRequestOrigin(HttpRequest request)
  {
    ArgumentNullException.ThrowIfNull(request);
    return PublicRequestOrigin.Resolve(request);
  }
}
