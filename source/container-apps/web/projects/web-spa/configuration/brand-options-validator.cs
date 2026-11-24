#region Purpose
// Startup-time FluentValidation guard for BrandOptions.
#endregion

#region Design
// Wired through AddFluentValidatedOptions(...).ValidateOnStart() in the SPA ConfigureSettings,
// which the server host calls too, so a bad seed, logo, icon, or role color crashes the host at
// boot. Internal visibility keeps the validator out of AddValidatorsFromAssemblyContaining.
// Hex and asset rules are the same predicates /brand.css, /manifest.json, and the theme script
// trust. LogoAlt, ThemeColor, and BackgroundColor are validated as Effective* values, after their
// fallbacks, so an empty configured value is valid and a set one must be well formed.
#endregion

namespace TimeWarp.Architecture.Configuration;

using System.Linq.Expressions;

/// <summary>Validator for <see cref="BrandOptions"/>.</summary>
internal sealed class BrandOptionsValidator : AbstractValidator<BrandOptions>
{
  public BrandOptionsValidator()
  {
    RuleFor(brand => brand.ProductName).Must(BrandOptions.IsProductName);
    Asset(brand => brand.LogoSrc);
    Asset(brand => brand.FaviconSrc);
    Asset(brand => brand.AppleTouchIconSrc);
    Asset(brand => brand.Icon192Src);
    Asset(brand => brand.Icon512Src);
    Asset(brand => brand.OgImageSrc);
    RuleFor(brand => brand.EffectiveLogoAlt).Must(BrandOptions.IsProductName).MaximumLength(120);
    RuleFor(brand => brand.LogoWidth).InclusiveBetween(1, 512);
    RuleFor(brand => brand.LogoHeight).InclusiveBetween(1, 512);
    RuleFor(brand => brand.SeedColor).Must(BrandOptions.IsHexColor);
    Color(brand => brand.EffectiveThemeColor);
    Color(brand => brand.EffectiveBackgroundColor);
    RuleFor(brand => brand.Tokens).NotNull();

    When
    (
      brand => brand.Tokens is not null,
      () =>
      {
        Color(brand => brand.Tokens.Primary);
        Color(brand => brand.Tokens.PrimaryHover);
        Color(brand => brand.Tokens.PrimaryActive);
        Color(brand => brand.Tokens.PrimarySoft);
        Color(brand => brand.Tokens.OnPrimary);
        Color(brand => brand.Tokens.Secondary);
        Color(brand => brand.Tokens.SecondaryHover);
        Color(brand => brand.Tokens.SecondaryActive);
        Color(brand => brand.Tokens.SecondaryBright);
        Color(brand => brand.Tokens.Highlight);
        Color(brand => brand.Tokens.HighlightSoft);
        Color(brand => brand.Tokens.Info);
        Color(brand => brand.Tokens.PrimaryDark);
        Color(brand => brand.Tokens.SecondaryDark);
        Color(brand => brand.Tokens.HighlightDark);
      }
    );
  }

  private void Asset(Expression<Func<BrandOptions, string>> asset) =>
    RuleFor(asset)
      .Must(BrandOptions.IsAssetReference)
      .WithMessage("{PropertyName} must be a relative path, a root-relative path, or an absolute http(s) URL.");

  private void Color(Expression<Func<BrandOptions, string>> color) =>
    RuleFor(color)
      .Must(BrandOptions.IsHexColor)
      .WithMessage("Brand color must be a #RGB or #RRGGBB hex value.");
}
