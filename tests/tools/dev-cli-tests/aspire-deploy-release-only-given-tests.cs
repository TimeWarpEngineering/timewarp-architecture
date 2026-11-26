#region Purpose
// Gates that only the generated app's release-gated deploy job runs a deploy: no other workflow job, no
// template-repository workflow, and no `dev workflow` mode deploys, migrates a deployment, deprovisions, or opens.
#endregion

// ReSharper disable InconsistentNaming
namespace AspireDeploy_;

public class ReleaseOnly_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ReleaseOnly_Given_>();

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
      AppCiWorkflowShape.DeployCommandLine,
    ];

    foreach (string invocation in invocations)
    {
      AppCiWorkflowShape.DeployInvocation().IsMatch(invocation).ShouldBeTrue(invocation);
    }

    AppCiWorkflowShape.DeployInvocation().IsMatch("run: dev publish --target compose").ShouldBeFalse();
    AppCiWorkflowShape.DeployInvocation().IsMatch("run: dev db update").ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Workflows_Should_DeployOnlyFromTheAppReleaseDeployJob()
  {
    string root = RepoRoot();
    string github = Path.Combine(root, ".github");
    string appWorkflow = Path.GetFullPath(Path.Combine(root, AppCiWorkflowShape.SourceRelativePath));
    string[] files =
    [
      .. Directory.EnumerateFiles(github, "*.yml", SearchOption.AllDirectories),
      .. Directory.EnumerateFiles(github, "*.yaml", SearchOption.AllDirectories),
      Path.Combine(root, "tools", "dev-cli", "endpoints", "workflow-command.cs"),
    ];
    files.ShouldContain(appWorkflow);

    string[] offenders =
    [
      .. files.SelectMany(file =>
      {
        string text = File.ReadAllText(file);
        IEnumerable<string> lines = Path.GetFullPath(file) == appWorkflow
          ? AppCiWorkflowShape.OutsideJob(text, AppCiWorkflowShape.DeployJob)
          : text.Split('\n');
        return lines
          .Where(line => !IsComment(line))
          .Where(line => AppCiWorkflowShape.DeployInvocation().IsMatch(line))
          .Select(line => $"{Path.GetRelativePath(root, file)}: {line.Trim()}");
      }),
    ];

    offenders.ShouldBeEmpty("A merge never deploys. Only the app workflow's deploy job (a published release or a dispatch with deploy on) runs `dev deploy`.");
    return Task.CompletedTask;
  }

  public static Task AppDeployJob_Should_RunOneDevDeploy()
  {
    string yaml = File.ReadAllText(Path.Combine(RepoRoot(), AppCiWorkflowShape.SourceRelativePath));
    List<string> errors = [];
    HashSet<string> paths = AppCiWorkflowShape.ParseKeyPaths(yaml, errors);

    errors.ShouldBeEmpty();
    paths.ShouldContain($"jobs/{AppCiWorkflowShape.DeployJob}");
    AppCiWorkflowShape.DeployJobErrors(yaml, paths).ShouldBeEmpty();
    return Task.CompletedTask;
  }

  // A full-line YAML or C# comment names a verb; it does not run one.
  private static bool IsComment(string line)
  {
    string trimmed = line.TrimStart();
    return (trimmed.Length > 0 && trimmed[0] == '#') || trimmed.StartsWith("//", StringComparison.Ordinal);
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
