#region Purpose
// Production-safety guard over the aca-neon publish output (task 317): Azure Container Apps, external Neon Postgres.
#endregion

#region Design
// Same two-source shape as aca-publish-tests. SetupOnce publishes in-proc with Publish:Target=aca-neon
// unless TIMEWARP_ACA_NEON_OUTPUT points at `dev publish aca-neon` output. Publishing writes Bicep only.
// The facts that matter for this target, and that the aca suite must keep failing if they leak the other way:
// no Flexible Server, no Postgres Key Vault, no firewall rule, min replicas 0, a small container,
// web-server's ConnectionStrings__postgres-db is the pooled secret. The direct string is not an Azure
// resource: dev deploy migrate reads it on the operator machine, so publish output has no
// web-migrations container and no neon-direct parameter. The longer EF retry is on web-server only.
// CPU is emitted as json('0.25'). Azure.Provisioning formats the double with the invariant culture.
// The pooled-string fact compiles only with web and postgres: Neon is declared inside that AppHost block.
// The passkey-host fact compiles only with web and yarp. Without yarp there is no ingress, so no
// container app is external. SigNoz (task 417) is run-mode only: the Bicep does not name signoz.
// Publish logs MissingParameterValueException while Aspire copies the two
// secret parameters into deployment state. They are @secure() with no default; the warning is not a
// publish failure, and a default would put the connection string in the Bicep.
#endregion

namespace Aspire.Tests;

using System.Text.RegularExpressions;

[TestTag("Integration")]
public partial class AcaNeonPublish_Given_
{
  internal const string OutputEnvironmentVariable = "TIMEWARP_ACA_NEON_OUTPUT";

  private static string OutputDirectory = null!;
  private static bool OwnsOutputDirectory;
  private static Dictionary<string, string> Modules = null!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AcaNeonPublish_Given_>();

  public static async Task SetupOnce()
  {
    string? provided = Environment.GetEnvironmentVariable(OutputEnvironmentVariable);
    if (string.IsNullOrWhiteSpace(provided))
    {
      OutputDirectory = Directory.CreateTempSubdirectory("aca-neon-publish-").FullName;
      OwnsOutputDirectory = true;
      await PublishInProcAsync(OutputDirectory);
    }
    else
    {
      OutputDirectory = Path.GetFullPath(provided);
    }

    File.Exists(Path.Combine(OutputDirectory, "main.bicep")).ShouldBeTrue($"publish produced no main.bicep in {OutputDirectory}");
    Modules = Directory.EnumerateFiles(OutputDirectory, "*.bicep", SearchOption.AllDirectories)
      .ToDictionary(path => Path.GetRelativePath(OutputDirectory, path).Replace('\\', '/'), File.ReadAllText);
    Modules.ShouldNotBeEmpty();
  }

  public static Task CleanUpOnce()
  {
    if (OwnsOutputDirectory && Directory.Exists(OutputDirectory))
    {
      Directory.Delete(OutputDirectory, recursive: true);
    }

    return Task.CompletedTask;
  }

  public static async Task Publish_Should_NotProvisionFlexibleServerOrKeyVault()
  {
    string all = string.Join('\n', Modules.Values);
    all.ShouldNotContain("Microsoft.DBforPostgreSQL/flexibleServers");
    all.ShouldNotContain("Microsoft.KeyVault/vaults");
    all.ShouldNotContain("postgres-kv");
    all.ShouldNotContain("firewallRules");
    all.ShouldNotContain("keyVaultUrl:");
    await Task.CompletedTask;
  }

  public static async Task Publish_Should_ScaleToZeroWithASmallContainer()
  {
    bool sawApp = false;
    foreach ((string module, string text) in Modules)
    {
      if (!text.Contains("Microsoft.App/containerApps@", StringComparison.Ordinal))
      {
        continue;
      }

      sawApp = true;
      text.ShouldContain("minReplicas: 0", Case.Sensitive, module);
      text.ShouldNotContain("minReplicas: 1", Case.Sensitive, module);
      text.ShouldContain("maxReplicas: 1", Case.Sensitive, module);
      Cpu().IsMatch(text).ShouldBeTrue($"{module} does not set cpu 0.25");
      text.ShouldContain("0.5Gi", Case.Sensitive, module);
    }

    sawApp.ShouldBeTrue("publish produced no container app");
    await Task.CompletedTask;
  }

  public static async Task Publish_Should_GiveWebServerThePooledStringAndLeaveTheDirectStringOffAzure()
  {
    string all = string.Join('\n', Modules.Values);
    ContainsParameter(all, "neon-direct-connection-string").ShouldBeFalse("publish output contains the direct Neon parameter");
    Modules.Keys.ShouldNotContain(path => path.Contains("web-migrations", StringComparison.Ordinal));
#if web
#if postgres
    string webServer = ModuleContaining("name: 'web-server'");
    webServer.ShouldContain("ConnectionStrings__postgres-db");
    webServer.ShouldContain("secretRef:");
    webServer.ShouldContain("Postgres__RetryCount");
    webServer.ShouldContain("'6'");
    webServer.ShouldContain("Postgres__RetryDelaySeconds");
    webServer.ShouldContain("'8'");
    ContainsParameter(webServer, "neon-pooled-connection-string").ShouldBeTrue("web-server does not read the pooled Neon parameter");
    ContainsParameter(webServer, "neon-direct-connection-string").ShouldBeFalse("web-server reads the direct Neon parameter");

    string allExceptWeb = string.Join('\n', Modules.Where(module => !module.Value.Contains("name: 'web-server'", StringComparison.Ordinal)).Select(module => module.Value));
    allExceptWeb.ShouldNotContain("Postgres__RetryCount");
#endif
#endif
    await Task.CompletedTask;
  }

