#region Purpose
// KubernetesPublish_Given_ facts over secrets and storage: nothing secret-shaped in a ConfigMap or
// inline env, Postgres as a StatefulSet on a persistent volume, Microsoft365 from a parameter, run mode
// carries no Kubernetes wiring, and an unknown target is rejected. Setup and the rule set: kubernetes-publish-tests.cs.
#endregion

namespace Aspire.Tests;

using YamlDotNet.RepresentationModel;

public partial class KubernetesPublish_Given_
{
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

  public static Task Publish_Should_AppendMicrosoft365FromAParameter()
  {
    if (!Workloads().Any(workload => Component(workload) == WebServerComponent))
    {
      return Task.CompletedTask;
    }

    string templates = string.Join(
      '\n',
      Directory.EnumerateFiles(Path.Combine(OutputDirectory, "templates"), "*.yaml", SearchOption.AllDirectories)
        .Select(File.ReadAllText));
    int at = templates.IndexOf("SignInOptions__IncludeMicrosoft365", StringComparison.Ordinal);
    at.ShouldBeGreaterThan(-1);
    string window = templates.Substring(at, Math.Min(400, templates.Length - at));
    window.ShouldContain(".Values");
    window.ShouldNotContain("Passkey");
    templates.ShouldNotContain("SignInOptions__Methods__0");
    templates.ShouldNotContain("SignInOptions__Methods__1");
    return Task.CompletedTask;
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
}
