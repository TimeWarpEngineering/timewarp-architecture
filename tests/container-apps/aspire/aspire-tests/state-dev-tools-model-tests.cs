#region Purpose
// AppHost model inspection (task 335-001): run mode adds the TimeWarp.State DevTools inspector and
// points web-server at it; a publish model carries neither.
#endregion

#region Design
// Model-only, like postgres-volume-model-tests: DistributedApplicationTestingBuilder.CreateAsync runs
// the AppHost entry point up to Build, so nothing starts. web-server's environment callbacks run
// against a run-mode context; the TIMEWARP_STATE_DEVTOOLS_ENDPOINT value is the unresolved
// EndpointReference, so the fact checks it names the inspector's http endpoint. Names are literals,
// not TimeWarp.State.DevTools.Hosting constants: that package is absent with the web flag off, where
// the facts assert there is no inspector at all. The publish-output half lives in the compose,
// kubernetes and aca suites (Publish_Should_HaveNoStateDevTools).
#endregion

namespace Aspire.Tests;

[TestTag("Integration")]
public class StateDevToolsModel_Given_
{
  private const string DevToolsResourceName = "timewarp-state-devtools";
  private const string DevToolsEndpointName = "http";
  private const string EndpointVariable = "TIMEWARP_STATE_DEVTOOLS_ENDPOINT";
  private const string WebServerResourceName = "web-server";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<StateDevToolsModel_Given_>();

  public static async Task RunMode_Should_AddDevToolsAndPointWebServerAtIt()
  {
    await using IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>([]);

    IResource? webServer = appHost.Resources.SingleOrDefault(resource => resource.Name == WebServerResourceName);
    if (webServer is null)
    {
      // web off: no TimeWarp.State app to inspect.
      appHost.Resources.ShouldNotContain(resource => resource.Name == DevToolsResourceName);
      return;
    }

    ExecutableResource devTools = appHost.Resources.OfType<ExecutableResource>()
      .Single(resource => resource.Name == DevToolsResourceName);
    devTools.Annotations.OfType<EndpointAnnotation>().ShouldContain(endpoint => endpoint.Name == DevToolsEndpointName);

    Dictionary<string, object> environment = await RunModeEnvironmentAsync(webServer);
    EndpointReference endpoint = environment[EndpointVariable].ShouldBeOfType<EndpointReference>();
    endpoint.Resource.ShouldBeSameAs(devTools);
    endpoint.EndpointName.ShouldBe(DevToolsEndpointName);

    webServer.Annotations.OfType<WaitAnnotation>().ShouldContain(wait => wait.Resource == devTools);
  }

  public static async Task PublishMode_Should_HaveNoDevTools()
  {
    await using IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>(
        ["--operation", "publish", "--step", "publish-compose"],
        (_, settings) => settings.EnvironmentName = "Production");

    appHost.Resources.ShouldNotContain(resource => resource.Name == DevToolsResourceName);

    IResource? webServer = appHost.Resources.SingleOrDefault(resource => resource.Name == WebServerResourceName);
    webServer?.Annotations.OfType<WaitAnnotation>().ShouldNotContain(wait => wait.Resource.Name == DevToolsResourceName);
  }

  private static async Task<Dictionary<string, object>> RunModeEnvironmentAsync(IResource resource)
  {
    var context = new EnvironmentCallbackContext(
      new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run), resource);
    foreach (EnvironmentCallbackAnnotation callback in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
    {
      await callback.Callback(context);
    }

    return context.EnvironmentVariables.ToDictionary(pair => pair.Key, pair => pair.Value);
  }
}
