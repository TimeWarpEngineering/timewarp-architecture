#region Purpose
// Production-safety guard over the Kubernetes publish output (task 070-004): runs the AppHost's
// publish pipeline with Publish:Target=kubernetes and inspects the generated Helm chart.
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

  public static async Task Publish_Should_KeepSecretsInSecretObjects()
  {
    // Nothing secret-shaped is a ConfigMap entry or an inline container env value.
    foreach ((string owner, string key, string value) in EnvironmentEntries().Where(entry => !entry.Owner.StartsWith("Secret/", StringComparison.Ordinal)))
    {
      IsSecretShaped(key, value).ShouldBeFalse($"{owner}:{key} is not in a Secret");
    }

    // values.yaml: everything secret-shaped lives under `secrets:` with an empty default (the
    // operator supplies it at deploy), never under `config:` or `parameters:`.
    foreach (string section in new[] { "config", "parameters" })
    {
      foreach ((string path, string value) in Leaves(OptionalMapping(Values, section), section))
      {
        IsSecretShaped(path, value).ShouldBeFalse($"values.yaml {path} is not under secrets");
      }
    }

    foreach ((string path, string value) in Leaves(OptionalMapping(Values, "secrets"), "secrets"))
    {
      value.ShouldBeEmpty($"values.yaml {path} ships a literal secret");
    }

    // Workloads read their Secret through envFrom.secretRef — the Secret named secrets.<resource>.
    if (Workloads().Any(workload => Component(workload) == PostgresComponent))
    {
      SecretData(PostgresComponent)["POSTGRES_PASSWORD"].ShouldBe("{{ .Values.secrets.postgres.postgres_password }}");
    }

    if (Workloads().Any(workload => Component(workload) == WebServerComponent))
    {
      SecretData(WebServerComponent)["Authentication__Entra__ClientSecret"].ShouldBe("{{ .Values.secrets.web_server.entra_client_secret }}");
    }

    await Task.CompletedTask;
  }

  public static async Task Publish_Should_RunPostgresAsAStatefulSetOnAPersistentVolume()
  {
    if (!Workloads().Any(workload => Component(workload) == PostgresComponent))
    {
      return;
    }

    OfKind("Deployment").ShouldNotContain(deployment => Component(deployment) == PostgresComponent);
    YamlMappingNode statefulSet = OfKind("StatefulSet").Single(workload => Component(workload) == PostgresComponent);
    YamlMappingNode podSpec = Mapping(Mapping(Mapping(statefulSet, "spec"), "template"), "spec");

    YamlMappingNode mount = Items(Containers(statefulSet).Single(), "volumeMounts").ShouldHaveSingleItem();
    Scalar(mount, "mountPath").ShouldBe("/var/lib/postgresql");
    YamlMappingNode volume = Items(podSpec, "volumes").Single(item => Scalar(item, "name") == Scalar(mount, "name"));
    string claimName = Scalar(Mapping(volume, "persistentVolumeClaim"), "claimName").ShouldNotBeNull();
    claimName.ShouldBe("postgres-data");
    OfKind("PersistentVolumeClaim").Select(Name).ShouldContain(claimName);

    // One writer: the claim is ReadWriteOnce and the StatefulSet never scales out.
    Scalar(Mapping(statefulSet, "spec"), "replicas").ShouldBe("1");
    Resolve(ConfigData(PostgresComponent)["POSTGRES_DB"]).ShouldBe("postgres-db");

    await Task.CompletedTask;
  }

  public static async Task RunMode_Should_CarryNoKubernetesWiring()
  {
    // Publish-only: the dev loop's model has no Kubernetes environment and none of its parameters,
    // even when the configuration asks for the kubernetes target.
    await using IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>(["--Publish:Target=kubernetes"]);

    appHost.Resources.ShouldNotContain(resource => resource is IComputeEnvironmentResource && resource.Name == "k8s");
    string[] kubernetesParameters =
      ["k8s-namespace", "helm-release-name", "helm-chart-version", "ingress-class", "registry-endpoint", "registry-repository", "postgres-storage-capacity"];
    appHost.Resources.OfType<ParameterResource>().Select(parameter => parameter.Name)
      .ShouldNotContain(name => kubernetesParameters.Contains(name));
  }

  public static async Task Publish_Should_RejectAnUnknownTarget()
  {
    InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
    {
      await using IDistributedApplicationTestingBuilder appHost =
        await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>(
          ["--operation", "publish", "--Publish:Target=swarm"],
          (_, settings) => settings.EnvironmentName = "Production");
    });
    exception.Message.ShouldContain("Publish:Target");
  }

  private static async Task PublishInProcAsync(string outputDirectory)
  {
    await using IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>(
        ["--operation", "publish", "--step", "publish-k8s", "--output-path", outputDirectory, "--Publish:Target=kubernetes"],
        // The testing builder defaults to Development; `aspire publish` defaults to Production.
        (_, settings) => settings.EnvironmentName = "Production");

    await using DistributedApplication app = await appHost.BuildAsync();
    await app.RunAsync();
  }

  private static List<YamlMappingNode> LoadDocuments(string path)
  {
    var yaml = new YamlStream();
    yaml.Load(new StringReader(UnquotedTemplateExpression().Replace(File.ReadAllText(path), "0")));
    return [.. yaml.Documents.Select(document => document.RootNode).OfType<YamlMappingNode>()];
  }

  // Every container env source the chart defines: ConfigMap data, Secret stringData/data, and inline
  // container env values. Owner is "<Kind>/<name>"; values are resolved against values.yaml.
  private static IEnumerable<(string Owner, string Key, string Value)> EnvironmentEntries()
  {
    foreach (YamlMappingNode manifest in Manifests)
    {
      string kind = Scalar(manifest, "kind") ?? "";
      string owner = $"{kind}/{Name(manifest)}";
      if (kind is "ConfigMap" or "Secret")
      {
        foreach (string section in new[] { "data", "stringData" })
        {
          if (manifest.Children.TryGetValue(new YamlScalarNode(section), out YamlNode? data))
          {
            foreach ((YamlNode key, YamlNode value) in ((YamlMappingNode)data).Children)
            {
              yield return (owner, ((YamlScalarNode)key).Value!, Resolve(((YamlScalarNode)value).Value ?? ""));
            }
          }
        }
      }
    }

    foreach (YamlMappingNode workload in Workloads())
    {
      foreach (YamlMappingNode container in Containers(workload))
      {
        foreach (YamlMappingNode variable in Items(container, "env"))
        {
          yield return ($"{Scalar(workload, "kind")}/{Name(workload)}", Scalar(variable, "name") ?? "", Resolve(Scalar(variable, "value") ?? ""));
        }
      }
    }
  }

  private static bool IsSecretShaped(string key, string value) =>
    key.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase)
    || key.Contains("SECRET", StringComparison.OrdinalIgnoreCase)
    || key.StartsWith("ConnectionStrings", StringComparison.OrdinalIgnoreCase)
    || key.EndsWith("_URI", StringComparison.OrdinalIgnoreCase)
    || value.Contains("Password=", StringComparison.OrdinalIgnoreCase)
    || UriCredential().IsMatch(value);

  private static Dictionary<string, string> ConfigData(string component) => Data("ConfigMap", component, "data");

  private static Dictionary<string, string> SecretData(string component) => Data("Secret", component, "stringData");

  private static Dictionary<string, string> Data(string kind, string component, string section) =>
    Mapping(OfKind(kind).Single(manifest => Component(manifest) == component), section).Children
      .ToDictionary(pair => ((YamlScalarNode)pair.Key).Value!, pair => ((YamlScalarNode)pair.Value).Value ?? "");

  // "{{ .Values.a.b }}" → values.yaml a.b (missing path → the expression itself); anything else as-is.
  private static string Resolve(string value)
  {
    Match match = ValuesReference().Match(value);
    if (!match.Success)
    {
      return value;
    }

    YamlNode node = Values;
    foreach (string segment in match.Groups[1].Value.Split('.'))
    {
      if (node is not YamlMappingNode mapping || !mapping.Children.TryGetValue(new YamlScalarNode(segment), out YamlNode? child))
      {
        return value;
      }

      node = child;
    }

    return node is YamlScalarNode scalar ? scalar.Value ?? "" : value;
  }

  private static IEnumerable<(string Path, string Value)> Leaves(YamlMappingNode? node, string path)
  {
    if (node is null)
    {
      yield break;
    }

    foreach ((YamlNode key, YamlNode value) in node.Children)
    {
      string childPath = $"{path}.{((YamlScalarNode)key).Value}";
      if (value is YamlMappingNode mapping)
      {
        foreach ((string Path, string Value) leaf in Leaves(mapping, childPath))
        {
          yield return leaf;
        }
      }
      else if (value is YamlScalarNode scalar)
      {
        yield return (childPath, scalar.Value ?? "");
      }
    }
  }

  private static IEnumerable<string> BackendServices(YamlMappingNode ingress)
  {
    YamlMappingNode spec = Mapping(ingress, "spec");
    if (spec.Children.TryGetValue(new YamlScalarNode("defaultBackend"), out YamlNode? defaultBackend))
    {
      yield return Scalar(Mapping((YamlMappingNode)defaultBackend, "service"), "name")!;
    }

    foreach (YamlMappingNode rule in Items(spec, "rules"))
    {
      if (rule.Children.TryGetValue(new YamlScalarNode("http"), out YamlNode? http))
      {
        foreach (YamlMappingNode path in Items((YamlMappingNode)http, "paths"))
        {
          yield return Scalar(Mapping(Mapping(path, "backend"), "service"), "name")!;
        }
      }
    }
  }

  private static List<YamlMappingNode> OfKind(string kind) =>
    [.. Manifests.Where(manifest => Scalar(manifest, "kind") == kind)];

  private static List<YamlMappingNode> Workloads() =>
    [.. Manifests.Where(manifest => Scalar(manifest, "kind") is "Deployment" or "StatefulSet" or "DaemonSet" or "Job" or "CronJob" or "Pod")];

  private static IEnumerable<YamlMappingNode> Containers(YamlMappingNode workload)
  {
    YamlMappingNode spec = Mapping(workload, "spec");
    YamlMappingNode podSpec = Scalar(workload, "kind") == "Pod" ? spec : Mapping(Mapping(spec, "template"), "spec");
    return Items(podSpec, "containers").Concat(Items(podSpec, "initContainers"));
  }

  private static string Name(YamlMappingNode manifest) => Scalar(Mapping(manifest, "metadata"), "name") ?? "";

  private static string? Component(YamlMappingNode manifest) =>
    Mapping(manifest, "metadata").Children.TryGetValue(new YamlScalarNode("labels"), out YamlNode? labels)
      ? Scalar((YamlMappingNode)labels, "app.kubernetes.io/component")
      : null;

  private static YamlMappingNode Mapping(YamlMappingNode node, string key) =>
    (YamlMappingNode)node.Children[new YamlScalarNode(key)];

  // A values.yaml section a flag combination may not emit (no secret parameters → no `secrets:`).
  private static YamlMappingNode? OptionalMapping(YamlMappingNode node, string key) =>
    node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value) ? value as YamlMappingNode : null;

  private static IEnumerable<YamlMappingNode> Items(YamlMappingNode node, string key) =>
    node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value)
      ? ((YamlSequenceNode)value).Children.Cast<YamlMappingNode>()
      : [];

  private static string? Scalar(YamlMappingNode node, string key) =>
    node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value) ? ((YamlScalarNode)value).Value : null;

  // A whole unquoted scalar that is a Helm expression (e.g. `containerPort: {{ .Values.x | int }}`) —
  // invalid YAML until rendered. Expressions inside quoted strings are left alone.
  [GeneratedRegex(@"(?<=(:|-)[ \t]+)\{\{[^}]*\}\}(?=[ \t]*$)", RegexOptions.Multiline)]
  private static partial Regex UnquotedTemplateExpression();

  [GeneratedRegex(@"^\{\{\s*\.Values\.([A-Za-z0-9_.\-]+)\s*\}\}$")]
  private static partial Regex ValuesReference();

  // scheme://user:password@host — a literal (non-template) password.
  [GeneratedRegex(@"://[^/:@\s]+:(?!\{\{)[^@\s]+@")]
  private static partial Regex UriCredential();
}
