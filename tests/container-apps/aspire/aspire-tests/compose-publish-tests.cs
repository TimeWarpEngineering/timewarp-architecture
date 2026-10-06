#region Purpose
// Production-safety guard over the Docker Compose publish output (task 070-003): runs the AppHost's
// publish pipeline and inspects the generated docker-compose.yaml and .env.
#endregion

#region Design
// Two sources, one rule set. By default SetupOnce runs the publish pipeline in-proc
// (DistributedApplicationTestingBuilder with the publish operation, step publish-compose) into a temp
// directory — no containers, no images, no AppHost run. It pins the Production environment, as the
// `aspire publish` CLI does by default (the testing builder would otherwise publish as Development). When TIMEWARP_COMPOSE_OUTPUT names a
// directory, the facts inspect THAT output instead: `dev publish compose` runs the real
// `aspire publish` CLI and points this suite at its artifact, so CI checks the exact files it uploads.
// Only the publish-compose step runs in-proc: the EF migration script/bundle steps need dotnet-ef and
// add nothing to compose.yaml (the CLI path still produces them).
// Every rule is conditional on the services the template flags emitted: no ingress (yarp off) means
// NO service may publish a host port; no postgres/web-server means their rules do not apply. The
// parsed compose model is plain YamlDotNet (a transitive Aspire.Hosting dependency), not Aspire's
// internal ComposeFile types, so the guard reads what an operator's `docker compose` reads.
// Browser-log forwarding (task 261) and the dashboard REPL (task 262) are gated on the hosting
// environment, so "absent" here means no service is configured as Development/Testing and no
// dashboard service exists at all. RunMode_Should_CarryNoPublishOnlyWiring is the other half of
// "publish-only": the run-mode model gains no publish parameters and keeps web-server external.
#endregion

namespace Aspire.Tests;

using YamlDotNet.RepresentationModel;

[TestTag("Integration")]
public class ComposePublish_Given_
{
  internal const string OutputEnvironmentVariable = "TIMEWARP_COMPOSE_OUTPUT";
  private const string IngressService = "ingress";
  private const string PostgresService = "postgres";
  private const string WebServerService = "web-server";

  private static string OutputDirectory = null!;
  private static bool OwnsOutputDirectory;
  private static YamlMappingNode Services = null!;
  private static YamlMappingNode? Volumes;
  private static Dictionary<string, string> EnvFile = null!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ComposePublish_Given_>();

  public static async Task SetupOnce()
  {
    string? provided = Environment.GetEnvironmentVariable(OutputEnvironmentVariable);
    if (string.IsNullOrWhiteSpace(provided))
    {
      OutputDirectory = Directory.CreateTempSubdirectory("compose-publish-").FullName;
      OwnsOutputDirectory = true;
      await PublishInProcAsync(OutputDirectory);
    }
    else
    {
      OutputDirectory = Path.GetFullPath(provided);
    }

    string composePath = Path.Combine(OutputDirectory, "docker-compose.yaml");
    File.Exists(composePath).ShouldBeTrue($"publish produced no {composePath}");

    var yaml = new YamlStream();
    using (var reader = new StreamReader(composePath))
    {
      yaml.Load(reader);
    }

    var root = (YamlMappingNode)yaml.Documents.Single().RootNode;
    Services = (YamlMappingNode)root.Children[new YamlScalarNode("services")];
    Volumes = root.Children.TryGetValue(new YamlScalarNode("volumes"), out YamlNode? volumes) ? (YamlMappingNode)volumes : null;
    EnvFile = ReadEnvFile(Path.Combine(OutputDirectory, ".env"));
  }

  public static Task CleanUpOnce()
  {
    if (OwnsOutputDirectory && Directory.Exists(OutputDirectory))
    {
      Directory.Delete(OutputDirectory, recursive: true);
    }

    return Task.CompletedTask;
  }

