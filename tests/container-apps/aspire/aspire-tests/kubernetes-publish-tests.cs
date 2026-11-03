#region Purpose
// Production-safety guard over the Kubernetes publish output (task 070-004): runs the AppHost's
// publish pipeline with Publish:Target=kubernetes and inspects the generated Helm chart.
// One partial class across files: secrets/storage facts in kubernetes-publish-secrets-tests.cs,
// publish and chart parsing in kubernetes-publish-chart-parsing.cs. One class keeps one publish per run.
#endregion

#region Design
// Same two-source shape as compose-publish-tests. By default SetupOnce runs the publish pipeline
// in-proc (step publish-k8s, Production environment) into a temp directory — no images, no cluster,
// no AppHost run. When TIMEWARP_HELM_OUTPUT names a directory, the facts inspect THAT chart instead:
// `dev publish kubernetes` runs the real `aspire publish` CLI and points this suite at its output, so
// CI checks the exact chart it uploads (and helm-lints). The in-proc publish runs only publish-k8s;
// Publish_Should_StayLoadableByHelm is meaningful on the CLI output, where the EF migration steps
// also write into the chart directory.
// The chart is read as Helm sees it before rendering: templates/**.yaml plus values.yaml. Quoted
// template expressions ("{{ .Values.x }}") are plain YAML strings; the few unquoted ones (integer
// ports, `{{ ... | int }}`) are not valid YAML, so they are swapped for a 0 before parsing. A value
// that is exactly "{{ .Values.<path> }}" is resolved against values.yaml, so a check reads the value an
// operator's `helm install` would apply with default values.
// Every rule is conditional on the services the template flags emitted: no ingress (yarp off) means
// NO cluster Ingress and no externally reachable Service; no postgres/web-server means their rules do
// not apply. Browser-log forwarding (task 261) and the dashboard REPL (task 262) are gated on the
// hosting environment, so "absent" means no workload is configured as Development/Testing and the
// chart carries no dashboard workload at all.
// State DevTools (task 335-001) is run-mode only: no timewarp-devtools resource and no
// TIMEWARP_STATE_DEVTOOLS_ENDPOINT anywhere in the output. Task 335-002: nor any inspector CORS
// variable (TIMEWARP_STATE_DEVTOOLS_CORS_ORIGINS, TimeWarp__State__DevTools__CorsOrigins).
// SigNoz (task 417) is run-mode only: the in-proc publish passes Observability:SigNoz:Url, and the
// chart still has no signoz workload. Without that argument the absence check would pass even if
// the run-mode gate were removed.
#endregion

namespace Aspire.Tests;

using YamlDotNet.RepresentationModel;

[TestTag("Integration")]
public partial class KubernetesPublish_Given_
{
  internal const string OutputEnvironmentVariable = "TIMEWARP_HELM_OUTPUT";
  private const string IngressComponent = "ingress";
  private const string PostgresComponent = "postgres";
  private const string WebServerComponent = "web-server";
  private const string SigNozUrlArgument = "--Observability:SigNoz:Url=https://observability.example";
  private const long HelmMaxFileBytes = 5 * 1024 * 1024;

  private static string OutputDirectory = null!;
  private static bool OwnsOutputDirectory;
  private static List<YamlMappingNode> Manifests = null!;
  private static YamlMappingNode Values = null!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<KubernetesPublish_Given_>();

  public static async Task SetupOnce()
  {
    string? provided = Environment.GetEnvironmentVariable(OutputEnvironmentVariable);
    if (string.IsNullOrWhiteSpace(provided))
    {
      OutputDirectory = Directory.CreateTempSubdirectory("helm-publish-").FullName;
      OwnsOutputDirectory = true;
      await PublishInProcAsync(OutputDirectory);
    }
    else
    {
      OutputDirectory = Path.GetFullPath(provided);
    }

    File.Exists(Path.Combine(OutputDirectory, "Chart.yaml")).ShouldBeTrue($"publish produced no Chart.yaml in {OutputDirectory}");
    Values = LoadDocuments(Path.Combine(OutputDirectory, "values.yaml")).Single();
    Manifests = [.. Directory.EnumerateFiles(Path.Combine(OutputDirectory, "templates"), "*.yaml", SearchOption.AllDirectories)
      .Order(StringComparer.Ordinal)
      .SelectMany(LoadDocuments)];
    Manifests.ShouldNotBeEmpty();
  }

  public static Task CleanUpOnce()
  {
    if (OwnsOutputDirectory && Directory.Exists(OutputDirectory))
    {
      Directory.Delete(OutputDirectory, recursive: true);
    }

    return Task.CompletedTask;
  }

