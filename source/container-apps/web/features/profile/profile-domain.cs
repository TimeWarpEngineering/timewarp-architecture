#region Purpose
// Domain aggregate for a user's personalization settings (display name, optional email, language, region, theme, notifications).
#endregion

#region Design
// Private constructor + fail-closed static Create factories (identity style, see
// source/libraries/timewarp-identity/principals/principal.cs): DisplayName/Language/Region/Theme are
// guard-clause-validated before the instance exists, so a Profile can never be constructed
// half-initialized or with a blank required field. Create(string…) mints a new ProfileId;
// Create(ProfileId, …) is the 1:1 principal key path (GetProfile create-if-missing, task 148) —
// empty ProfileId is rejected. Named mutations (Rename/SetEmail/SetLanguage/SetRegion/SetTheme/
// EnableNotifications/DisableNotifications) keep every state change intention-revealing —
// there are no public setters.
// Email is optional progressive profile (task 205): passkey/agent-key register, session, and token
// never require it. Null or whitespace clears the field; a present value is trimmed, length-capped,
// and format-checked. It does not live on TimeWarp.Identity.Principal — identity stays credentials
// and trust; product chrome hangs here.
// MaxDisplayNameLength / MaxEmailLength are the length-rule SSOT, enforced in Create/Rename/SetEmail
// and the nested Invariants validator so the consts cannot drift inside the exemplar. Contract
// validators duplicate the literals (contracts must not reference domain).
// Language and Region use BCL checks (CultureInfo.GetCultureInfo(name, predefinedOnly: true)
// for specific cultures; ISO 3166-1 alpha-2 from those cultures' RegionInfo), not a
// handwritten list. Domain cannot reference the contracts assembly, so this is the same
// check ProfileCatalog.IsLanguage / IsRegion uses, not a duplicated string table. Theme stays
// the closed system/light/dark set. Create, named setters, and Invariants all consult these
// helpers so a store write cannot bypass the form. Stored Language is a preference only; UI
// culture application is a later i18n task.
// The nested private Invariants validator is the save-time half of the pattern —
// DomainInvariantsGuard discovers and runs it from the SaveChanges hook before persistence
// (TWA0011/TWA0012 enforce the shape at build time). Private nesting keeps it out of
// AddValidatorsFromAssemblyContaining auto-registration; the class being sealed already satisfies
// CA1852 so no pragma is needed.
#endregion

namespace TimeWarp.Architecture.Features.Profiles.Domain;

using System.Globalization;
using FluentValidation;

public sealed class Profile : Entity<ProfileId>, IAggregateRoot
{
  public const int MaxDisplayNameLength = 100;
  public const int MaxEmailLength = 254;

  private static readonly HashSet<string> AllowedRegions = BuildIso3166Alpha2();

  private static readonly HashSet<string> AllowedThemes =
  [
    "system",
    "light",
    "dark"
  ];

  private Profile(ProfileId id, string displayName, string language, string region, string theme)
    : base(id)
  {
    DisplayName = displayName;
    Language = language;
    Region = region;
    Theme = theme;
  }

  public string DisplayName { get; private set; }
  public string? Email { get; private set; }
  public string Language { get; private set; }
  public string Region { get; private set; }
  public string Theme { get; private set; }
  public bool Notifications { get; private set; }

  public static Profile Create(string displayName, string language, string region, string theme)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
    EnsureDisplayNameLength(displayName);
    EnsureLanguage(language);
    EnsureRegion(region);
    EnsureTheme(theme);

