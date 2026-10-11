#region Purpose
// Operator-facing copy and image for the server-rendered Open Graph and Twitter card tags.
#endregion

#region Design
// Section name "SiteMetadata" matches the type name: AddFluentValidatedOptions binds by type name
// unless [ConfigurationKey] says otherwise. Defaults are the template's own name and the home-page
// welcome sentence, not a deployed host. BaseUrl is optional; when it is unset the tags use the
// inbound request (see LinkPreview). Leave BaseUrl unset behind YARP so X-Forwarded-Proto and
// X-Forwarded-Host supply the public origin. Do not hard-code arch.timewarp.work — generated apps
// are not that site.
// These values are site-wide. A per-route title has to be another static head contribution:
// HeadContent inside an interactive page is missing from the first HTML byte when Prerender is false,
// and link-preview crawlers never run Blazor.
#endregion

namespace TimeWarp.Architecture.Configuration;

/// <summary>
/// Link-preview copy bound from configuration. Absolute page and image URLs are resolved per request.
/// </summary>
public sealed class SiteMetadata
{
  public const string DefaultSiteName = "TimeWarp.Architecture";
  public const string DefaultTitle = "TimeWarp.Architecture";
  public const string DefaultDescription =
    "A distributed .NET reference application and project template by TimeWarp Enterprises.";
  public const string DefaultImagePath = "/images/og-image.png";
  public const string DefaultImageAlt = "TimeWarp Enterprises";
  public const int DefaultImageWidth = 1200;
  public const int DefaultImageHeight = 630;
  public const string DefaultImageType = "image/png";
  public const string OpenGraphType = "website";
  public const string TwitterCard = "summary_large_image";

  /// <summary>
  /// Absolute public origin (optional path prefix, no query). Unset means derive it from the request.
  /// </summary>
  public Uri? BaseUrl { get; set; }

  public string SiteName { get; set; } = DefaultSiteName;
  public string Title { get; set; } = DefaultTitle;
  public string Description { get; set; } = DefaultDescription;

  /// <summary>Root-relative static path, or an absolute http(s) image URL.</summary>
  public string ImagePath { get; set; } = DefaultImagePath;

  public string ImageAlt { get; set; } = DefaultImageAlt;
  public int ImageWidth { get; set; } = DefaultImageWidth;
  public int ImageHeight { get; set; } = DefaultImageHeight;
  public string ImageType { get; set; } = DefaultImageType;
}
