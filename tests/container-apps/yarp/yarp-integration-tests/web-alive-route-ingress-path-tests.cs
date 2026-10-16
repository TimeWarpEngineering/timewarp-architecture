#region Purpose
// Build check that the yarp Development WebAliveRoute (path match and PathSet) agrees with
// IngressPaths.WebServerAlivePath and HealthEndpointPaths.Alive, the probe path the AppHost ingress
// health check and its publish-mode route use (task 377).
#endregion

#region Design
// Same shape as GrpcRouteIngressPath_: the route stays in configuration and JSON cannot reference a
// C# constant, so this test is the agreement check. It reads the yarp project's own
// appsettings.Development.json through the configuration JSON provider (comments allowed), located
// via ProjectContentRoot. That the route answers is IngressSmoke_Given_ (aspire-tests): the in-proc
// standalone smoke boots web-server without Main, so service defaults (and /alive) are absent there.
#endregion

namespace WebAliveRouteIngressPath_;

using Microsoft.Extensions.Configuration;
using TimeWarp.Foundation.Configuration;

[TestTag("Integration")]
public class DevelopmentConfig_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<DevelopmentConfig_>();

  public static Task WebAliveRoute_Should_Use_IngressPaths_WebServerAlivePath()
  {
    string yarpProjectDirectory = ProjectContentRoot.Resolve(typeof(TimeWarp.Architecture.Yarp.Server.Program).Assembly);
    string settingsPath = Path.Combine(yarpProjectDirectory, "appsettings.Development.json");
    File.Exists(settingsPath).ShouldBeTrue($"Missing {settingsPath}");

    IConfigurationRoot configuration = new ConfigurationBuilder()
      .AddJsonFile(settingsPath, optional: false, reloadOnChange: false)
      .Build();

    IConfigurationSection aliveRoute = configuration.GetSection("ReverseProxy:Routes:WebAliveRoute");
    aliveRoute.Exists().ShouldBeTrue("ReverseProxy:Routes:WebAliveRoute is missing from the yarp Development config.");

    aliveRoute["ClusterId"].ShouldBe("Web.Server");
    aliveRoute["Match:Path"].ShouldBe(IngressPaths.WebServerAlivePath);
    aliveRoute.GetSection("Transforms").GetChildren()
      .Select(transform => transform["PathSet"])
      .Where(value => value is not null)
      .ShouldBe([HealthEndpointPaths.Alive]);

    return Task.CompletedTask;
  }
}
