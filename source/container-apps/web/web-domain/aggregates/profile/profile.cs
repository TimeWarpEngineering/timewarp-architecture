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
