#region Purpose
// Gates `dev open` / `dev deploy migrate` (task 287) option parsing.
#endregion

// ReSharper disable InconsistentNaming
namespace DeployOperate_;

public class Options_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Options_Given_>();

  public static Task ValidPort_Should_Parse()
  {
    DeployKubernetes.ParsePort("9090", "--port").ShouldBe((9090, null));
    DeployKubernetes.ParsePort(" 65535 ", "--port").ShouldBe((65535, null));
    return Task.CompletedTask;
  }

  public static Task ForwardPort_Should_PreferOptionThenIngressPortParameter()
  {
    ResolvedDeployParameter parameter = new("ingress-port", "36371", "AppHost appsettings.json");
    DeployKubernetes.ChooseForwardPort("9090", parameter, "app/app.csproj").ShouldBe((9090, "--port"));
    DeployKubernetes.ChooseForwardPort(null, parameter, "app/app.csproj").ShouldBe((36371, "ingress-port from the AppHost appsettings.json"));
    DeployKubernetes.ChooseForwardPort(" ", parameter, "app/app.csproj").Port.ShouldBe(36371);
    return Task.CompletedTask;
  }

  public static Task ForwardPort_Should_RefuseWithoutOptionOrParameter()
  {
    (int? port, string detail) = DeployKubernetes.ChooseForwardPort(null, null, "app/app.csproj");
    port.ShouldBeNull();
    detail.ShouldContain("ingress-port is not set");
    detail.ShouldContain("--port <n>");
    detail.ShouldContain("dotnet user-secrets set 'Parameters:ingress-port'");
    return Task.CompletedTask;
  }

  public static Task ForwardPort_Should_RefuseABadIngressPortParameter()
  {
    (int? port, string detail) = DeployKubernetes.ChooseForwardPort(null, new ResolvedDeployParameter("ingress-port", "http", "AppHost user secret"), "a.csproj");
    port.ShouldBeNull();
    detail.ShouldContain("ingress-port from the AppHost user secret 'http' is not a TCP port");
    return Task.CompletedTask;
  }

  public static Task InvalidPort_Should_BeRefused()
  {
    foreach (string value in (string[])["0", "65536", "-1", "http", "80.5"])
    {
      (int? port, string? error) = DeployKubernetes.ParsePort(value, "--port");
      port.ShouldBeNull(value);
      error.ShouldNotBeNull().ShouldContain($"--port '{value}' is not a TCP port");
    }

    return Task.CompletedTask;
  }

  public static Task Targets_Should_ResolveLikeDevDeploy()
  {
    AspireDeploy.ResolveTarget(null).ShouldBe(AspireDeploy.Compose);
    AspireDeploy.ResolveTarget("KUBERNETES").ShouldBe(AspireDeploy.Kubernetes);
    AspireDeploy.ResolveTarget("swarm").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task OpenAndMigrateScopes_Should_SkipTheToolsTheyDoNotRun()
  {
    PreflightScope.Open.ShouldBe(new PreflightScope(RequireParameters: false, ProbeRegistry: false, RequireAspire: false, RequireHelm: false));
    PreflightScope.Migrate.ShouldBe(new PreflightScope(RequireParameters: true, ProbeRegistry: false, RequireAspire: false, RequireHelm: false));
    PreflightScope.Deploy.ShouldBe(new PreflightScope(RequireParameters: true, ProbeRegistry: true, RequireAspire: true, RequireHelm: true));
    PreflightScope.Deprovision.ShouldBe(new PreflightScope(RequireParameters: false, ProbeRegistry: false, RequireAspire: true, RequireHelm: true));
    return Task.CompletedTask;
  }

  public static Task ResolveNamedParameters_Should_UseTheDeployPrecedence()
  {
    Func<string, string?> environment = AspireDeployParameters.CaseInsensitiveEnvironment(new Dictionary<string, string> { ["parameters__ingress-port"] = "9000" });
    DeployParameterResolution fromEnvironment = AspireDeployParameters.ResolveParameters(["ingress-port"], environment, false, "");
    fromEnvironment.Resolved.Single().ShouldBe(new ResolvedDeployParameter("ingress-port", "9000", "Parameters__ingress-port environment variable"));

    AppSettingsFile[] appSettings = [new("appsettings.json", """{ "Parameters": { "ingress-port": "36371" } }""")];
    DeployParameterResolution fromAppSettings = AspireDeployParameters.ResolveParameters(["ingress-port"], _ => null, true, "", appSettings);
    fromAppSettings.Resolved.Single().ShouldBe(new ResolvedDeployParameter("ingress-port", "36371", "AppHost appsettings.json"));
    AspireDeployParameters.ResolveParameters(["ingress-port"], environment, true, "", appSettings).Resolved.Single().Value.ShouldBe("9000");

    DeployParameterResolution fromSecret = AspireDeployParameters.ResolveParameters(["ingress-port"], _ => null, true, "Parameters:ingress-port = 9100");
    fromSecret.Resolved.Single().Source.ShouldBe(AspireDeployParameters.UserSecretSource);

    AspireDeployParameters.ResolveParameters(["ingress-port"], _ => null, true, "").Missing.ShouldBe(["ingress-port"]);
    return Task.CompletedTask;
  }
}
