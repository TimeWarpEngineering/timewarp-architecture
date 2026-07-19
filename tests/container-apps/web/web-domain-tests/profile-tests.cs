namespace Profile_;

public class Create
{
  public void Sets_properties()
  {
    Profile profile = Profile.Create("Ada Lovelace", "en-US", "US", "dark");

    profile.DisplayName.ShouldBe("Ada Lovelace");
    profile.Language.ShouldBe("en-US");
    profile.Region.ShouldBe("US");
    profile.Theme.ShouldBe("dark");
    profile.Notifications.ShouldBeFalse();
    profile.Id.IsEmpty.ShouldBeFalse();
  }

  public void Assigns_distinct_ids()
  {
    Profile a = Profile.Create("Ada", "en-US", "US", "dark");
    Profile b = Profile.Create("Ada", "en-US", "US", "dark");
    a.Id.ShouldNotBe(b.Id);
  }

  public void Rejects_null_displayName() =>
    Should.Throw<ArgumentException>(() => Profile.Create(null!, "en-US", "US", "dark"));

  public void Rejects_whitespace_displayName() =>
    Should.Throw<ArgumentException>(() => Profile.Create("   ", "en-US", "US", "dark"));

  public void Rejects_whitespace_language() =>
    Should.Throw<ArgumentException>(() => Profile.Create("Ada", "   ", "US", "dark"));

  public void Rejects_whitespace_region() =>
    Should.Throw<ArgumentException>(() => Profile.Create("Ada", "en-US", "   ", "dark"));

  public void Rejects_whitespace_theme() =>
    Should.Throw<ArgumentException>(() => Profile.Create("Ada", "en-US", "US", "   "));
}

public class Rename
{
  public void Updates_display_name()
  {
    Profile profile = Profile.Create("Ada", "en-US", "US", "dark");
    profile.Rename("Grace Hopper");
    profile.DisplayName.ShouldBe("Grace Hopper");
  }

  public void Rejects_whitespace()
  {
    Profile profile = Profile.Create("Ada", "en-US", "US", "dark");
    Should.Throw<ArgumentException>(() => profile.Rename("   "));
  }
}

public class SetLanguage
{
  public void Updates_language()
  {
    Profile profile = Profile.Create("Ada", "en-US", "US", "dark");
    profile.SetLanguage("fr-FR");
    profile.Language.ShouldBe("fr-FR");
  }

  public void Rejects_whitespace()
  {
    Profile profile = Profile.Create("Ada", "en-US", "US", "dark");
    Should.Throw<ArgumentException>(() => profile.SetLanguage("   "));
  }
}

public class SetRegion
{
  public void Updates_region()
  {
    Profile profile = Profile.Create("Ada", "en-US", "US", "dark");
    profile.SetRegion("FR");
    profile.Region.ShouldBe("FR");
  }

  public void Rejects_whitespace()
  {
    Profile profile = Profile.Create("Ada", "en-US", "US", "dark");
    Should.Throw<ArgumentException>(() => profile.SetRegion("   "));
  }
}

public class SetTheme
{
  public void Updates_theme()
  {
    Profile profile = Profile.Create("Ada", "en-US", "US", "dark");
    profile.SetTheme("light");
    profile.Theme.ShouldBe("light");
  }

  public void Rejects_whitespace()
  {
    Profile profile = Profile.Create("Ada", "en-US", "US", "dark");
    Should.Throw<ArgumentException>(() => profile.SetTheme("   "));
  }
}

public class NotificationsLifecycle
{
  public void Enable_and_disable()
  {
    Profile profile = Profile.Create("Ada", "en-US", "US", "dark");
    profile.Notifications.ShouldBeFalse();

    profile.EnableNotifications();
    profile.Notifications.ShouldBeTrue();

    profile.DisableNotifications();
    profile.Notifications.ShouldBeFalse();
  }
}
