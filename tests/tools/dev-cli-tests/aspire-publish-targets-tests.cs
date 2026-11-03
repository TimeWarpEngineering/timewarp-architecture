#region Purpose
// Pins `dev publish` safety-suite arguments: one aspire-tests run for every target, with a class filter
// that selects exactly the publish safety suites (one class per target).
#endregion

namespace AspirePublishTargets_;

using System.Text.RegularExpressions;

public partial class SafetySuiteTestArguments_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SafetySuiteTestArguments_Given_>();

  public static Task SingleTarget_Should_FilterOnItsOwnClassAndBuild()
  {
    AspirePublishTargets.SafetySuiteTestArguments([AspirePublishTargets.Kubernetes], noBuild: false)
      .ShouldBe(["test", "-c", "Release", "--", "--filter-class", "KubernetesPublish_Given_"]);
    return Task.CompletedTask;
  }

  public static Task AllTargetsNoBuild_Should_RunOnceWithoutBuildOrRestore()
  {
    AspirePublishTargets.SafetySuiteTestArguments(AspirePublishTargets.All, noBuild: true)
      .ShouldBe(["test", "-c", "Release", "--no-build", "--no-restore", "--", "--filter-class", "Publish_Given_"]);
    return Task.CompletedTask;
  }

  public static Task AllTargets_Should_HaveDistinctOutputsAndVariables()
  {
    AspirePublishTargets.All.Select(target => target.Name).ShouldBe(["compose", "kubernetes", "aca", "aca-neon"]);
    AspirePublishTargets.All.Select(target => target.OutputDirectory).ShouldBeUnique();
    AspirePublishTargets.All.Select(target => target.OutputEnvironmentVariable).ShouldBeUnique();
    return Task.CompletedTask;
  }

  // Jaribu's --filter-class is one substring of the class FullName, so the combined filter must hit
  // every target's suite and no other aspire-tests class.
  public static Task CombinedFilter_Should_SelectExactlyThePublishSafetySuites()
  {
    string suiteDirectory = Path.Combine(RepoRoot(), "tests", "container-apps", "aspire", "aspire-tests");
    string[] matched =
    [
      .. Directory.EnumerateFiles(suiteDirectory, "*.cs", SearchOption.AllDirectories)
        .SelectMany(file => ClassDeclaration().Matches(File.ReadAllText(file)))
        .Select(match => (Name: match.Groups["name"].Value, IsPartial: match.Groups["partial"].Success))
        .Where(declaration => declaration.Name.Contains(AspirePublishTargets.SafetySuiteClassSuffix, StringComparison.OrdinalIgnoreCase))
        .GroupBy(declaration => declaration.Name, StringComparer.Ordinal)
        // A suite split across partial files declares its class once per file; it is still one class.
        // A repeated name that is not partial everywhere is a second class, so every declaration stays
        // and the exact-set assertion fails.
        .SelectMany(group => group.All(declaration => declaration.IsPartial) ? group.Take(1) : group)
        .Select(declaration => declaration.Name)
        .Order(StringComparer.Ordinal),
    ];

    matched.ShouldBe([.. AspirePublishTargets.All.Select(target => target.SafetySuiteClass).Order(StringComparer.Ordinal)]);
    return Task.CompletedTask;
  }

  [GeneratedRegex(@"(?<partial>\bpartial\s+)?\bclass\s+(?<name>\w+)")]
  private static partial Regex ClassDeclaration();

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
