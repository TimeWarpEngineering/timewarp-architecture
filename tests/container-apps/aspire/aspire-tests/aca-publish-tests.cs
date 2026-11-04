#region Purpose
// Production-safety guard over the Azure Container Apps publish output (task 070-007): runs the
// AppHost's publish pipeline with Publish:Target=aca and inspects the generated Bicep.
// One partial class across files: secrets/data facts in aca-publish-secrets-tests.cs, publish and
// Bicep parsing in aca-publish-bicep-parsing.cs. One class keeps one publish per run.
#endregion

#region Design
// Same two-source shape as compose-publish-tests and kubernetes-publish-tests. By default SetupOnce
// runs the publish pipeline in-proc (step publish-azure-environment, Production environment) into a
// temp directory. Publishing Azure only WRITES Bicep — no images, no Azure credentials, no
// subscription, no AppHost run — so the suite runs on any CI runner. When TIMEWARP_ACA_OUTPUT names a
// directory, the facts inspect THAT output instead: `dev publish aca` runs the real `aspire publish`
// CLI and points this suite at its output, so CI checks the exact files it uploads.
// Bicep is read as text, not compiled: the bicep CLI is not guaranteed on a runner, and the shapes
// Aspire emits are regular (one `resource ... 'Microsoft.App/containerApps@…'` per compute module,
// `env: [ { name: '…' value|secretRef: … } ]`, `secrets: [ … ]`, `@secure() param …`). Arrays are
// sliced by bracket matching outside quoted strings.
// Rule set = the Compose/Kubernetes rules plus the two ACA-specific ones:
//   - only the YARP ingress container app has external ingress (none at all without the yarp flag);
//   - no Aspire dashboard: Aspire emits it as a managedEnvironments/dotNetComponents resource of
//     componentType AspireDashboard on a public URL unless WithDashboard(false);
//   - no mock auth; no app runs as Development/Testing (browser-log forwarding and the REPL are
//     Development-only);
//   - secrets: every secret-shaped env var reads a container-app secret (secretRef), every secret is a
//     Key Vault reference or a @secure() parameter (never a literal), and every secret-shaped
//     parameter is @secure() with no default;
//   - Postgres is an Azure Database for PostgreSQL Flexible Server with password auth whose
//     connection string web-server reads from Key Vault — never a postgres container app, and no
//     Azure Files share (managedEnvironments/storages) anywhere;
//   - the postgres-derived secrets are PINNED: web-server carries exactly the Key Vault-backed
//     connection string plus the two copies Aspire's WithReference emits from the @secure() password
//     parameter (postgres-db-password, postgres-db-uri), and no other app carries any. A new copy of
//     the password is then a deliberate edit here, not a silent widening;
//   - the Flexible Server firewall is PINNED to Aspire's single AllowAllAzureIps rule (0.0.0.0–0.0.0.0:
//     any Azure-hosted IP, any tenant — the password is the barrier). Any other or wider rule in the
//     published Bicep fails — including one declared in a loop or nested (a raw occurrence count of
//     firewallRules / startIpAddress backs up the parsed declaration); the operator's migration rule is created and deleted by hand, never in code.
// Ingress: each container app's `ingress: { … }` block is sliced and its `external` field read; an
// ingress block without `external` fails rather than defaulting to internal.
// Every rule is conditional on what the template flags emitted (no web/postgres → their rules skip).
// The Flexible Server pin compiles only with web and postgres (postgres is declared inside the web
// block). The passkey-host fact compiles only with web and yarp, which is where that environment is set.
// State DevTools (task 335-001) is run-mode only: no timewarp-devtools resource and no
// TIMEWARP_STATE_DEVTOOLS_ENDPOINT anywhere in the output. Task 335-002: nor any inspector CORS
// variable (TIMEWARP_STATE_DEVTOOLS_CORS_ORIGINS, TimeWarp__State__DevTools__CorsOrigins).
// SigNoz (task 417) is run-mode only: the in-proc publish passes Observability:SigNoz:Url, and the
// Bicep still does not name signoz. Without that argument the absence check would pass even if
// the run-mode gate were removed.
#endregion

namespace Aspire.Tests;

[TestTag("Integration")]
public partial class AcaPublish_Given_
{
  internal const string OutputEnvironmentVariable = "TIMEWARP_ACA_OUTPUT";
  private const string IngressApp = "ingress";
  private const string WebServerApp = "web-server";
  private const string SigNozUrlArgument = "--Observability:SigNoz:Url=https://observability.example";
  private const string PostgresModule = "postgres";

  private static string OutputDirectory = null!;
  private static bool OwnsOutputDirectory;
  private static Dictionary<string, string> Modules = null!;
  private static List<ContainerApp> ContainerApps = null!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AcaPublish_Given_>();

