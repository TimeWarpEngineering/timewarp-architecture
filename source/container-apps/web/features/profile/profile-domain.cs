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
// Language/Region/Theme are closed catalogs. Codes here must match ProfileCatalog in
// profile-details-contracts.cs (contracts cannot reference this assembly). Create, named
// setters, and Invariants all consult the same HashSets so a store write cannot bypass the form.
// The nested private Invariants validator is the save-time half of the pattern —
// DomainInvariantsGuard discovers and runs it from the SaveChanges hook before persistence
// (TWA0011/TWA0012 enforce the shape at build time). Private nesting keeps it out of
// AddValidatorsFromAssemblyContaining auto-registration; the class being sealed already satisfies
// CA1852 so no pragma is needed.
#endregion

namespace TimeWarp.Architecture.Features.Profiles.Domain;

using FluentValidation;

public sealed class Profile : Entity<ProfileId>, IAggregateRoot
{
  public const int MaxDisplayNameLength = 100;
  public const int MaxEmailLength = 254;

  private static readonly HashSet<string> AllowedLanguages =
  [
    "en-US",
    "en-GB",
    "fr-FR",
    "de-DE",
    "es-ES",
    "it-IT",
    "pt-BR",
    "ja-JP",
    "zh-CN",
    "ko-KR",
    "nl-NL",
    "sv-SE",
    "ar-SA",
    "hi-IN",
    "pl-PL"
  ];

  private static readonly HashSet<string> AllowedRegions =
  [
    "US",
    "GB",
    "FR",
    "DE",
    "ES",
    "IT",
    "PT",
    "BR",
    "JP",
    "CN",
    "KR",
    "NL",
    "SE",
    "SA",
    "IN",
    "PL",
    "CA",
    "AU",
    "MX",
    "NZ",
    "IE",
    "AT",
    "CH",
    "BE",
    "DK",
    "NO",
    "FI"
  ];

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
    if (!AllowedLanguages.Contains(language))
    {
      throw new ArgumentException("Language must be a supported culture name (for example en-US).", nameof(language));
    }
  }

  private static void EnsureRegion(string region)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(region);
    if (!AllowedRegions.Contains(region))
    {
      throw new ArgumentException("Region must be a supported ISO 3166-1 country code (for example US).", nameof(region));
    }
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
      RuleFor(profile => profile.Language).NotEmpty().Must(AllowedLanguages.Contains);
      RuleFor(profile => profile.Region).NotEmpty().Must(AllowedRegions.Contains);
      RuleFor(profile => profile.Theme).NotEmpty().Must(AllowedThemes.Contains);
    }

    private static bool BePlausibleEmail(string? email) =>
      email?.Contains('@', StringComparison.Ordinal) == true
      && !email.StartsWith('@')
      && !email.EndsWith('@');
  }
}
