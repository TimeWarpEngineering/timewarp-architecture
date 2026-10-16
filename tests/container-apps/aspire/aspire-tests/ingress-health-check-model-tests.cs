#region Purpose
// AppHost model inspection (task 377): the ingress HTTP health check probes
// IngressPaths.WebServerAlivePath, never "/" (the prerendered SPA shell), in run and publish mode.
#endregion

#region Design
// Model-only, like state-dev-tools-model-tests: DistributedApplicationTestingBuilder.CreateAsync runs
// the AppHost entry point up to Build, so nothing starts. WithHttpHealthCheck records only a
// HealthCheckAnnotation whose key Aspire composes as "{resource}_{endpoint}_{path}_{status}_check",
// so the key is the one observable carrying the probe path. It emits no EndpointProbeAnnotation,
// so the Compose, Kubernetes and ACA outputs carry no ingress probe to assert; each publish target
// is checked here at the model instead. Flag-tolerant at runtime: with yarp off there is no
// ingress resource, and with web off the ingress carries no health check at all.
// That the route answers through the ingress is IngressSmoke_Given_ (closed-box boot).
#endregion

namespace Aspire.Tests;

using TimeWarp.Foundation.Configuration;

[TestTag("Integration")]
public class IngressHealthCheck_Given_
{
  private const string IngressResourceName = "ingress";
  private const string WebServerResourceName = "web-server";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<IngressHealthCheck_Given_>();

  public static async Task RunMode_Should_ProbeWebServerAlivePath()
  {
    await using IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>([]);

    AssertIngressProbe(appHost);
  }

  public static async Task ComposePublish_Should_ProbeWebServerAlivePath()
  {
    await using IDistributedApplicationTestingBuilder appHost = await CreatePublishAsync("publish-compose");

    AssertIngressProbe(appHost);
  }

  public static async Task KubernetesPublish_Should_ProbeWebServerAlivePath()
  {
    await using IDistributedApplicationTestingBuilder appHost = await CreatePublishAsync("publish-k8s", "--Publish:Target=kubernetes");

    AssertIngressProbe(appHost);
  }

  public static async Task AcaPublish_Should_ProbeWebServerAlivePath()
  {
    await using IDistributedApplicationTestingBuilder appHost = await CreatePublishAsync("publish-azure-environment", "--Publish:Target=aca");

    AssertIngressProbe(appHost);
  }

  private static Task<IDistributedApplicationTestingBuilder> CreatePublishAsync(string step, params string[] extraArgs) =>
    DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>(
      ["--operation", "publish", "--step", step, .. extraArgs],
      (_, settings) => settings.EnvironmentName = "Production");

  private static void AssertIngressProbe(IDistributedApplicationTestingBuilder appHost)
  {
    IResource? ingress = appHost.Resources.SingleOrDefault(resource => resource.Name == IngressResourceName);
    if (ingress is null)
    {
      // yarp off: no ingress, nothing probes it.
      return;
    }

    string[] healthCheckKeys = [.. ingress.Annotations.OfType<HealthCheckAnnotation>().Select(annotation => annotation.Key)];

    // "/" is the SPA catch-all: probing it prerenders the app and mints a State store per probe.
    healthCheckKeys.ShouldNotContain(key => key.Contains("_/_", StringComparison.Ordinal), "the ingress health check probes \"/\" again");

    if (appHost.Resources.Any(resource => resource.Name == WebServerResourceName))
    {
      healthCheckKeys.ShouldBe([$"{IngressResourceName}_http_{IngressPaths.WebServerAlivePath}_200_check"]);
    }
    else
    {
      // web off: no web-server route to probe, so no check is emitted.
      healthCheckKeys.ShouldBeEmpty();
    }
  }
}
