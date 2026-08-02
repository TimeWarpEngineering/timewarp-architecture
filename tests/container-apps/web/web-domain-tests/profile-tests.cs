namespace Profile_;

public class Create
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Create>();

  public static Task Sets_properties()
  {
    Profile profile = Profile.Create("Ada Lovelace", "en-US", "US", "dark");

    profile.DisplayName.ShouldBe("Ada Lovelace");
    profile.Language.ShouldBe("en-US");
    profile.Region.ShouldBe("US");
    profile.Theme.ShouldBe("dark");
    profile.Notifications.ShouldBeFalse();
    profile.Id.IsEmpty.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Assigns_distinct_ids()
  {
    Profile a = Profile.Create("Ada", "en-US", "US", "dark");
    Profile b = Profile.Create("Ada", "en-US", "US", "dark");
    a.Id.ShouldNotBe(b.Id);
    return Task.CompletedTask;
  }

  public static Task Rejects_null_displayName()
  {
    Should.Throw<ArgumentException>(() => Profile.Create(null!, "en-US", "US", "dark"));
    return Task.CompletedTask;
  }

  public static Task Rejects_whitespace_displayName()
  {
    Should.Throw<ArgumentException>(() => Profile.Create("   ", "en-US", "US", "dark"));
    return Task.CompletedTask;
  }

  public static Task Rejects_whitespace_language()
  {
    Should.Throw<ArgumentException>(() => Profile.Create("Ada", "   ", "US", "dark"));
    return Task.CompletedTask;
  }

  public static Task Rejects_whitespace_region()
  {
    Should.Throw<ArgumentException>(() => Profile.Create("Ada", "en-US", "   ", "dark"));
    return Task.CompletedTask;
  }

  public static Task Rejects_whitespace_theme()
  {
    Should.Throw<ArgumentException>(() => Profile.Create("Ada", "en-US", "US", "   "));
    return Task.CompletedTask;
  }

  public static Task Accepts_displayName_at_max_length()
  {
    string maxLengthName = new('a', Profile.MaxDisplayNameLength);
    Profile profile = Profile.Create(maxLengthName, "en-US", "US", "dark");
    profile.DisplayName.ShouldBe(maxLengthName);
    return Task.CompletedTask;
  }

  public static Task Rejects_displayName_over_max_length()
  {
    string tooLongName = new('a', Profile.MaxDisplayNameLength + 1);
    Should.Throw<ArgumentOutOfRangeException>(() => Profile.Create(tooLongName, "en-US", "US", "dark"));
    return Task.CompletedTask;
  }
}

public class Rename
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Rename>();

  public static Task Updates_display_name()
  {
    Profile profile = Profile.Create("Ada", "en-US", "US", "dark");
    profile.Rename("Grace Hopper");
    profile.DisplayName.ShouldBe("Grace Hopper");
    return Task.CompletedTask;
  }

  public static Task Rejects_whitespace()
  {
    Profile profile = Profile.Create("Ada", "en-US", "US", "dark");
    Should.Throw<ArgumentException>(() => profile.Rename("   "));
    return Task.CompletedTask;
  }

  public static Task Rejects_displayName_over_max_length()
  {
    Profile profile = Profile.Create("Ada", "en-US", "US", "dark");
    string tooLongName = new('a', Profile.MaxDisplayNameLength + 1);
    Should.Throw<ArgumentOutOfRangeException>(() => profile.Rename(tooLongName));
    return Task.CompletedTask;
  }
}

public class SetLanguage
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SetLanguage>();

  public static Task Updates_language()
  {
    Profile profile = Profile.Create("Ada", "en-US", "US", "dark");
    profile.SetLanguage("fr-FR");
    profile.Language.ShouldBe("fr-FR");
    return Task.CompletedTask;
  }

  public static Task Rejects_whitespace()
  {
    Profile profile = Profile.Create("Ada", "en-US", "US", "dark");
    Should.Throw<ArgumentException>(() => profile.SetLanguage("   "));
    return Task.CompletedTask;
  }
}

public class SetRegion
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SetRegion>();

  public static Task Updates_region()
  {
    Profile profile = Profile.Create("Ada", "en-US", "US", "dark");
    profile.SetRegion("FR");
    profile.Region.ShouldBe("FR");
    return Task.CompletedTask;
  }

  public static Task Rejects_whitespace()
  {
    Profile profile = Profile.Create("Ada", "en-US", "US", "dark");
    Should.Throw<ArgumentException>(() => profile.SetRegion("   "));
    return Task.CompletedTask;
  }
}

public class SetTheme
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SetTheme>();

  public static Task Updates_theme()
  {
    Profile profile = Profile.Create("Ada", "en-US", "US", "dark");
    profile.SetTheme("light");
    profile.Theme.ShouldBe("light");
    return Task.CompletedTask;
  }

  public static Task Rejects_whitespace()
  {
    Profile profile = Profile.Create("Ada", "en-US", "US", "dark");
    Should.Throw<ArgumentException>(() => profile.SetTheme("   "));
    return Task.CompletedTask;
  }
}

public class NotificationsLifecycle
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<NotificationsLifecycle>();

  public static Task Enable_and_disable()
  {
    Profile profile = Profile.Create("Ada", "en-US", "US", "dark");
    profile.Notifications.ShouldBeFalse();

    profile.EnableNotifications();
    profile.Notifications.ShouldBeTrue();

    profile.DisableNotifications();
    profile.Notifications.ShouldBeFalse();
    return Task.CompletedTask;
  }
}
