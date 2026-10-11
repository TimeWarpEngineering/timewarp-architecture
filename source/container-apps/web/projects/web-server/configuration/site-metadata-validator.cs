#region Purpose
// Startup-time FluentValidation guard for SiteMetadata configuration binding.
#endregion

#region Design
// Wired through AddFluentValidatedOptions(...).ValidateOnStart() in Program.ConfigureSettings,
// so a bad BaseUrl or image path crashes the host at boot instead of emitting a crawler-hostile card.
// Internal visibility keeps the validator out of AddValidatorsFromAssemblyContaining (request validators).
#endregion

namespace TimeWarp.Architecture.Configuration;

/// <summary>Validator for <see cref="SiteMetadata"/>.</summary>
internal sealed class SiteMetadataValidator : AbstractValidator<SiteMetadata>
{
  public SiteMetadataValidator()
  {
    RuleFor(metadata => metadata.SiteName).NotEmpty();
    RuleFor(metadata => metadata.Title).NotEmpty();
    RuleFor(metadata => metadata.Description).NotEmpty();
    RuleFor(metadata => metadata.ImageAlt).NotEmpty();
    RuleFor(metadata => metadata.ImageType).NotEmpty();
    RuleFor(metadata => metadata.ImageWidth).GreaterThan(0);
    RuleFor(metadata => metadata.ImageHeight).GreaterThan(0);
    RuleFor(metadata => metadata.ImagePath)
      .Must(static path => IsImageReference(path))
      .WithMessage("ImagePath must be a root-relative path or an absolute http(s) URL.");
    RuleFor(metadata => metadata.BaseUrl)
      .Must(static url => url is null || IsPublicOrigin(url))
      .WithMessage("BaseUrl must be an absolute http or https URL without user info.");
  }

  private static bool IsImageReference(string? path)
  {
    if (string.IsNullOrWhiteSpace(path))
    {
      return false;
    }

    if (path.StartsWith('/', StringComparison.Ordinal) && !path.StartsWith("//", StringComparison.Ordinal))
    {
      return true;
    }

    return Uri.TryCreate(path, UriKind.Absolute, out Uri? image)
      && IsHttpScheme(image.Scheme)
      && string.IsNullOrEmpty(image.UserInfo);
  }

  private static bool IsPublicOrigin(Uri url) =>
    url.IsAbsoluteUri && IsHttpScheme(url.Scheme) && string.IsNullOrEmpty(url.UserInfo);

  private static bool IsHttpScheme(string scheme) =>
    string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
    || string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
}
