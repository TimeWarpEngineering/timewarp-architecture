#region Purpose
// Resolves absolute Open Graph and canonical URLs for one inbound request.
#endregion

#region Design
// Origin order matches outbound mail: SiteMetadata.BaseUrl when it is an absolute http(s) URL,
// else the first X-Forwarded-Proto value (only http/https) and the first X-Forwarded-Host value
// (port kept), else Request.Scheme + Request.Host. PathBase joins the request fallback only —
// a configured BaseUrl already includes any public path prefix. YARP sets those headers with
// action Set (client values dropped). Where web-server is reached directly, a client can forge
// them, the same posture as HttpAppBaseUrlAccessor.
// The page URL is that origin plus Request.Path. The query is omitted. The image is site-wide:
// an absolute ImagePath is used as-is; a root-relative path is resolved against the origin,
// not against the page. A leading slash on the relative part would replace the origin path,
// so it is stripped before combining.
#endregion

namespace TimeWarp.Architecture.Configuration;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Primitives;

internal static class LinkPreview
{
  internal static LinkPreviewValues Resolve(SiteMetadata metadata, HttpRequest? request)
  {
    ArgumentNullException.ThrowIfNull(metadata);

    Uri origin = ResolveOrigin(metadata, request);
    Uri pageUrl = Combine(origin, request?.Path.Value);
    Uri? absoluteImage = AbsoluteHttpOrNull(metadata.ImagePath);
    Uri imageUrl = absoluteImage ?? Combine(origin, metadata.ImagePath);

    return new LinkPreviewValues
    {
      Description = metadata.Description,
      SiteName = metadata.SiteName,
      Title = metadata.Title,
      PageUrl = pageUrl,
      ImageUrl = imageUrl,
      ImageAlt = metadata.ImageAlt,
      ImageType = metadata.ImageType,
      ImageWidth = metadata.ImageWidth,
      ImageHeight = metadata.ImageHeight,
    };
  }

  private static Uri ResolveOrigin(SiteMetadata metadata, HttpRequest? request)
  {
    if (metadata.BaseUrl is { IsAbsoluteUri: true } configured && IsHttpScheme(configured.Scheme))
    {
      return NormalizeOrigin(configured);
    }

    if (request is null)
    {
      throw new InvalidOperationException(
        "SiteMetadata.BaseUrl is unset and there is no request to derive the public origin from.");
    }

    string? forwardedProto = FirstValue(request.Headers[ForwardedHeadersDefaults.XForwardedProtoHeaderName]);
    string scheme = forwardedProto switch
    {
      not null when string.Equals(forwardedProto, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
        => Uri.UriSchemeHttps,
      not null when string.Equals(forwardedProto, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
        => Uri.UriSchemeHttp,
      _ => request.Scheme,
    };

    string? host = FirstValue(request.Headers[ForwardedHeadersDefaults.XForwardedHostHeaderName])
      ?? (request.Host.HasValue ? request.Host.Value : null);
    if (string.IsNullOrEmpty(host))
    {
      throw new InvalidOperationException("The request has no host, so link-preview URLs cannot be absolute.");
    }

    string pathBase = request.PathBase.Value ?? string.Empty;
    if (!Uri.TryCreate($"{scheme}://{host}{pathBase}", UriKind.Absolute, out Uri? created))
    {
      throw new InvalidOperationException("The request host did not form an absolute link-preview origin.");
    }

    return NormalizeOrigin(created);
  }

  private static Uri NormalizeOrigin(Uri absolute)
  {
    string prefix = absolute.AbsolutePath.Trim('/');
    string text = string.IsNullOrEmpty(prefix)
      ? $"{absolute.Scheme}://{absolute.Authority}/"
      : $"{absolute.Scheme}://{absolute.Authority}/{prefix}/";
    return new Uri(text, UriKind.Absolute);
  }

  private static Uri Combine(Uri origin, string? path)
  {
    // A leading slash replaces the origin's path. "/" is the origin itself.
    if (string.IsNullOrEmpty(path) || path == "/")
    {
      return origin;
    }

    return new Uri(origin, path.TrimStart('/'));
  }

  private static Uri? AbsoluteHttpOrNull(string value)
  {
    if (Uri.TryCreate(value, UriKind.Absolute, out Uri? created) && IsHttpScheme(created.Scheme))
    {
      return created;
    }

    return null;
  }

  private static bool IsHttpScheme(string scheme) =>
    string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
    || string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);

  private static string? FirstValue(StringValues values)
  {
    string? first = values.FirstOrDefault()?.Split(',')[0].Trim();
    return string.IsNullOrEmpty(first) ? null : first;
  }
}
