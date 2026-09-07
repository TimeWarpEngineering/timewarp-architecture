#region Purpose
// Shared editable progressive-profile shape, ISO Language/Region catalogs, Theme set, and validation.
#endregion

#region Design
// Validating against the interface lets UpdateProfile.Command and GetProfile.Response share one
// rule set via SetValidator so the Profile page form matches the PUT body. Avatar is display-only
// (GetProfile) and is not on this interface. Length literals duplicate Profile.MaxDisplayNameLength
// / MaxEmailLength — contracts must not reference the domain assembly. Email is optional
// (progressive; never a register/session gate). Alias stays required so chrome always has a name
// (GetProfile create-if-missing defaults to "Member").
// Language and Region are BCL ISO catalogs, not handwritten lists: Languages/Regions hold Entry
// rows (specific cultures + distinct ISO 3166-1 alpha-2); LanguageCodes/RegionCodes cache the
// same-order code lists for Combobox Items. Validators use CultureInfo.GetCultureInfo(name,
// predefinedOnly: true) for language and membership in the GetCultures-derived region set
// (new RegionInfo(alpha2) rejects a few catalog codes such as EH/DG/EA/IC). Domain repeats those
// BCL checks; it cannot reference this assembly. Theme stays the closed system/light/dark set.
// Fluent Combobox closed text comes from OnAfterRenderAsync: Value is TOption then GetOptionText,
// so Language/Region bind TOption == TValue == string (ISO tag) with OptionText → LabelFor.
// Matching/LabelFor remain for catalog lookup (tests and LabelFor OptionText). Recognizing a
// stored locale is not applying UI translations. Profile.Language is a preference; missing
// resources fall back to English (web-spa SetIsoCulture stays en-US). Language and Region are
// independent (th-TH + US is valid). Junk such as "en-US asdfasdf" still fails the BCL checks.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

using System.Globalization;

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

  public static readonly IReadOnlyList<Entry> Languages = BuildLanguages();

  public static readonly IReadOnlyList<Entry> Regions = BuildRegions();

  public static readonly IReadOnlyList<string> LanguageCodes =
    Languages.Select(entry => entry.Code).ToArray();

  public static readonly IReadOnlyList<string> RegionCodes =
    Regions.Select(entry => entry.Code).ToArray();

  private static readonly HashSet<string> RegionCodeSet =
    new(RegionCodes, StringComparer.OrdinalIgnoreCase);

  public static readonly IReadOnlyList<Entry> Themes =
  [
    new("system", "System"),
    new("light", "Light"),
    new("dark", "Dark")
  ];

  public static bool IsLanguage(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    try
    {
      var culture = CultureInfo.GetCultureInfo(value, predefinedOnly: true);
      return !culture.IsNeutralCulture && !string.IsNullOrEmpty(culture.Name);
    }
    catch (CultureNotFoundException)
    {
      return false;
    }
  }

  public static bool IsRegion(string? value) =>
    value is { Length: 2 } && RegionCodeSet.Contains(value);

  public static bool IsTheme(string? value) =>
    Themes.Any(entry => entry.Code == value);

  public static IReadOnlyList<Entry> Matching(IReadOnlyList<Entry> catalog, string? code)
  {
    if (string.IsNullOrWhiteSpace(code))
    {
      return [];
    }

    Entry? match = catalog.FirstOrDefault(entry =>
      string.Equals(entry.Code, code, StringComparison.OrdinalIgnoreCase));
    return match is null ? [] : [match];
  }

  public static string LabelFor(IReadOnlyList<Entry> catalog, string? code)
  {
    IReadOnlyList<Entry> match = Matching(catalog, code);
    return match.Count == 0 ? code ?? string.Empty : match[0].Label;
  }

  private static IReadOnlyList<Entry> BuildLanguages()
  {
    return
    [
      .. CultureInfo.GetCultures(CultureTypes.SpecificCultures)
        .Where(culture => !string.IsNullOrEmpty(culture.Name) && !culture.IsNeutralCulture)
        .Select(culture => new Entry(culture.Name, culture.EnglishName))
        .DistinctBy(entry => entry.Code, StringComparer.OrdinalIgnoreCase)
        .OrderBy(entry => entry.Label, StringComparer.OrdinalIgnoreCase)
        .ThenBy(entry => entry.Code, StringComparer.OrdinalIgnoreCase)
    ];
  }

  private static IReadOnlyList<Entry> BuildRegions()
  {
    Dictionary<string, Entry> byCode = new(StringComparer.OrdinalIgnoreCase);
    foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
    {
      RegionInfo regionInfo;
      try
      {
        regionInfo = new RegionInfo(culture.Name);
      }
      catch (ArgumentException)
      {
        continue;
      }

      string code = regionInfo.TwoLetterISORegionName;
      if (code.Length != 2)
      {
        continue;
      }

      byCode.TryAdd(code, new Entry(code, regionInfo.EnglishName));
    }

    return
    [
      .. byCode.Values
        .OrderBy(entry => entry.Label, StringComparer.OrdinalIgnoreCase)
        .ThenBy(entry => entry.Code, StringComparer.OrdinalIgnoreCase)
    ];
  }
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
      .WithMessage("Language must be a valid specific culture name (for example en-US).");
    RuleFor(details => details.Region)
      .NotEmpty()
      .Must(ProfileCatalog.IsRegion)
      .WithMessage("Region must be a valid ISO 3166-1 alpha-2 code (for example US).");
    RuleFor(details => details.Theme)
      .NotEmpty()
      .Must(ProfileCatalog.IsTheme)
      .WithMessage("Theme must be system, light, or dark.");
  }
}
