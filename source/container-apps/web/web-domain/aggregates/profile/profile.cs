#region Purpose
// Domain entity for a user's personalization settings (display name, language, region, theme, notifications).
#endregion

#region Design
// Constructor requires the string settings so an instance can never exist half-initialized;
// Notifications defaults to false as the safe opt-in posture.
// The nested private Invariants validator is the template's convention for keeping entity rules
// inside the aggregate; private nesting keeps it out of AddValidatorsFromAssemblyContaining
// auto-registration (hence the CA1852 suppression rather than sealing).
#endregion

namespace TimeWarp.Architecture.Entities;

using FluentValidation;

public class Profile : BaseEntity
{
  public Profile(string displayName, string language, string region, string theme)
  {
    DisplayName = displayName;
    Language = language;
    Region = region;
    Theme = theme;
  }

  public string DisplayName { get; set; }
  public string Language { get; set; }
  public bool Notifications { get; set; }
  public string Region { get; set; }
  public string Theme { get; set; }

  # pragma warning disable CA1852
  private class Invariants : AbstractValidator<Profile>
  {
    
  }
# pragma warning restore CA1852
}