  public static async Task SetupOnce()
  {
    string? provided = Environment.GetEnvironmentVariable(OutputEnvironmentVariable);
    if (string.IsNullOrWhiteSpace(provided))
    {
      OutputDirectory = Directory.CreateTempSubdirectory("aca-publish-").FullName;
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
    ContainerApps = [.. Modules.SelectMany(module => ParseContainerApps(module.Key, module.Value))];
    ContainerApps.ShouldNotBeEmpty();
  }

  public static Task CleanUpOnce()
  {
    if (OwnsOutputDirectory && Directory.Exists(OutputDirectory))
    {
      Directory.Delete(OutputDirectory, recursive: true);
    }

    return Task.CompletedTask;
  }

  public static async Task Publish_Should_OnlyExposeTheIngressOutsideTheEnvironment()
  {
    bool hasIngress = ContainerApps.Any(app => app.Name == IngressApp);
    foreach (ContainerApp app in ContainerApps)
    {
      bool expected = hasIngress && app.Name == IngressApp;
      if (app.Ingress is { } ingress)
      {
        string external = Field(ingress, "external").ShouldNotBeNull($"container app '{app.Name}' has an ingress block without an `external` field");
        external.ShouldBeOneOf(["true", "false"], $"container app '{app.Name}' ingress external is '{external}', not a literal");
        (external == "true").ShouldBe(expected, $"container app '{app.Name}' external ingress is {external}; only the YARP ingress may be external");
      }
      else
      {
        expected.ShouldBeFalse($"container app '{app.Name}' has no ingress block; the YARP ingress must be external");
      }

      app.Text.ShouldNotContain("additionalPortMappings", Case.Sensitive, $"container app '{app.Name}' opens extra ports");
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_HaveNoDashboard()
  {
    foreach ((string module, string text) in Modules)
    {
      text.ShouldNotContain("dotNetComponents", Case.Insensitive, $"{module} declares a dotNetComponents resource (the Aspire dashboard)");
      text.ShouldNotContain("AspireDashboard", Case.Insensitive, $"{module} declares the Aspire dashboard");
    }

    ContainerApps.ShouldNotContain(app => app.Name.Contains("dashboard", StringComparison.OrdinalIgnoreCase));

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_HaveNoStateDevTools()
  {
    // Task 335-001: the TimeWarp.State DevTools inspector is a run-mode-only resource.
    // Literals: TimeWarp.State.DevTools.Hosting is absent with web off.
    foreach ((string module, string text) in Modules)
    {
      text.ShouldNotContain("timewarp-devtools", Case.Insensitive, $"{module} declares State DevTools");
      text.ShouldNotContain("TIMEWARP_STATE_DEVTOOLS_ENDPOINT", Case.Insensitive, $"{module} points at State DevTools");
      text.ShouldNotContain("DEVTOOLS_CORS_ORIGINS", Case.Insensitive, $"{module} carries the State DevTools CORS list");
      text.ShouldNotContain("DevTools__CorsOrigins", Case.Insensitive, $"{module} carries the State DevTools CORS list");
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_HaveNoSigNoz()
  {
    // Task 417: PublishInProcAsync sets Observability:SigNoz:Url. The Bicep still does not name signoz.
    foreach ((string module, string text) in Modules)
    {
      text.ShouldNotContain("signoz", Case.Insensitive, $"{module} declares SigNoz");
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_NeverEnableMockAuth()
  {
    foreach ((string module, string text) in Modules)
    {
      text.ShouldNotContain("UseMock", Case.Insensitive, $"{module} carries a mock-auth setting");
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_NotRunAnyAppAsDevelopmentOrTesting()
  {
    // Browser-log forwarding and the Postgres REPL are Development(/Testing)-only; a published app
    // running as either environment would switch them back on.
    foreach (ContainerApp app in ContainerApps)
    {
      foreach (EnvironmentEntry entry in app.Environment.Where(entry => entry.Name is "ASPNETCORE_ENVIRONMENT" or "DOTNET_ENVIRONMENT"))
      {
        new[] { "'Development'", "'Testing'" }.ShouldNotContain(
          environment => string.Equals(environment, entry.Expression, StringComparison.OrdinalIgnoreCase), $"{app.Name}:{entry.Name}={entry.Expression}");
      }
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_NotLogBelowInformation()
  {
    // Task 358: a published app never logs at Debug/Trace (EF Core SQL per query fills disks).
    foreach (ContainerApp app in ContainerApps)
    {
      foreach (EnvironmentEntry entry in app.Environment.Where(entry => entry.Name.StartsWith("Logging__LogLevel__", StringComparison.OrdinalIgnoreCase)))
      {
        new[] { "'Debug'", "'Trace'" }.ShouldNotContain(
          level => string.Equals(level, entry.Expression, StringComparison.OrdinalIgnoreCase), $"{app.Name}:{entry.Name}={entry.Expression}");
      }
    }

    await Task.CompletedTask;
  }
}