  public static async Task Publish_Should_OnlyExposeTheIngressAndNoDashboard()
  {
    string all = string.Join('\n', Modules.Values);
    all.ShouldNotContain("AspireDashboard");
    all.ShouldNotContain("Authentication__UseMock");
    all.ShouldNotContain("TIMEWARP_STATE_DEVTOOLS");
    all.ShouldNotContain("signoz");

    bool hasIngress = Modules.Values.Any(text => text.Contains("name: 'ingress'", StringComparison.Ordinal));
    int external = ExternalTrue().Count(all);
    if (!hasIngress)
    {
      external.ShouldBe(0, "without the YARP ingress no container app may set external: true");
      await Task.CompletedTask;
      return;
    }

    external.ShouldBe(1, "only the ingress container app may set external: true");
    ModuleContaining("name: 'ingress'").ShouldContain("external: true");
    await Task.CompletedTask;
  }

  public static async Task Publish_Should_KeepSecretsAsSecureParameters()
  {
    foreach ((string module, string text) in Modules)
    {
      foreach (Match parameter in ParameterDeclaration().Matches(text))
      {
        string name = parameter.Groups["name"].Value;
        if (!name.Contains("neon", StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }

        parameter.Groups["secure"].Success.ShouldBeTrue($"{module} parameter '{name}' is not @secure()");
        parameter.Groups["default"].Success.ShouldBeFalse($"{module} parameter '{name}' ships a default");
      }
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_AllowTheIngressHostForPasskeys()
  {
#if web
#if yarp
    string webServer = ModuleContaining("name: 'web-server'");
    webServer.ShouldContain("WebAuthnOptions__AllowedRpIds__0");
    webServer.ShouldContain("ingress.${aca_env_outputs_azure_container_apps_environment_default_domain}");
    int includeAt = webServer.IndexOf("name: 'SignInOptions__IncludeMicrosoft365'", StringComparison.Ordinal);
    includeAt.ShouldBeGreaterThan(-1);
    int valueAt = webServer.IndexOf("value:", includeAt, StringComparison.Ordinal);
    valueAt.ShouldBeGreaterThan(includeAt);
    int lineEnd = webServer.IndexOf('\n', valueAt, StringComparison.Ordinal);
    string value = webServer[valueAt..lineEnd]["value:".Length..].Trim();
    value.StartsWith('\'', StringComparison.Ordinal).ShouldBeFalse(value);
    value.Contains("entra", StringComparison.OrdinalIgnoreCase).ShouldBeTrue(value);
    webServer.ShouldNotContain("SignInOptions__Methods__0");
    webServer.ShouldNotContain("SignInOptions__Methods__1");
#endif
#endif
    await Task.CompletedTask;
  }

  public static async Task RunMode_Should_CarryNoAzureWiring()
  {
    await using IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>(["--Publish:Target=aca-neon"]);

    appHost.Resources.ShouldNotContain(
      resource => resource.GetType().Namespace!.StartsWith("Aspire.Hosting.Azure", StringComparison.Ordinal),
      "run mode declared an Azure resource");
    if (appHost.Resources.SingleOrDefault(resource => resource.Name == "postgres") is { } postgres)
    {
      postgres.GetType().Name.ShouldBe("PostgresServerResource");
    }
  }

  private static async Task PublishInProcAsync(string outputDirectory)
  {
    await using IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>(
        ["--operation", "publish", "--step", "publish-azure-environment", "--output-path", outputDirectory, "--Publish:Target=aca-neon"],
        (_, settings) => settings.EnvironmentName = "Production");

    await using DistributedApplication app = await appHost.BuildAsync();
    await app.RunAsync();
  }

  private static string ModuleContaining(string needle)
  {
    KeyValuePair<string, string> module = Modules.Single(candidate => candidate.Value.Contains(needle, StringComparison.Ordinal));
    return module.Value;
  }

  private static bool ContainsParameter(string text, string parameterName)
  {
    string underscored = parameterName.Replace('-', '_');
    return text.Contains(parameterName, StringComparison.Ordinal)
      || text.Contains(underscored, StringComparison.Ordinal);
  }

  [GeneratedRegex(@"cpu:\s*(?:json\(\s*)?'?0\.25'?")]
  private static partial Regex Cpu();

  [GeneratedRegex(@"external:\s*true")]
  private static partial Regex ExternalTrue();

  [GeneratedRegex(@"(?<secure>@secure\(\)\s*)?^param\s+(?<name>\w+)\s+\w+(?<default>\s*=)?", RegexOptions.Multiline)]
  private static partial Regex ParameterDeclaration();
}
