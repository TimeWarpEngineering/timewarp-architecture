#region Purpose
// Gates agreement with the AppHost's resource names and its committed ingress-port parameter.
#endregion

// ReSharper disable InconsistentNaming
namespace DeployOperate_;

public partial class AppHostAgreement_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AppHostAgreement_Given_>();

  public static Task ResourceNames_Should_EqualTheAppHostConstants()
  {
    string appHost = AppHostDirectory();
    Dictionary<string, string> constants = ConstantPattern()
      .Matches(File.ReadAllText(Path.Combine(appHost, "constants.cs")))
      .ToDictionary(match => match.Groups[1].Value, match => match.Groups[2].Value);

    constants["YarpResourceName"].ShouldBe(DeployOperate.IngressResourceName);
    constants["PostgresResourceName"].ShouldBe(DeployOperate.PostgresResourceName);
    constants["PostgresDatabaseResourceName"].ShouldBe(DeployOperate.PostgresDatabaseName);
    constants["IngressPortParameterName"].ShouldBe(DeployOperate.IngressPortParameter);
    constants["PasskeyRpHostParameterName"].ShouldBe(AspireDeployParameters.PasskeyRpHostParameter);
    DeployOperate.PostgresStatefulSet.ShouldBe($"statefulset/{constants["PostgresResourceName"]}-statefulset");
    return Task.CompletedTask;
  }

  public static Task IngressPort_Should_BeTheCommittedParameterNotACodeLiteral()
  {
    string appHost = AppHostDirectory();
    string? committed = AspireDeployParameters.ParseAppSettingsParameter(
      File.ReadAllText(Path.Combine(appHost, AspireDeployParameters.AppSettingsFileName)), DeployOperate.IngressPortParameter);
    committed.ShouldNotBeNull();
    DeployKubernetes.ParsePort(committed, "Parameters:ingress-port").Error.ShouldBeNull();

    // The AppHost declares the parameter from configuration (the value appsettings.json commits), never a literal port.
    string program = AppHostSource(appHost);
    program.ShouldContain("builder.Configuration[$\"Parameters:{IngressPortParameterName}\"]");
    IngressPortLiteralPattern().IsMatch(program).ShouldBeFalse("the AppHost passes a literal to AddParameter(IngressPortParameterName, …)");
    program.ShouldContain("WebAuthnOptions__AllowedRpIds__");
    program.ShouldContain("GetOutput(");
    program.ShouldContain("AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN");
    program.ShouldNotContain("ContainerAppDomain");
    program.ShouldContain("PasskeyRpHostParameterName");
    return Task.CompletedTask;
  }

  public static Task SourceMigrations_Should_BeTheFolderTheAppHostTellsEfToWriteTo()
  {
    string appHost = AppHostDirectory();
    string repoRoot = Path.GetFullPath(Path.Combine(appHost, "..", "..", "..", "..", ".."));
    string[] folders = DeployStaleScript.FindSourceMigrationFolders(Path.Combine(appHost, "aspire-app-host.csproj"), repoRoot);

    // postgres-resources.cs: WithMigrationsProject(web-infrastructure) + WithMigrationOutputDirectory("../../platform/postgres/migrations").
    string program = AppHostSource(appHost);
    program.ShouldContain(".WithMigrationOutputDirectory(\"../../platform/postgres/migrations\")");
    folders.ShouldBe([Path.GetFullPath(Path.Combine(appHost, "..", "..", "..", "web", DeployStaleScript.SourceMigrationsSubpath))]);
    DeployStaleScript.ParseSourceMigrationIds(Directory.EnumerateFiles(folders[0])).ShouldNotBeEmpty();
    return Task.CompletedTask;
  }

  [System.Text.RegularExpressions.GeneratedRegex(@"AddParameter\(\s*IngressPortParameterName\s*,\s*""")]
  private static partial System.Text.RegularExpressions.Regex IngressPortLiteralPattern();

  [System.Text.RegularExpressions.GeneratedRegex(@"const\s+string\s+(\w+)\s*=\s*""([^""]*)""")]
  private static partial System.Text.RegularExpressions.Regex ConstantPattern();

  // The AppHost model spans its concern files (program.cs, ingress-resource.cs, postgres-resources.cs, ...).
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
      if (File.Exists(Path.Combine(candidate, "constants.cs")))
      {
        return candidate;
      }
    }

    throw new DirectoryNotFoundException("aspire-app-host not found above the test output directory.");
  }
}
