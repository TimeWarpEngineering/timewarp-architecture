#region Purpose
// Gates deploy-parameter resolution (env var, user secret, appsettings), agreement with the AppHost's value-less parameters and its committed appsettings.json Parameters, and the aca subscription source selection.
#endregion

// ReSharper disable InconsistentNaming
namespace AspireDeploy_;

public partial class DeployParameters_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<DeployParameters_Given_>();

  private const string AppHost = "/repo/app-host.csproj";

  private static string? NoEnvironment(string _) => null;

  public static Task Kubernetes_Should_RequireTheValuelessAppHostParameters()
  {
    AspireDeployParameters.RequiredParameters(AspireDeploy.Kubernetes)
      .ShouldBe(["k8s-namespace", "helm-release-name", "registry-endpoint", "registry-repository"]);
    AspireDeployParameters.RequiredParameters(AspireDeploy.Compose).ShouldBeEmpty();
    AspireDeployParameters.RequiredParameters(AspireDeploy.ContainerApps).ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task UserSecrets_Should_ResolveEveryParameter()
  {
    const string output =
      "Parameters:postgres-password = secret\nParameters:k8s-namespace = timewarp-architecture\nParameters:helm-release-name = twa\n"
      + "parameters:registry-endpoint = localhost:5001\nParameters:registry-repository = timewarp-architecture\n";
    DeployParameterResolution resolution = AspireDeployParameters.ResolveParameters(AspireDeploy.Kubernetes, NoEnvironment, true, output);

    resolution.Missing.ShouldBeEmpty();
    resolution.Resolved.Select(parameter => $"{parameter.Name}={parameter.Value}").ShouldBe(
      ["k8s-namespace=timewarp-architecture", "helm-release-name=twa", "registry-endpoint=localhost:5001", "registry-repository=timewarp-architecture"]);
    resolution.Resolved.ShouldAllBe(parameter => parameter.Source == "AppHost user secret");
    return Task.CompletedTask;
  }

  public static Task EnvironmentVariable_Should_BeatTheUserSecret()
  {
    DeployParameterResolution resolution = AspireDeployParameters.ResolveParameters(
      AspireDeploy.Kubernetes,
      name => name == "Parameters__k8s-namespace" ? " from-env " : null,
      true,
      "Parameters:k8s-namespace = from-secret\n");

    ResolvedDeployParameter ns = resolution.Resolved.Single();
    ns.Value.ShouldBe("from-env");
    ns.Source.ShouldContain("Parameters__k8s-namespace environment variable");
    resolution.Missing.ShouldBe(["helm-release-name", "registry-endpoint", "registry-repository"]);
    return Task.CompletedTask;
  }

  public static Task NothingSet_Should_ListEveryMissingParameter()
  {
    AspireDeployParameters.ResolveParameters(AspireDeploy.Kubernetes, NoEnvironment, false, "").Missing
      .ShouldBe(["k8s-namespace", "helm-release-name", "registry-endpoint", "registry-repository"]);
    AspireDeployParameters.ResolveParameters(AspireDeploy.Kubernetes, _ => "  ", true, "Parameters:k8s-namespace = \n").Missing.Count.ShouldBe(4);
    AspireDeployParameters.ResolveParameters(AspireDeploy.Compose, NoEnvironment, false, "").Resolved.ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task MissingParameterRefusal_Should_GiveThePwshCommandForEach()
  {
    string text = string.Join('\n', AspireDeployParameters.BuildMissingParameterLines(AppHost, AspireDeploy.Kubernetes, ["k8s-namespace", "registry-endpoint"]));

    text.ShouldContain("Missing deploy parameters for kubernetes: k8s-namespace, registry-endpoint.");
    text.ShouldContain("Parameters section of the AppHost appsettings.json");
    text.ShouldContain("dotnet user-secrets set 'Parameters:k8s-namespace' '<value>' --project '/repo/app-host.csproj'");
    text.ShouldContain("dotnet user-secrets set 'Parameters:registry-endpoint' '<value>' --project '/repo/app-host.csproj'");
    text.ShouldContain("${env:Parameters__k8s-namespace} = '<value>'");
    text.ShouldNotContain("export ");
    text.ShouldNotContain("helm-release-name");
    return Task.CompletedTask;
  }

  public static Task EnvironmentLookup_Should_IgnoreCaseLikeConfiguration()
  {
    Func<string, string?> environment = AspireDeployParameters.CaseInsensitiveEnvironment(
      new Dictionary<string, string> { ["PARAMETERS__K8S-NAMESPACE"] = "upper", ["Parameters__helm-release-name"] = "exact", ["parameters__helm-release-name"] = "lower" });

    environment("Parameters__k8s-namespace").ShouldBe("upper");
    environment("Parameters__helm-release-name").ShouldBe("exact");
    environment("Parameters__registry-endpoint").ShouldBeNull();
    AspireDeployParameters.ResolveParameters(AspireDeploy.Kubernetes, environment, false, "").Missing.ShouldBe(["registry-endpoint", "registry-repository"]);
    return Task.CompletedTask;
  }

  public static Task UnreadableUserSecrets_Should_BeReportedBeforeTheMissingList()
  {
    DeployParameterResolution nothing = AspireDeployParameters.ResolveParameters(AspireDeploy.Kubernetes, NoEnvironment, false, "");
    string[] problems = AspireDeployParameters.CollectParameterProblems(AppHost, AspireDeploy.Kubernetes, nothing, "MSB4025: The project file could not be loaded.");

    problems.Length.ShouldBe(2);
    problems[0].ShouldContain("Could not read the AppHost user secrets");
    problems[0].ShouldContain("MSB4025: The project file could not be loaded.");
    problems[1].ShouldContain("Missing deploy parameters for kubernetes");
    AspireDeployParameters.CollectParameterProblems(AppHost, AspireDeploy.Kubernetes, nothing, null).Single().ShouldContain("Missing deploy parameters");
    AspireDeployParameters.CollectParameterProblems(AppHost, AspireDeploy.Kubernetes, new([], []), "failed").ShouldBeEmpty();
    AspireDeployCluster.FirstLine("\n  first \nsecond", "fallback").ShouldBe("first");
    AspireDeployCluster.FirstLine(" \n", "fallback").ShouldBe("fallback");
    return Task.CompletedTask;
  }

  public static Task KubernetesRequiredParameters_Should_MatchTheAppHostsValuelessParameters()
  {
    string appHostDirectory = AppHostDirectory();
    // The AppHost model spans its concern files (program.cs, publish-target.cs, postgres-resources.cs, ...).
    string program = AppHostSource(appHostDirectory);
    string publishTarget = File.ReadAllText(Path.Combine(appHostDirectory, "publish-target.cs"));
    Dictionary<string, string> constants = ConstantPattern()
      .Matches(File.ReadAllText(Path.Combine(appHostDirectory, "constants.cs")))
      .ToDictionary(match => match.Groups[1].Value, match => match.Groups[2].Value);
    string[] Valueless(string text) =>
      [.. ValuelessParameterPattern().Matches(text).Select(match => constants[match.Groups[1].Value])];

    int branchStart = publishTarget.IndexOf("if (string.Equals(publishTarget, KubernetesPublishTarget", StringComparison.Ordinal);
    int branchEnd = publishTarget.IndexOf("else if (string.Equals(publishTarget, ContainerAppsPublishTarget", StringComparison.Ordinal);
    branchStart.ShouldBeGreaterThan(0);
    branchEnd.ShouldBeGreaterThan(branchStart);

    // Every AddParameter names a constants.cs identifier first, so the value-less scan below cannot miss a literal or named-argument form.
    NonIdentifierParameterPattern().Matches(program).ShouldBeEmpty();

    // A value-less parameter in the kubernetes branch is required and forwarded on argv. The aca-neon
    // connection strings are value-less secrets outside that branch. dev deploy copies them into the
    // aspire process environment and does not put them on argv.
    Valueless(publishTarget[branchStart..branchEnd]).ShouldBe(AspireDeployParameters.RequiredParameters(AspireDeploy.Kubernetes), ignoreOrder: true);
    Valueless(program).ShouldBe(
      [.. AspireDeployParameters.RequiredParameters(AspireDeploy.Kubernetes), .. AspireDeploySecrets.SecretParameters(AspireDeploy.ContainerAppsNeon)],
      ignoreOrder: true);
    return Task.CompletedTask;
  }

  [System.Text.RegularExpressions.GeneratedRegex(@"const\s+string\s+(\w+)\s*=\s*""([^""]*)""")]
  private static partial System.Text.RegularExpressions.Regex ConstantPattern();

  // Value-less: AddParameter(Name) or AddParameter(Name, secret: …) — no value argument.
  [System.Text.RegularExpressions.GeneratedRegex(@"AddParameter\(\s*(\w+)\s*(?:\)|,\s*secret\s*:)")]
  private static partial System.Text.RegularExpressions.Regex ValuelessParameterPattern();

  [System.Text.RegularExpressions.GeneratedRegex(@"AddParameter\(\s*(?!\w+\s*[,)])")]
  private static partial System.Text.RegularExpressions.Regex NonIdentifierParameterPattern();

  private static string AppHostSource(string appHostDirectory) =>
    string.Concat(
      Directory.EnumerateFiles(appHostDirectory, "*.cs", SearchOption.TopDirectoryOnly)
        .Order(StringComparer.Ordinal)
        .Select(File.ReadAllText));

  private static string AppHostDirectory()
  {
    for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
    {
      string candidate = Path.Combine(directory.FullName, "source", "container-apps", "aspire", "projects", "aspire-app-host");
      if (File.Exists(Path.Combine(candidate, "program.cs")))
      {
        return candidate;
      }
    }

    throw new DirectoryNotFoundException("aspire-app-host not found above the test output directory.");
  }

  private static AppSettingsFile AppSettings(string fileName, string parameters) =>
    new(fileName, $"{{ \"Logging\": {{}}, \"Parameters\": {{ {parameters} }} }}");

  public static Task AppSettings_Should_ResolveWhatNoSecretOrVariableSets()
  {
    AppSettingsFile appSettings = AppSettings(
      "appsettings.json",
      "\"k8s-namespace\": \"my-app\", \"helm-release-name\": \"my-app\", \"registry-endpoint\": \"localhost:5001\", \"registry-repository\": \"my-app\"");
    DeployParameterResolution resolution = AspireDeployParameters.ResolveParameters(AspireDeploy.Kubernetes, NoEnvironment, true, "", [appSettings]);

    resolution.Missing.ShouldBeEmpty();
    resolution.Resolved.Select(parameter => $"{parameter.Name}={parameter.Value}").ShouldBe(
      ["k8s-namespace=my-app", "helm-release-name=my-app", "registry-endpoint=localhost:5001", "registry-repository=my-app"]);
    resolution.Resolved.ShouldAllBe(parameter => parameter.Source == "AppHost appsettings.json");
    string.Join('\n', AspireDeployParameters.BuildParameterLines(resolution.Resolved))
      .ShouldContain("Parameter:   k8s-namespace=my-app (from the AppHost appsettings.json)");
    return Task.CompletedTask;
  }

  public static Task NeonSecrets_Should_ComeFromEnvOrUserSecretsAndNeverAppSettings()
  {
    const string committed = "Host=committed;Password=s3cret-value";
    AppSettingsFile appSettings = AppSettings("appsettings.json", $"\"neon-pooled-connection-string\": \"{committed}\"");
    SecretResolution resolution = AspireDeploySecrets.ResolveSecrets(
      AspireDeploySecrets.SecretParameters(AspireDeploy.ContainerAppsNeon),
      name => name == "Parameters__neon-pooled-connection-string" ? "from-env" : null,
      true,
      "Parameters:neon-direct-connection-string = from-secret\n",
      [appSettings]);

    resolution.CommittedToAppSettings.ShouldBe([AspireDeploySecrets.NeonPooledConnectionStringParameter]);
    resolution.Missing.ShouldBeEmpty();
    resolution.Resolved.Select(parameter => $"{parameter.Name}={parameter.Value}").ShouldBe(
    [
      "neon-pooled-connection-string=from-env",
      "neon-direct-connection-string=from-secret",
    ]);
    resolution.Resolved.ShouldNotContain(parameter => parameter.Value == committed);

    string problems = string.Join('\n', AspireDeploySecrets.CollectSecretProblems(AppHost, AspireDeploy.ContainerAppsNeon, resolution, null));
    problems.ShouldContain("never be committed");
    problems.ShouldNotContain("s3cret-value");
    problems.ShouldNotContain("from-env");
    problems.ShouldNotContain("from-secret");

    SecretResolution missing = AspireDeploySecrets.ResolveSecrets(
      AspireDeploySecrets.SecretParameters(AspireDeploy.ContainerAppsNeon), NoEnvironment, true, "");
    string missingText = string.Join('\n', AspireDeploySecrets.CollectSecretProblems(AppHost, AspireDeploy.ContainerAppsNeon, missing, null));
    missingText.ShouldContain("Missing Neon connection strings for aca-neon");
    missingText.ShouldContain("dotnet user-secrets set 'Parameters:neon-pooled-connection-string'");
    missingText.ShouldContain("dotnet user-secrets set 'Parameters:neon-direct-connection-string'");
    missingText.ShouldContain("Do not commit them in appsettings.json.");
    missingText.ShouldNotContain("s3cret-value");

    string plan = string.Join('\n', AspireDeployText.BuildDeployPlanLines(
      AppHost, AspireDeploy.ContainerAppsNeon, "subscription: example", [], resolution.Resolved));
    plan.ShouldContain("Publish:Target=aca-neon");
    plan.ShouldNotContain("from-env");
    plan.ShouldNotContain("from-secret");
    plan.ShouldNotContain("neon-pooled");
    plan.ShouldNotContain("neon-direct");

    Dictionary<string, string> environment = [];
    AspireDeployEnvironment.AddSecretParameterEnvironment(environment, resolution.Resolved, _ => null);
    environment.Select(pair => pair.Key).ShouldBe(
    [
      "Parameters__neon-pooled-connection-string",
      "Parameters__neon-direct-connection-string",
    ]);
    environment["Parameters__neon-pooled-connection-string"].ShouldBe("from-env");
    environment["Parameters__neon-direct-connection-string"].ShouldBe("from-secret");
    return Task.CompletedTask;
  }

  public static Task ResolutionOrder_Should_BeVariableThenSecretThenEnvironmentFileThenAppSettings()
  {
    AppSettingsFile production = AppSettings("appsettings.Production.json", "\"helm-release-name\": \"from-production\", \"registry-endpoint\": \"from-production\"");
    AppSettingsFile appSettings = AppSettings(
      "appsettings.json",
      "\"k8s-namespace\": \"from-appsettings\", \"helm-release-name\": \"from-appsettings\", \"registry-endpoint\": \"from-appsettings\", \"registry-repository\": \"from-appsettings\"");
    DeployParameterResolution resolution = AspireDeployParameters.ResolveParameters(
      AspireDeploy.Kubernetes,
      name => name == "Parameters__k8s-namespace" ? "from-env" : null,
      true,
      "Parameters:k8s-namespace = from-secret\nParameters:helm-release-name = from-secret\n",
      [production, appSettings]);

    resolution.Missing.ShouldBeEmpty();
    resolution.Resolved.Select(parameter => $"{parameter.Name}={parameter.Value} ({parameter.Source})").ShouldBe(
    [
      "k8s-namespace=from-env (Parameters__k8s-namespace environment variable)",
      "helm-release-name=from-secret (AppHost user secret)",
      "registry-endpoint=from-production (AppHost appsettings.Production.json)",
      "registry-repository=from-appsettings (AppHost appsettings.json)",
    ]);
    AspireDeployParameters.AppSettingsFileNames().ShouldBe(["appsettings.Production.json", "appsettings.json"]);
    return Task.CompletedTask;
  }

  public static Task AppSettingsParameter_Should_ReadLikeJsonConfiguration()
  {
    AspireDeployParameters.ParseAppSettingsParameter("{ \"parameters\": { \"K8S-NAMESPACE\": \" my-app \" } }", "k8s-namespace").ShouldBe("my-app");
    AspireDeployParameters.ParseAppSettingsParameter("{ // comment\n \"Parameters\": { \"k8s-namespace\": \"a\", \"k8s-namespace\": \"b\", } }", "k8s-namespace").ShouldBe("b");
    AspireDeployParameters.ParseAppSettingsParameter("{ \"Parameters\": { \"k8s-namespace\": \" \" } }", "k8s-namespace").ShouldBeNull();
    AspireDeployParameters.ParseAppSettingsParameter("{ \"Parameters\": { \"other\": \"x\" } }", "k8s-namespace").ShouldBeNull();
    AspireDeployParameters.ParseAppSettingsParameter("{ \"Logging\": {} }", "k8s-namespace").ShouldBeNull();
    AspireDeployParameters.ParseAppSettingsParameter("{ not json", "k8s-namespace").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task BlankAppSettingsValue_Should_StillBeReportedMissing()
  {
    AppSettingsFile appSettings = AppSettings("appsettings.json", "\"registry-endpoint\": \"\"");
    AspireDeployParameters.ResolveParameters(AspireDeploy.Kubernetes, NoEnvironment, true, "", [appSettings]).Missing
      .ShouldBe(["k8s-namespace", "helm-release-name", "registry-endpoint", "registry-repository"]);
    return Task.CompletedTask;
  }

  public static Task AppHostAppSettings_Should_CommitEveryKubernetesRequiredParameter()
  {
    // Task 288: none of the kubernetes parameters is a secret; the AppHost appsettings.json commits them all, so a fresh
    // checkout's `dev deploy kubernetes` reports nothing missing (registry-endpoint defaults to the kind recipe's registry).
    string appHostDirectory = AppHostDirectory();
    AppSettingsFile appSettings = new("appsettings.json", File.ReadAllText(Path.Combine(appHostDirectory, "appsettings.json")));
    DeployParameterResolution resolution = AspireDeployParameters.ResolveParameters(AspireDeploy.Kubernetes, NoEnvironment, false, "", [appSettings]);

    resolution.Missing.ShouldBeEmpty();
    resolution.Resolved.Single(parameter => parameter.Name == "registry-endpoint").Value.ShouldBe("localhost:5001");
    resolution.Resolved.Where(parameter => parameter.Name != "registry-endpoint")
      .ShouldAllBe(parameter => parameter.Value == "timewarp-architecture");
    return Task.CompletedTask;
  }

  public static Task PreflightReport_Should_CollectEveryProblemOnce()
  {
    string[] report = AspireDeployText.BuildPreflightReport("dev deploy", ["first", "second"]);
    report.ShouldBe(["dev deploy preflight failed (2 problems); nothing was run:", "- first", "- second"]);
    return Task.CompletedTask;
  }
}
