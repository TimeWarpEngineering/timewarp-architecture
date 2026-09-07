#region Purpose
// Shared editable progressive-profile shape, closed Language/Region/Theme catalogs, and validation.
#endregion

#region Design
// Validating against the interface lets UpdateProfile.Command and GetProfile.Response share one
// rule set via SetValidator so the Profile page form matches the PUT body. Avatar is display-only
// (GetProfile) and is not on this interface. Length literals duplicate Profile.MaxDisplayNameLength
// / MaxEmailLength — contracts must not reference the domain assembly. Email is optional
// (progressive; never a register/session gate). Alias stays required so chrome always has a name
// (GetProfile create-if-missing defaults to "Member").
// Language, Region, and Theme are closed catalogs (not free text): the SPA binds FluentSelect to
// ProfileCatalog and ProfileDetailsValidator.Must membership so typed PUT cannot sneak junk the
// form cannot type. Domain duplicates the code sets (it cannot reference contracts) so a store
// write cannot bypass. Catalogs are curated demo subsets — not CultureInfo.GetCultures() and not
// every ISO 3166-1 row.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

public interface IProfileDetails
{
  public string Alias { get; set; }
  public string? Email { get; set; }
  public string Language { get; set; }
  public string Region { get; set; }
  public string Theme { get; set; }
  public bool Notifications { get; set; }
}

public static class ProfileCatalog
{
  public sealed record Entry(string Code, string Label);

  public static readonly IReadOnlyList<Entry> Languages =
  [
    new("en-US", "English (United States)"),
    new("en-GB", "English (United Kingdom)"),
    new("fr-FR", "French (France)"),
    new("de-DE", "German (Germany)"),
    new("es-ES", "Spanish (Spain)"),
    new("it-IT", "Italian (Italy)"),
    new("pt-BR", "Portuguese (Brazil)"),
    new("ja-JP", "Japanese (Japan)"),
    new("zh-CN", "Chinese (Simplified, China)"),
    new("ko-KR", "Korean (Korea)"),
    new("nl-NL", "Dutch (Netherlands)"),
    new("sv-SE", "Swedish (Sweden)"),
    new("ar-SA", "Arabic (Saudi Arabia)"),
    new("hi-IN", "Hindi (India)"),
    new("pl-PL", "Polish (Poland)")
  ];

  public static readonly IReadOnlyList<Entry> Regions =
  [
    new("US", "United States"),
    new("GB", "United Kingdom"),
    new("FR", "France"),
    new("DE", "Germany"),
    new("ES", "Spain"),
    new("IT", "Italy"),
    new("PT", "Portugal"),
    new("BR", "Brazil"),
    new("JP", "Japan"),
    new("CN", "China"),
    new("KR", "Korea"),
    new("NL", "Netherlands"),
    new("SE", "Sweden"),
    new("SA", "Saudi Arabia"),
    new("IN", "India"),
    new("PL", "Poland"),
    new("CA", "Canada"),
    new("AU", "Australia"),
    new("MX", "Mexico"),
    new("NZ", "New Zealand"),
    new("IE", "Ireland"),
    new("AT", "Austria"),
    new("CH", "Switzerland"),
    new("BE", "Belgium"),
    new("DK", "Denmark"),
    new("NO", "Norway"),
    new("FI", "Finland")
  ];

  public static readonly IReadOnlyList<Entry> Themes =
  [
    new("system", "System"),
    new("light", "Light"),
    new("dark", "Dark")
  ];

  public static bool IsLanguage(string? value) =>
    Languages.Any(entry => entry.Code == value);

  public static bool IsRegion(string? value) =>
    Regions.Any(entry => entry.Code == value);

  public static bool IsTheme(string? value) =>
    Themes.Any(entry => entry.Code == value);
}

public sealed class ProfileDetailsValidator : AbstractValidator<IProfileDetails>
{
  public const int MaxAliasLength = 100;
  public const int MaxEmailLength = 254;

  public ProfileDetailsValidator()
  {
    RuleFor(details => details.Alias).NotEmpty().MaximumLength(MaxAliasLength);
    RuleFor(details => details.Email)
      .MaximumLength(MaxEmailLength)
      .EmailAddress()
      .When(details => !string.IsNullOrWhiteSpace(details.Email));
    RuleFor(details => details.Language)
      .NotEmpty()
      .Must(ProfileCatalog.IsLanguage)
      .WithMessage("Language must be a supported culture (for example en-US).");
    RuleFor(details => details.Region)
      .NotEmpty()
      .Must(ProfileCatalog.IsRegion)
      .WithMessage("Region must be a supported ISO 3166-1 country code (for example US).");
    RuleFor(details => details.Theme)
      .NotEmpty()
      .Must(ProfileCatalog.IsTheme)
      .WithMessage("Theme must be system, light, or dark.");
  }
}
