#region Purpose
// Pins the dev-run xAI key check: environment or `Key = Value` user-secrets lines, never the value.
#endregion

namespace XaiPreflight_;

public class HasKey_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<HasKey_Given_>();

  public static Task EnvironmentValue_Should_Count()
  {
    XaiPreflight.HasKey("present", secretsList: null).ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task BlankEnvironment_And_SpacedSecretsLine_Should_Count()
  {
    XaiPreflight.HasKey("  ", "Other = 1\nXAI:ApiKey = secret-value\n").ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task SecretsLine_Without_Spaces_Around_Equals_Should_Not_Count()
  {
    XaiPreflight.HasKey(null, "XAI:ApiKey=secret-value").ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task BlankSecretValue_Should_Not_Count()
  {
    XaiPreflight.HasKey(null, "XAI:ApiKey =   ").ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task MissingKey_Should_Not_Count()
  {
    XaiPreflight.HasKey(null, "Azure:SubscriptionId = abc").ShouldBeFalse();
    return Task.CompletedTask;
  }
}

public class WarningLead_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<WarningLead_Given_>();

  public static Task Leads_Should_NameTheCommand_And_NotASecret()
  {
    string readable = XaiPreflight.WarningLead(secretsListSucceeded: true);
    string unreadable = XaiPreflight.WarningLead(secretsListSucceeded: false);
    readable.ShouldContain("XAI:ApiKey");
    unreadable.ShouldContain("XAI:ApiKey");
    readable.ShouldNotContain("secret-value");
    unreadable.ShouldNotContain("secret-value");
    XaiPreflight.SetupCommand.ShouldContain(XaiPreflight.WebServerProject);
    XaiPreflight.SetupCommand.ShouldContain("<your-xai-key>");
    return Task.CompletedTask;
  }

  public static Task SetupCommand_Should_AppearInTheContractsSource()
  {
    string? directory = AppContext.BaseDirectory;
    while (directory is not null && !File.Exists(Path.Combine(directory, "timewarp-architecture.slnx")))
    {
      directory = Path.GetDirectoryName(directory);
    }

    directory.ShouldNotBeNull();
    string contracts = File.ReadAllText(Path.Combine(
      directory,
      "source",
      "container-apps",
      "web",
      "features",
      "agent-chat",
      "xai-chat-contracts.cs"));
    contracts.ShouldContain(XaiPreflight.WebServerProject);
    contracts.ShouldContain("<your-xai-key>");
    contracts.ShouldContain("XAI:ApiKey");
    return Task.CompletedTask;
  }
}