    return new Profile(ProfileId.New(), displayName, language, region, theme);
  }

  /// <summary>
  /// Create a profile with a fixed id (1:1 with the authenticated principal's UserId).
  /// </summary>
  public static Profile Create(
    ProfileId id,
    string displayName,
    string language,
    string region,
    string theme)
  {
    if (id.IsEmpty)
    {
      throw new ArgumentException("ProfileId must be non-empty.", nameof(id));
    }

    ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
    EnsureDisplayNameLength(displayName);
    EnsureLanguage(language);
    EnsureRegion(region);
    EnsureTheme(theme);

    return new Profile(id, displayName, language, region, theme);
  }

  public void Rename(string displayName)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
    EnsureDisplayNameLength(displayName);
    DisplayName = displayName;
  }

  public void SetEmail(string? email)
  {
    if (string.IsNullOrWhiteSpace(email))
    {
      Email = null;
      return;
    }

    string trimmed = email.Trim();
    ArgumentOutOfRangeException.ThrowIfGreaterThan(trimmed.Length, MaxEmailLength, nameof(email));
    if (!trimmed.Contains('@', StringComparison.Ordinal) || trimmed.StartsWith('@') || trimmed.EndsWith('@'))
    {
      throw new ArgumentException("Email must be a valid address.", nameof(email));
    }

    Email = trimmed;
  }

  public void SetLanguage(string language)
  {
    EnsureLanguage(language);
    Language = language;
  }

  public void SetRegion(string region)
  {
    EnsureRegion(region);
    Region = region;
  }

  public void SetTheme(string theme)
  {
    EnsureTheme(theme);
    Theme = theme;
  }

  public void EnableNotifications() => Notifications = true;

  public void DisableNotifications() => Notifications = false;

  private static void EnsureDisplayNameLength(string displayName) =>
    ArgumentOutOfRangeException.ThrowIfGreaterThan(displayName.Length, MaxDisplayNameLength, nameof(displayName));

  private static void EnsureLanguage(string language)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(language);
    if (!IsSpecificCulture(language))
    {
      throw new ArgumentException("Language must be a valid specific culture name (for example en-US).", nameof(language));
    }
  }

  private static void EnsureRegion(string region)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(region);
    if (!IsIso3166Alpha2(region))
    {
      throw new ArgumentException("Region must be a valid ISO 3166-1 alpha-2 code (for example US).", nameof(region));
    }
  }

  private static bool IsSpecificCulture(string language)
  {
    try
    {
      var culture = CultureInfo.GetCultureInfo(language, predefinedOnly: true);
      return !culture.IsNeutralCulture && !string.IsNullOrEmpty(culture.Name);
    }
    catch (CultureNotFoundException)
    {
      return false;
    }
  }

  private static bool IsIso3166Alpha2(string region) =>
    region.Length == 2 && AllowedRegions.Contains(region);

  private static HashSet<string> BuildIso3166Alpha2()
  {
    HashSet<string> codes = new(StringComparer.OrdinalIgnoreCase);
    foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
    {
      try
      {
        var regionInfo = new RegionInfo(culture.Name);
        if (regionInfo.TwoLetterISORegionName.Length == 2)
        {
          codes.Add(regionInfo.TwoLetterISORegionName);
        }
      }
      catch (ArgumentException)
      {
      }
    }

    return codes;
  }

  private static void EnsureTheme(string theme)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(theme);
    if (!AllowedThemes.Contains(theme))
    {
      throw new ArgumentException("Theme must be system, light, or dark.", nameof(theme));
    }
  }

  private sealed class Invariants : AbstractValidator<Profile>
  {
    public Invariants()
    {
      RuleFor(profile => profile.DisplayName).NotEmpty().MaximumLength(MaxDisplayNameLength);
      RuleFor(profile => profile.Email)
        .MaximumLength(MaxEmailLength)
        .Must(BePlausibleEmail)
        .When(profile => profile.Email is not null);
      RuleFor(profile => profile.Language).NotEmpty().Must(IsSpecificCulture);
      RuleFor(profile => profile.Region).NotEmpty().Must(IsIso3166Alpha2);
      RuleFor(profile => profile.Theme).NotEmpty().Must(AllowedThemes.Contains);
    }

    private static bool BePlausibleEmail(string? email) =>
      email?.Contains('@', StringComparison.Ordinal) == true
      && !email.StartsWith('@')
      && !email.EndsWith('@');
  }
}
