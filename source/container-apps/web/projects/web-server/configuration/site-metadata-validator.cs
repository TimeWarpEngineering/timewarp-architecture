#region Purpose
// Startup-time FluentValidation guard for SiteMetadata configuration binding.
#endregion

#region Design
// Wired through AddFluentValidatedOptions(...).ValidateOnStart() in WebServerSettings.ConfigureServices,
// so a bad BaseUrl crashes the host at boot instead of emitting a crawler-hostile card. The image
// path is BrandOptions.OgImageSrc and BrandOptionsValidator checks it. SiteName, Title, and
// ImageAlt may be empty: they fall back to BrandOptions, whose validator requires the name.
// Internal visibility keeps the validator out of AddValidatorsFromAssemblyContaining (request validators).
#endregion

namespace TimeWarp.Architecture.Configuration;

/// <summary>Validator for <see cref="SiteMetadata"/>.</summary>
internal sealed class SiteMetadataValidator : AbstractValidator<SiteMetadata>
{
  public SiteMetadataValidator()
  {
    RuleFor(metadata => metadata.Description).NotEmpty();
    RuleFor(metadata => metadata.ImageType).NotEmpty();
    RuleFor(metadata => metadata.ImageWidth).GreaterThan(0);
    RuleFor(metadata => metadata.ImageHeight).GreaterThan(0);
    RuleFor(metadata => metadata.BaseUrl)
      .Must(static url => url is null || IsPublicOrigin(url))
      .WithMessage("BaseUrl must be an absolute http or https URL without user info.");
  }

  private static bool IsPublicOrigin(Uri url) =>
    url.IsAbsoluteUri && IsHttpScheme(url.Scheme) && string.IsNullOrEmpty(url.UserInfo);

  private static bool IsHttpScheme(string scheme) =>
    string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
    || string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
}
