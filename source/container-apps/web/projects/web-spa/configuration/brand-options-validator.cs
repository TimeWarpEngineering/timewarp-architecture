#region Purpose
// Startup-time FluentValidation guard for BrandOptions.
#endregion

#region Design
// Wired through AddFluentValidatedOptions(...).ValidateOnStart() in the SPA ConfigureSettings,
// which the server host calls too, so a bad seed or logo crashes the host at boot.
// Internal visibility keeps the validator out of AddValidatorsFromAssemblyContaining.
// Hex and logo rules are the same predicates the stylesheet and the theme script trust.
#endregion

namespace TimeWarp.Architecture.Configuration;

using System.Linq.Expressions;

/// <summary>Validator for <see cref="BrandOptions"/>.</summary>
internal sealed class BrandOptionsValidator : AbstractValidator<BrandOptions>
{
  public BrandOptionsValidator()
  {
    RuleFor(brand => brand.ProductName).Must(BrandOptions.IsProductName);
    RuleFor(brand => brand.LogoSrc)
      .Must(BrandOptions.IsLogoReference)
      .WithMessage("LogoSrc must be a relative path, a root-relative path, or an absolute http(s) URL.");
    RuleFor(brand => brand.LogoAlt).NotEmpty().MaximumLength(120);
    RuleFor(brand => brand.LogoWidth).InclusiveBetween(1, 512);
    RuleFor(brand => brand.LogoHeight).InclusiveBetween(1, 512);
    RuleFor(brand => brand.SeedColor).Must(BrandOptions.IsHexColor);
    RuleFor(brand => brand.Tokens).NotNull();

    When
    (
      brand => brand.Tokens is not null,
      () =>
      {
        Color(brand => brand.Tokens.Purple);
        Color(brand => brand.Tokens.Purple2);
        Color(brand => brand.Tokens.PurpleDeep);
        Color(brand => brand.Tokens.PurpleSoft);
        Color(brand => brand.Tokens.Blue);
        Color(brand => brand.Tokens.BlueBright);
        Color(brand => brand.Tokens.BlueDeep);
        Color(brand => brand.Tokens.Green);
        Color(brand => brand.Tokens.GreenDeep);
        Color(brand => brand.Tokens.GreenSoft);
        Color(brand => brand.Tokens.Primary);
        Color(brand => brand.Tokens.PrimaryHover);
        Color(brand => brand.Tokens.PrimaryActive);
        Color(brand => brand.Tokens.OnPrimary);
        Color(brand => brand.Tokens.Secondary);
        Color(brand => brand.Tokens.SecondaryHover);
        Color(brand => brand.Tokens.SecondaryActive);
        Color(brand => brand.Tokens.Highlight);
        Color(brand => brand.Tokens.Info);
      }
    );
  }

  private void Color(Expression<Func<BrandOptions, string>> color) =>
    RuleFor(color)
      .Must(BrandOptions.IsHexColor)
      .WithMessage("Brand color must be a #RGB or #RRGGBB hex value.");
}