  public static async Task Publish_Should_OnlyExposeIngressToTheHost()
  {
    bool hasIngress = ServiceNames().Contains(IngressService);
    foreach (string service in ServiceNames())
    {
      List<string> ports = Sequence(Service(service), "ports");
      if (hasIngress && service == IngressService)
      {
        // The single host mapping is the ingress-port .env parameter, never a random host port.
        ports.ShouldHaveSingleItem().ShouldStartWith("${INGRESS_PORT}:");
        EnvFile.ShouldContainKey("INGRESS_PORT");
      }
      else
      {
        ports.ShouldBeEmpty($"service '{service}' publishes a host port; only the ingress may");
      }
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_HaveNoDashboard()
  {
    foreach (string service in ServiceNames())
    {
      service.ShouldNotContain("dashboard");
      Scalar(Service(service), "image")?.ShouldNotContain("aspire-dashboard");
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_NeverEnableMockAuth()
  {
    foreach (string service in ServiceNames())
    {
      ServiceEnvironment(service).Keys.ShouldNotContain(
        key => key.Contains("UseMock", StringComparison.OrdinalIgnoreCase),
        $"service '{service}' carries a mock-auth setting");
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_NotRunAnyServiceAsDevelopmentOrTesting()
  {
    // Browser-log forwarding and the Postgres REPL are Development(/Testing)-only; a published
    // service running as either environment would switch them back on.
    foreach (string service in ServiceNames())
    {
      Dictionary<string, string> environment = ServiceEnvironment(service);
      foreach (string key in new[] { "ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT" })
      {
        if (environment.TryGetValue(key, out string? value))
        {
          new[] { "Development", "Testing" }.ShouldNotContain(
            environment => string.Equals(environment, value, StringComparison.OrdinalIgnoreCase), $"{service}:{key}={value}");
        }
      }
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_KeepSecretsAsEnvParameters()
  {
    foreach (string service in ServiceNames())
    {
      foreach ((string key, string value) in ServiceEnvironment(service))
      {
        if (key.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase) || key.Contains("SECRET", StringComparison.OrdinalIgnoreCase))
        {
          value.ShouldMatch(@"^\$\{[A-Z0-9_]+\}$", $"{service}:{key} is a literal, not an .env parameter");
        }

        if (value.Contains("Password=", StringComparison.OrdinalIgnoreCase))
        {
          value.ShouldContain("Password=${", Case.Insensitive, $"{service}:{key} embeds a literal password");
        }
      }
    }

    if (ServiceNames().Contains(PostgresService))
    {
      ServiceEnvironment(PostgresService)["POSTGRES_PASSWORD"].ShouldBe("${POSTGRES_PASSWORD}");
      EnvFile.ShouldContainKey("POSTGRES_PASSWORD");
    }

    if (ServiceNames().Contains(WebServerService))
    {
      ServiceEnvironment(WebServerService)["Authentication__Entra__ClientSecret"].ShouldBe("${ENTRA_CLIENT_SECRET}");
      EnvFile.ShouldContainKey("ENTRA_CLIENT_SECRET");
    }

    // aspire publish leaves .env unfilled: a value on a secret key would ship a literal secret.
    foreach ((string key, string value) in EnvFile)
    {
      if (key.Contains("PASSWORD", StringComparison.Ordinal) || key.Contains("SECRET", StringComparison.Ordinal))
      {
        value.ShouldBeEmpty($".env {key} carries a value");
      }
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_PersistPostgresInANamedVolume()
  {
    if (!ServiceNames().Contains(PostgresService))
    {
      return;
    }

    YamlMappingNode volume = ((YamlSequenceNode)Service(PostgresService).Children[new YamlScalarNode("volumes")])
      .Children.Cast<YamlMappingNode>().ShouldHaveSingleItem();
    Scalar(volume, "type").ShouldBe("volume");
    Scalar(volume, "target").ShouldBe("/var/lib/postgresql");
    Scalar(volume, "source").ShouldBe("postgres-data");
    Volumes.ShouldNotBeNull().Children.Keys.Select(key => ((YamlScalarNode)key).Value).ShouldContain("postgres-data");
    ServiceEnvironment(PostgresService)["POSTGRES_DB"].ShouldBe("postgres-db");

    await Task.CompletedTask;
  }

  public static async Task RunMode_Should_CarryNoPublishOnlyWiring()
  {
    // Publish-only: the dev loop's model keeps its external web-server endpoint and gains none of
    // the publish parameters (which `dev run` would otherwise prompt for).
    await using IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>([]);

    string[] publishOnlyParameters =
      ["ingress-port", "entra-enabled", "entra-tenant-id", "entra-client-id", "entra-client-secret", "entra-public-origin"];
    appHost.Resources.OfType<ParameterResource>().Select(parameter => parameter.Name)
      .ShouldNotContain(name => publishOnlyParameters.Contains(name));

    IResource? webServer = appHost.Resources.SingleOrDefault(resource => resource.Name == WebServerService);
    if (webServer is not null)
    {
      webServer.Annotations.OfType<EndpointAnnotation>().Where(endpoint => endpoint.UriScheme.StartsWith("http", StringComparison.Ordinal))
        .ShouldAllBe(endpoint => endpoint.IsExternal);
    }
  }

  private static async Task PublishInProcAsync(string outputDirectory)
  {
    await using IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>(
        ["--operation", "publish", "--step", "publish-compose", "--output-path", outputDirectory],
        // The testing builder defaults to Development; `aspire publish` defaults to Production.
        (_, settings) => settings.EnvironmentName = "Production");

    await using DistributedApplication app = await appHost.BuildAsync();
    await app.RunAsync();
  }

  private static Dictionary<string, string> ReadEnvFile(string path)
  {
    File.Exists(path).ShouldBeTrue($"publish produced no {path}");
    return File.ReadAllLines(path)
      .Where(line => line.Length > 0 && !line.StartsWith('#'))
      .Select(line => line.Split('=', 2))
      .ToDictionary(parts => parts[0], parts => parts.Length > 1 ? parts[1] : "");
  }

  private static List<string> ServiceNames() =>
    [.. Services.Children.Keys.Select(key => ((YamlScalarNode)key).Value!)];

  private static YamlMappingNode Service(string name) =>
    (YamlMappingNode)Services.Children[new YamlScalarNode(name)];

  private static Dictionary<string, string> ServiceEnvironment(string service) =>
    Service(service).Children.TryGetValue(new YamlScalarNode("environment"), out YamlNode? node)
      ? ((YamlMappingNode)node).Children.ToDictionary(pair => ((YamlScalarNode)pair.Key).Value!, pair => ((YamlScalarNode)pair.Value).Value ?? "")
      : [];

  private static List<string> Sequence(YamlMappingNode node, string key) =>
    node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value)
      ? [.. ((YamlSequenceNode)value).Children.Select(item => ((YamlScalarNode)item).Value ?? "")]
      : [];

  private static string? Scalar(YamlMappingNode node, string key) =>
    node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value) ? ((YamlScalarNode)value).Value : null;
}