  public static async Task Publish_Should_OnlyExposeTheIngressOutsideTheCluster()
  {
    // No Service is reachable from outside the cluster on its own: ClusterIP only, no host ports.
    foreach (YamlMappingNode service in OfKind("Service"))
    {
      (Scalar(Mapping(service, "spec"), "type") ?? "ClusterIP").ShouldBe("ClusterIP", $"Service '{Name(service)}' is externally reachable");
    }

    foreach (YamlMappingNode workload in Workloads())
    {
      foreach (YamlMappingNode container in Containers(workload))
      {
        foreach (YamlMappingNode port in Items(container, "ports"))
        {
          Scalar(port, "hostPort").ShouldBeNull($"workload '{Name(workload)}' binds a host port");
        }
      }
    }

    // The single way in is the cluster Ingress, and every backend it names is the YARP ingress
    // Service. Without the yarp flag there is no ingress at all.
    bool hasIngress = Workloads().Any(workload => Component(workload) == IngressComponent);
    List<YamlMappingNode> ingresses = OfKind("Ingress");
    OfKind("HTTPRoute").ShouldBeEmpty();
    if (!hasIngress)
    {
      ingresses.ShouldBeEmpty();
      await Task.CompletedTask;
      return;
    }

    string ingressService = Name(OfKind("Service").Single(service => Component(service) == IngressComponent));
    YamlMappingNode ingress = ingresses.ShouldHaveSingleItem();
    List<string> backends = [.. BackendServices(ingress)];
    backends.ShouldNotBeEmpty();
    backends.ShouldAllBe(backend => backend == ingressService);

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_StayLoadableByHelm()
  {
    // Helm loads EVERY file in the chart directory and rejects one over 5 MiB, so a stray large
    // artifact (the self-contained EF migration bundle) breaks helm lint and `aspire deploy` alike.
    // Asserted here too so a machine without helm still catches it.
    foreach (string file in Directory.EnumerateFiles(OutputDirectory, "*", SearchOption.AllDirectories))
    {
      new FileInfo(file).Length.ShouldBeLessThanOrEqualTo(HelmMaxFileBytes, $"{Path.GetRelativePath(OutputDirectory, file)} exceeds Helm's chart file limit");
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_HaveNoDashboard()
  {
    foreach (YamlMappingNode workload in Workloads())
    {
      Name(workload).ShouldNotContain("dashboard");
      foreach (YamlMappingNode container in Containers(workload))
      {
        Scalar(container, "image")?.ShouldNotContain("aspire-dashboard");
      }
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_HaveNoStateDevTools()
  {
    // Task 335-001: the TimeWarp.State DevTools inspector is a run-mode-only resource.
    // Literals: TimeWarp.State.DevTools.Hosting is absent with web off.
    foreach (YamlMappingNode manifest in Manifests)
    {
      Name(manifest).ShouldNotContain("timewarp-devtools");
    }

    foreach ((string owner, string key, string _) in EnvironmentEntries())
    {
      key.ShouldNotBe("TIMEWARP_STATE_DEVTOOLS_ENDPOINT", $"{owner} points at State DevTools");
      key.ShouldNotContain("DEVTOOLS", Case.Insensitive, $"{owner} carries a State DevTools variable");
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_HaveNoSigNoz()
  {
    // Task 417: PublishInProcAsync sets Observability:SigNoz:Url. The chart still has no signoz workload.
    foreach (YamlMappingNode manifest in Manifests)
    {
      Name(manifest).ShouldNotContain("signoz");
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_NeverEnableMockAuth()
  {
    foreach ((string owner, string key, string _) in EnvironmentEntries())
    {
      key.ShouldNotContain("UseMock", Case.Insensitive, $"{owner} carries a mock-auth setting");
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_NotRunAnyWorkloadAsDevelopmentOrTesting()
  {
    // Browser-log forwarding and the Postgres REPL are Development(/Testing)-only; a published
    // workload running as either environment would switch them back on.
    foreach ((string owner, string key, string value) in EnvironmentEntries())
    {
      if (key is "ASPNETCORE_ENVIRONMENT" or "DOTNET_ENVIRONMENT")
      {
        new[] { "Development", "Testing" }.ShouldNotContain(
          environment => string.Equals(environment, value, StringComparison.OrdinalIgnoreCase), $"{owner}:{key}={value}");
      }
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_NotLogBelowInformation()
  {
    // Task 358: a published workload never logs at Debug/Trace (EF Core SQL per query fills disks).
    foreach ((string owner, string key, string value) in EnvironmentEntries().Where(entry => entry.Key.StartsWith("Logging__LogLevel__", StringComparison.OrdinalIgnoreCase)))
    {
      new[] { "Debug", "Trace" }.ShouldNotContain(
        level => string.Equals(level, value, StringComparison.OrdinalIgnoreCase), $"{owner}:{key}={value}");
    }

    await Task.CompletedTask;
  }
}
