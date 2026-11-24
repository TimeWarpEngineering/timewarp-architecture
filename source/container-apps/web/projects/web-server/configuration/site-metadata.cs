#region Purpose
// Operator-facing copy and image for the server-rendered Open Graph and Twitter card tags.
#endregion

#region Design
// Section name "SiteMetadata" matches the type name: AddFluentValidatedOptions binds by type name
// unless [ConfigurationKey] says otherwise. SiteName, Title, and ImageAlt are empty by default and
// read as BrandOptions.ProductName and BrandOptions.EffectiveLogoAlt (LinkPreview), and the image itself is
// BrandOptions.OgImageSrc (task 513: one brand source). The description default is the template's
// own sentence, not a deployed host. BaseUrl is optional; when it is unset the tags use the
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
  public const string DefaultDescription =
    "A distributed .NET application on a batteries-included architecture: Blazor, passkey and agent-key identity, x402 payments, and agent discovery documents.";
  public const int DefaultImageWidth = 1200;
  public const int DefaultImageHeight = 630;
  public const string DefaultImageType = "image/png";
  public const string OpenGraphType = "website";
  public const string TwitterCard = "summary_large_image";

  /// <summary>
  /// Absolute public origin (optional path prefix, no query). Unset means derive it from the request.
  /// </summary>
  public Uri? BaseUrl { get; set; }

  /// <summary><c>og:site_name</c>. Empty reads as BrandOptions.ProductName.</summary>
  public string SiteName { get; set; } = "";

  /// <summary><c>og:title</c> and <c>twitter:title</c>. Empty reads as BrandOptions.ProductName.</summary>
  public string Title { get; set; } = "";

  public string Description { get; set; } = DefaultDescription;

  /// <summary><c>og:image:alt</c> for BrandOptions.OgImageSrc. Empty reads as BrandOptions.EffectiveLogoAlt.</summary>
  public string ImageAlt { get; set; } = "";
  public int ImageWidth { get; set; } = DefaultImageWidth;
  public int ImageHeight { get; set; } = DefaultImageHeight;
  public string ImageType { get; set; } = DefaultImageType;
}
