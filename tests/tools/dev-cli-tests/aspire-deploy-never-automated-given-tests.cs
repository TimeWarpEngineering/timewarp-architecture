#region Purpose
// Gates that no CI workflow or `dev workflow` mode ever invokes a deploy, `dev deploy migrate` or `dev open`.
#endregion

// ReSharper disable InconsistentNaming
namespace AspireDeploy_;

public partial class NeverAutomated_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<NeverAutomated_Given_>();

  // A dev deploy/deprovision/open verb — `dev deploy` also covers `dev deploy migrate` (task 287) — (bin/dev or the dev.cs
  // runfile) or a raw aspire deploy/destroy.
  [System.Text.RegularExpressions.GeneratedRegex(
    @"(\bdev(\.cs)?\s+(--\s+)?(deploy|deprovision|open)\b)|(\baspire\s+(deploy|destroy)\b)",
    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant)]
  private static partial System.Text.RegularExpressions.Regex DeployInvocation();

  public static Task Pattern_Should_MatchEveryDeployInvocationShape()
  {
    string[] invocations =
    [
      "run: dev deploy --target compose --yes",
      "run: ./bin/dev deprovision --yes",
      "dotnet run tools/dev-cli/dev.cs -- deploy",
      "aspire deploy --apphost app.csproj",
      "aspire destroy --non-interactive --yes",
      "run: dev deploy migrate --target kubernetes --yes",
      "run: ./bin/dev deploy migrate",
      "dotnet run tools/dev-cli/dev.cs -- deploy migrate --target aca --yes",
      "run: dev open --target kubernetes --no-browser",
    ];

    foreach (string invocation in invocations)
    {
      DeployInvocation().IsMatch(invocation).ShouldBeTrue(invocation);
    }

    DeployInvocation().IsMatch("run: dev publish --target compose").ShouldBeFalse();
    DeployInvocation().IsMatch("run: dev db update").ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task CiWorkflowsAndDevWorkflow_Should_NeverDeploy()
  {
    string root = RepoRoot();
    string github = Path.Combine(root, ".github");
    string[] files =
    [
      .. Directory.EnumerateFiles(github, "*.yml", SearchOption.AllDirectories),
      .. Directory.EnumerateFiles(github, "*.yaml", SearchOption.AllDirectories),
      Path.Combine(root, "tools", "dev-cli", "endpoints", "workflow-command.cs"),
    ];
    files.Length.ShouldBeGreaterThan(1);

    string[] offenders =
    [
      .. files.SelectMany(file => File.ReadLines(file)
        .Select((line, index) => (line, index))
        .Where(entry => DeployInvocation().IsMatch(entry.line))
        .Select(entry => $"{Path.GetRelativePath(root, file)}:{entry.index + 1}: {entry.line.Trim()}")),
    ];

    offenders.ShouldBeEmpty("Deploying and migrating a deployment are operator-run (`dev deploy`, `dev deploy migrate`), never a CI step or `dev workflow` mode.");
    return Task.CompletedTask;
  }

  private static string RepoRoot()
  {
    DirectoryInfo? directory = new(AppContext.BaseDirectory);
    while (directory is not null && !Path.Exists(Path.Combine(directory.FullName, ".git")))
    {
      directory = directory.Parent;
    }

    return directory.ShouldNotBeNull().FullName;
  }
}
