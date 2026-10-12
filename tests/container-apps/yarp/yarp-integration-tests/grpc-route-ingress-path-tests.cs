#region Purpose
// Build check that the yarp Development GrpcRoute (path match and PathRemovePrefix) agrees with
// IngressPaths.GrpcPathPrefix, the prefix the WASM gRPC client and the AppHost route use.
#endregion

#region Design
// The route stays in configuration (operators reshape routes without a rebuild — program.cs Design),
// and JSON cannot reference a C# constant, so this test is the agreement check (task 312). It reads
// the yarp project's own appsettings.Development.json through the same configuration JSON provider
// the host uses (comments allowed), located via ProjectContentRoot — the project's build-time
// directory, not this test's output folder.
#endregion

namespace GrpcRouteIngressPath_;

using Microsoft.Extensions.Configuration;
using TimeWarp.Foundation.Configuration;

[TestTag("Integration")]
public class DevelopmentConfig_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<DevelopmentConfig_>();

  public static Task GrpcRoute_Should_Use_IngressPaths_GrpcPathPrefix()
  {
    string yarpProjectDirectory = ProjectContentRoot.Resolve(typeof(TimeWarp.Architecture.Yarp.Server.Program).Assembly);
    string settingsPath = Path.Combine(yarpProjectDirectory, "appsettings.Development.json");
    File.Exists(settingsPath).ShouldBeTrue($"Missing {settingsPath}");

    IConfigurationRoot configuration = new ConfigurationBuilder()
      .AddJsonFile(settingsPath, optional: false, reloadOnChange: false)
      .Build();

    IConfigurationSection grpcRoute = configuration.GetSection("ReverseProxy:Routes:GrpcRoute");
    grpcRoute.Exists().ShouldBeTrue("ReverseProxy:Routes:GrpcRoute is missing from the yarp Development config.");

    grpcRoute["Match:Path"].ShouldBe($"{IngressPaths.GrpcPathPrefix}/{{**catch-all}}");
    grpcRoute.GetSection("Transforms").GetChildren()
      .Select(transform => transform["PathRemovePrefix"])
      .Where(value => value is not null)
      .ShouldBe([IngressPaths.GrpcPathPrefix]);

    return Task.CompletedTask;
  }
}
