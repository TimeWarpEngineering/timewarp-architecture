#region Purpose
// Domain aggregate for a user's personalization settings (display name, language, region, theme, notifications).
#endregion

#region Design
// Private constructor + fail-closed static Create factory (identity style, see
// source/libraries/timewarp-identity/principal.cs): DisplayName/Language/Region/Theme are
// guard-clause-validated before the instance exists, so a Profile can never be constructed
// half-initialized or with a blank required field. Named mutations (Rename/SetLanguage/SetRegion/
// SetTheme/EnableNotifications/DisableNotifications) keep every state change intention-revealing —
// there are no public setters.
// MaxDisplayNameLength is the single source of truth for the length rule, enforced in BOTH places
// that can produce a DisplayName (Create and Rename) as well as the nested Invariants validator —
// deliberately not validator-only, so the const cannot drift into "guard clauses say one thing, the
// validator says another" inside the exemplar itself (the agreement-by-memory failure mode this
// repo organizes against). Future contract validators should reference Profile.MaxDisplayNameLength
// instead of duplicating the literal (relates to TWA0002/TWA0003 nullability-vs-presence agreement
// on the contract side).
// The nested private Invariants validator is still the save-time half of the pattern —
// DomainInvariantsGuard discovers and runs it from the SaveChanges hook before persistence
// (TWA0011/TWA0012 enforce the shape at build time) — so an invalid DisplayName can never be
// persisted even if some future code path bypasses Create/Rename. Private nesting keeps it out of
// AddValidatorsFromAssemblyContaining auto-registration; the class being sealed already satisfies
// CA1852 so no pragma is needed.
#endregion

namespace TimeWarp.Architecture.Aggregates.Profiles;

using FluentValidation;

public sealed class Profile : Entity<ProfileId>, IAggregateRoot
{
  public const int MaxDisplayNameLength = 100;

  private Profile(ProfileId id, string displayName, string language, string region, string theme)
    : base(id)
  {
    DisplayName = displayName;
    Language = language;
    Region = region;
    Theme = theme;
  }

  public string DisplayName { get; private set; }
  public string Language { get; private set; }
  public string Region { get; private set; }
  public string Theme { get; private set; }
  public bool Notifications { get; private set; }

  public static Profile Create(string displayName, string language, string region, string theme)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
    ArgumentException.ThrowIfNullOrWhiteSpace(language);
    ArgumentException.ThrowIfNullOrWhiteSpace(region);
    ArgumentException.ThrowIfNullOrWhiteSpace(theme);
    EnsureDisplayNameLength(displayName);

    return new Profile(ProfileId.New(), displayName, language, region, theme);
  }

  public void Rename(string displayName)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
    EnsureDisplayNameLength(displayName);
    DisplayName = displayName;
  }

  public void SetLanguage(string language)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(language);
    Language = language;
  }

  public void SetRegion(string region)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(region);
    Region = region;
  }

  public void SetTheme(string theme)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(theme);
    Theme = theme;
  }

  public void EnableNotifications() => Notifications = true;

  public void DisableNotifications() => Notifications = false;

  private static void EnsureDisplayNameLength(string displayName) =>
    ArgumentOutOfRangeException.ThrowIfGreaterThan(displayName.Length, MaxDisplayNameLength, nameof(displayName));

  private sealed class Invariants : AbstractValidator<Profile>
  {
    public Invariants()
    {
      RuleFor(profile => profile.DisplayName).NotEmpty().MaximumLength(MaxDisplayNameLength);
      RuleFor(profile => profile.Language).NotEmpty();
      RuleFor(profile => profile.Region).NotEmpty();
      RuleFor(profile => profile.Theme).NotEmpty();
    }
  }
}
