#region Purpose
// Pins SignInOptions binding, the validator, and the three login-card predicates.
#endregion

#region Design
// No host. The validator is the same internal type ValidateOnStart runs. Both checked-in
// SignInOptions sections bind through the configuration binder and must equal new SignInOptions().
// An undefined numeric mode fails IsInEnum. A mode name the binder cannot parse throws.
#endregion

namespace SignInOptions_;

using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TimeWarp.Architecture.Configuration;

[TestTag("Unit")]
public class SignInOptions_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SignInOptions_Should_>();

  public static Task Accept_The_Three_Modes_And_Reject_An_Undefined_Value()
  {
    SignInOptionsValidator validator = new();
    validator.Validate(new SignInOptions()).IsValid.ShouldBeTrue();
    validator.Validate(new SignInOptions { Mode = SignInMode.Microsoft365Only }).IsValid.ShouldBeTrue();
    validator.Validate(new SignInOptions { Mode = SignInMode.Staff }).IsValid.ShouldBeTrue();
    validator.Validate(new SignInOptions { Mode = (SignInMode)99 }).IsValid.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Describe_Passkey_As_The_Default_Card()
  {
    SignInOptions signIn = new();
    signIn.Mode.ShouldBe(SignInMode.Passkey);
    signIn.OffersPasskey.ShouldBeTrue();
    signIn.ShowsWorkAccountLead.ShouldBeFalse();
    signIn.ShowsStaffNote.ShouldBeFalse();
    signIn.ShowMicrosoft365Unavailable(offered: false).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Describe_Microsoft365Only_Without_A_Passkey_Fallback()
  {
    SignInOptions signIn = new() { Mode = SignInMode.Microsoft365Only };
    signIn.OffersPasskey.ShouldBeFalse();
    signIn.ShowsWorkAccountLead.ShouldBeTrue();
    signIn.ShowsStaffNote.ShouldBeFalse();
    signIn.ShowMicrosoft365Unavailable(offered: true).ShouldBeFalse();
    signIn.ShowMicrosoft365Unavailable(offered: false).ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Describe_Staff_As_Microsoft365_Plus_The_Staff_Line()
  {
    SignInOptions signIn = new() { Mode = SignInMode.Staff };
    signIn.OffersPasskey.ShouldBeFalse();
    signIn.ShowsWorkAccountLead.ShouldBeTrue();
    signIn.ShowsStaffNote.ShouldBeTrue();
    SignInOptions.StaffNote.ShouldBe("Staff only.");
    SignInOptions.WorkAccountLead.ShouldBe("Use your Microsoft 365 work account.");
    return Task.CompletedTask;
  }

  public static Task Bind_Mode_Names_And_Reject_An_Unknown_Name()
  {
    SignInOptions staff = Bind(new Dictionary<string, string?>
    {
      ["SignInOptions:Mode"] = "Staff"
    });
    staff.Mode.ShouldBe(SignInMode.Staff);

    SignInOptions microsoft365 = Bind(new Dictionary<string, string?>
    {
      ["SignInOptions:Mode"] = "microsoft365only"
    });
    microsoft365.Mode.ShouldBe(SignInMode.Microsoft365Only);

    Should.Throw<InvalidOperationException>(() => Bind(new Dictionary<string, string?>
    {
      ["SignInOptions:Mode"] = "Nope"
    }));
    return Task.CompletedTask;
  }

  public static Task Bind_Both_Checked_In_Sections_To_The_Code_Default()
  {
    SignInOptions defaults = new();
    SignInOptions server = BindCheckedIn("source/container-apps/web/projects/web-server/appsettings.json");
    SignInOptions spa = BindCheckedIn("source/container-apps/web/projects/web-spa/wwwroot/appsettings.json");
    server.Mode.ShouldBe(defaults.Mode);
    spa.Mode.ShouldBe(defaults.Mode);
    server.Mode.ShouldBe(SignInMode.Passkey);
    return Task.CompletedTask;
  }

  private static SignInOptions Bind(Dictionary<string, string?> values)
  {
    ConfigurationBuilder builder = new();
    builder.AddInMemoryCollection(values);
    SignInOptions bound = new();
    builder.Build().GetSection(SignInOptions.SectionName).Bind(bound);
    return bound;
  }

  private static SignInOptions BindCheckedIn(string relativePath)
  {
    using FileStream stream = File.OpenRead(Path.Combine(RepoRoot(), relativePath));
    using JsonDocument document = JsonDocument.Parse(stream);
    Dictionary<string, string?> values = new(StringComparer.OrdinalIgnoreCase);
    Flatten(SignInOptions.SectionName, document.RootElement.GetProperty(SignInOptions.SectionName), values);
    return Bind(values);
  }

  private static void Flatten(string prefix, JsonElement element, Dictionary<string, string?> values)
  {
    if (element.ValueKind == JsonValueKind.Object)
    {
      foreach (JsonProperty property in element.EnumerateObject())
      {
        Flatten(prefix + ":" + property.Name, property.Value, values);
      }

      return;
    }

    values[prefix] = element.ValueKind switch
    {
      JsonValueKind.String => element.GetString(),
      JsonValueKind.Null => null,
      JsonValueKind.True => "true",
      JsonValueKind.False => "false",
      JsonValueKind.Number => element.GetRawText(),
      _ => throw new InvalidOperationException("SignInOptions JSON contains " + element.ValueKind + ".")
    };
  }

  private static string RepoRoot()
  {
    string? directory = AppContext.BaseDirectory;
    while (directory is not null && !File.Exists(Path.Combine(directory, "timewarp-architecture.slnx")))
    {
      directory = Path.GetDirectoryName(directory);
    }

    directory.ShouldNotBeNull();
    return directory;
  }
}
