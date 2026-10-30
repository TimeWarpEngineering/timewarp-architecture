#region Purpose
// Resolves absolute Open Graph and canonical URLs for one inbound request.
#endregion

#region Design
// Origin order matches outbound mail: SiteMetadata.BaseUrl when it is an absolute http(s) URL,
// else HttpAppBaseUrlAccessor.GetRequestOrigin, which delegates to PublicRequestOrigin: first
// X-Forwarded-Proto (http/https only), first X-Forwarded-Host (port kept; a host that is not a
// host is refused), else Request.Scheme + Request.Host, plus PathBase. A configured BaseUrl
// already includes any public path prefix. YARP
// sets those headers with action Set. The ingress first applies X-Forwarded-Proto and
// X-Forwarded-Host from a trusted proxy only (task 305), so the Set value is the client-facing
// origin. Where web-server is reached directly, a client can forge them, the same posture as
// HttpAppBaseUrlAccessor.
// No resolvable origin (malformed forwarded host, no Host) returns null and the tags are omitted:
// link-preview metadata is optional and must never fail the page render.
// The page URL is that origin plus Request.Path. The query is omitted. The image is site-wide:
// an absolute ImagePath is used as-is; a root-relative path is resolved against the origin,
// not against the page. A leading slash on the relative part would replace the origin path,
// so it is stripped before combining.
#endregion

namespace TimeWarp.Architecture.Configuration;

using TimeWarp.Architecture.Mail;

internal static class LinkPreview
{
  internal static LinkPreviewValues? Resolve(SiteMetadata metadata, HttpRequest? request)
  {
    ArgumentNullException.ThrowIfNull(metadata);

    Uri? origin = ResolveOrigin(metadata, request);
    if (origin is null)
    {
      return null;
    }

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

  private static Uri? ResolveOrigin(SiteMetadata metadata, HttpRequest? request)
  {
    if (metadata.BaseUrl is { IsAbsoluteUri: true } configured && IsHttpScheme(configured.Scheme))
    {
      return NormalizeOrigin(configured);
    }

    Uri? fromRequest = request is null ? null : HttpAppBaseUrlAccessor.GetRequestOrigin(request);
    return fromRequest is not null && IsHttpScheme(fromRequest.Scheme) ? NormalizeOrigin(fromRequest) : null;
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
}
