#region Purpose
// Pins SignInOptions binding and the startup validator.
#endregion

#region Design
// No host. SignInOptions and SignInOptionsValidator live in web-application. The initializer lists
// Passkey and Microsoft365. The checked-in web-server section lists Passkey only. The SPA
// wwwroot file has no SignInOptions section. An unknown method name fails the validator, which
// reads the raw configuration. Empty lists and duplicates fail too. IncludeMicrosoft365
// appends Microsoft365 beside the configured list and does not replace index 0.
#endregion

namespace SignInOptions_;

using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TimeWarp.Architecture.Features.Identity;
using SignInOptions = TimeWarp.Architecture.Features.Identity.Application.SignInOptions;
using SignInOptionsValidator = TimeWarp.Architecture.Features.Identity.Application.SignInOptionsValidator;

[TestTag("Unit")]
public class SignInOptions_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SignInOptions_Should_>();

  public static Task Default_To_Both_Methods_And_Reject_Empty_Duplicates_And_Undefined_Values()
  {
    IConfiguration empty = new ConfigurationBuilder().Build();
    SignInOptionsValidator validator = new(empty);
    SignInOptions defaults = new();
    defaults.Methods.ShouldBe([SignInMethod.Passkey, SignInMethod.Microsoft365]);
    validator.Validate(defaults).IsValid.ShouldBeTrue();
    validator.Validate(new SignInOptions { Methods = [SignInMethod.Microsoft365] }).IsValid.ShouldBeTrue();
    validator.Validate(new SignInOptions { Methods = [] }).IsValid.ShouldBeFalse();
    validator.Validate(new SignInOptions
    {
      Methods = [SignInMethod.Passkey, SignInMethod.Passkey]
    }).IsValid.ShouldBeFalse();
    validator.Validate(new SignInOptions { Methods = [(SignInMethod)99] }).IsValid.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Reject_An_Unknown_Method_Name()
  {
    IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["SignInOptions:Methods:0"] = "Nope"
    }).Build();
    SignInOptionsValidator validator = new(configuration);
    validator.Validate(new SignInOptions { Methods = [SignInMethod.Passkey] }).IsValid.ShouldBeFalse();

    IConfiguration wrongCase = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["SignInOptions:Methods:0"] = "microsoft365"
    }).Build();
    new SignInOptionsValidator(wrongCase)
      .Validate(new SignInOptions { Methods = [SignInMethod.Microsoft365] })
      .IsValid.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Replace_The_Initializer_With_The_Checked_In_Passkey_List()
  {
    SignInOptions server = new();
    server.Methods.ShouldBe([SignInMethod.Passkey, SignInMethod.Microsoft365]);
    server.ApplyConfiguredMethods(LoadCheckedIn("source/container-apps/web/projects/web-server/appsettings.json"));
    server.Methods.ShouldBe([SignInMethod.Passkey]);
    return Task.CompletedTask;
  }

  public static Task Leave_SignInOptions_Out_Of_The_Spa_Appsettings()
  {
    string json = File.ReadAllText(Path.Combine(
      RepoRoot(),
      "source/container-apps/web/projects/web-spa/wwwroot/appsettings.json"));
    using JsonDocument document = JsonDocument.Parse(json);
    document.RootElement.TryGetProperty("SignInOptions", out _).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Append_Microsoft365_When_Asked_And_Leave_Index_Zero_Alone()
  {
    SignInOptions appended = new();
    appended.ApplyConfiguredMethods(Methods("Passkey", includeMicrosoft365: "true"));
    appended.Methods.ShouldBe([SignInMethod.Passkey, SignInMethod.Microsoft365]);
    SignInOptions.ConfigurationLists(Methods("Passkey", includeMicrosoft365: "true"), SignInMethod.Microsoft365)
      .ShouldBeTrue();
    SignInOptions.RequestsMicrosoft365(Methods("Passkey", includeMicrosoft365: "1")).ShouldBeTrue();

    SignInOptions untouched = new();
    untouched.ApplyConfiguredMethods(Methods("Passkey", includeMicrosoft365: null));
    untouched.Methods.ShouldBe([SignInMethod.Passkey]);
    SignInOptions.ConfigurationLists(Methods("Passkey", includeMicrosoft365: "false"), SignInMethod.Microsoft365)
      .ShouldBeFalse();

    SignInOptions kept = new();
    kept.ApplyConfiguredMethods(Methods("Microsoft365", includeMicrosoft365: "false"));
    kept.Methods.ShouldBe([SignInMethod.Microsoft365]);
    return Task.CompletedTask;
  }

  private static IConfiguration Methods(string method, string? includeMicrosoft365)
  {
    Dictionary<string, string?> values = new()
    {
      ["SignInOptions:Methods:0"] = method
    };
    if (includeMicrosoft365 is not null)
    {
      values["SignInOptions:IncludeMicrosoft365"] = includeMicrosoft365;
    }

    return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
  }

  private static IConfiguration LoadCheckedIn(string relativePath)
  {
    using FileStream stream = File.OpenRead(Path.Combine(RepoRoot(), relativePath));
    using JsonDocument document = JsonDocument.Parse(stream);
    Dictionary<string, string?> values = new(StringComparer.OrdinalIgnoreCase);
    Flatten(SignInOptions.SectionName, document.RootElement.GetProperty(SignInOptions.SectionName), values);
    return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
  }

  private static void Flatten(string prefix, JsonElement element, Dictionary<string, string?> values)
  {
    if (element.ValueKind == JsonValueKind.Object)
    {
      foreach (JsonProperty property in element.EnumerateObject())
      {
        if (property.NameEquals("_comment"))
        {
          continue;
        }

        Flatten(prefix + ":" + property.Name, property.Value, values);
      }

      return;
    }

    if (element.ValueKind == JsonValueKind.Array)
    {
      int index = 0;
      foreach (JsonElement item in element.EnumerateArray())
      {
        Flatten(prefix + ":" + index, item, values);
        index++;
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
