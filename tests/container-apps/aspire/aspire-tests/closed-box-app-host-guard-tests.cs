#region Purpose
// Fail if a closed-box suite boots the AppHost in run mode without ClosedBoxAppHost, or if the
// helper's arguments leave the ingress on the `dev run` port pins.
#endregion

#region Design
// Task 319: a test AppHost that binds Ingress:Port 63610 / Ingress:HttpPort 63620 collides with
// the maintainer's running `dev run`, and its requests can reach that live instance. Every
// run-mode boot goes through tests/common/closed-box-app-host/closed-box-app-host.cs, which clears
// both pins. The source scan flags a test file that calls
// DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host> directly AND starts
// the app (StartAsync). Publish-mode boots run the pipeline with RunAsync and bind no ports;
// model-only boots never start. The model facts build (never start) the app with the helper's
// arguments (and, for the `dev run` pins, the default arguments) and read the ingress endpoint
// annotations, so no port is bound by this suite. This file holds the pattern and is skipped.
// The ingress model facts compile only with the yarp flag: with --yarp false there is no ingress
// resource (task 374).
#endregion

namespace ClosedBoxAppHostGuard_;

[TestTag("Unit")]
public partial class ClosedBoxAppHost_Should_
{
  private const string DirectBoot = "DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>";
#if yarp
  private static readonly int[] DevRunIngressPorts = [63610, 63620];
#endif

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ClosedBoxAppHost_Should_>();

  public static Task Be_The_Only_RunMode_Boot()
  {
    string testsDirectory = FindTestsDirectory();
    string helper = Path.Combine(testsDirectory, "common", "closed-box-app-host", "closed-box-app-host.cs");
    string self = Path.Combine(testsDirectory, "container-apps", "aspire", "aspire-tests", "closed-box-app-host-guard-tests.cs");
    File.Exists(helper).ShouldBeTrue(helper);

    List<string> offenders = [];
    foreach (string file in Directory.EnumerateFiles(testsDirectory, "*.cs", SearchOption.AllDirectories))
    {
      string relative = Path.GetRelativePath(testsDirectory, file);
      string fullPath = Path.GetFullPath(file);
      if (IsBuildOutput(relative)
        || string.Equals(fullPath, Path.GetFullPath(helper), StringComparison.Ordinal)
        || string.Equals(fullPath, Path.GetFullPath(self), StringComparison.Ordinal))
      {
        continue;
      }

      string text = File.ReadAllText(file);
      if (text.Contains(DirectBoot, StringComparison.Ordinal) && text.Contains(".StartAsync(", StringComparison.Ordinal))
      {
        offenders.Add(relative);
      }
    }

    offenders.ShouldBeEmpty(
      "Run-mode closed-box AppHost boots must use ClosedBoxAppHost.CreateAsync (dynamic ingress ports). Offenders: "
      + string.Join(", ", offenders));
    return Task.CompletedTask;
  }

  public static Task Args_Should_Clear_Ingress_Pins_And_Append_Extras()
  {
    string[] args = ClosedBoxAppHost.Args("--Authentication:UseMock=true");

    args.ShouldContain("--Postgres:UseDataVolume=false");
    args.ShouldContain("--Ingress:Port=");
    args.ShouldContain("--Ingress:HttpPort=");
    args[^1].ShouldBe("--Authentication:UseMock=true");
    return Task.CompletedTask;
  }
#if yarp

  public static async Task Ingress_Endpoints_Should_Not_Use_DevRun_Ports()
  {
    await using IDistributedApplicationTestingBuilder appHost = await ClosedBoxAppHost.CreateAsync();

    IResource ingress = appHost.Resources.Single(resource => resource.Name == "ingress");
    List<EndpointAnnotation> endpoints = [.. ingress.Annotations.OfType<EndpointAnnotation>()];
    endpoints.ShouldNotBeEmpty();
    foreach (EndpointAnnotation endpoint in endpoints)
    {
      DevRunIngressPorts.ShouldNotContain(endpoint.Port ?? 0, $"ingress endpoint '{endpoint.Name}' pinned to {endpoint.Port}");
    }
  }

  public static async Task Default_Config_Should_Keep_DevRun_Pins()
  {
    await using IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>([]);

    IResource ingress = appHost.Resources.Single(resource => resource.Name == "ingress");
    Dictionary<string, EndpointAnnotation> endpoints =
      ingress.Annotations.OfType<EndpointAnnotation>().ToDictionary(endpoint => endpoint.Name);
    endpoints["https"].Port.ShouldBe(63610);
    endpoints["http"].Port.ShouldBe(63620);
    endpoints["http"].IsProxied.ShouldBeFalse();
  }
#endif

  private static bool IsBuildOutput(string relativePath)
  {
    string[] segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    return segments.Contains("bin") || segments.Contains("obj");
  }

  private static string FindTestsDirectory()
  {
    DirectoryInfo? dir = new(AppContext.BaseDirectory);
    while (dir is not null)
    {
      string tests = Path.Combine(dir.FullName, "tests");
      if (File.Exists(Path.Combine(dir.FullName, "source", "Directory.Build.props")) && Directory.Exists(tests))
      {
        return tests;
      }

      dir = dir.Parent;
    }

    throw new InvalidOperationException("Could not locate the tests directory from " + AppContext.BaseDirectory);
  }
}
