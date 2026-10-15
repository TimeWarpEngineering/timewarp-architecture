#region Purpose
// Pins `dev test` argument construction: --no-build only for projects the solution build built.
#endregion

namespace DotNetTestArguments_;

public class For_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<For_Given_>();

  public static Task Default_Should_Build()
  {
    DotNetTestArguments.For(noBuild: false, builtBySolution: true)
      .ShouldBe(["test", "-c", "Release"]);
    return Task.CompletedTask;
  }

  public static Task NoBuildSolutionProject_Should_SkipBuildAndRestore()
  {
    DotNetTestArguments.For(noBuild: true, builtBySolution: true)
      .ShouldBe(["test", "-c", "Release", "--no-build", "--no-restore"]);
    return Task.CompletedTask;
  }

  public static Task NoBuildOutOfSolutionProject_Should_Build()
  {
    DotNetTestArguments.For(noBuild: true, builtBySolution: false)
      .ShouldBe(["test", "-c", "Release"]);
    return Task.CompletedTask;
  }
}

public class SolutionProjects_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SolutionProjects_Given_>();

  private const string Slnx = """
    <Solution>
      <Folder Name="/tests/">
        <!-- template conditional -->
        <Project Path="tests/foundation/foundation-domain-tests/foundation-domain-tests.csproj" />
        <!-- end conditional -->
        <Project Path="tests/common/timewarp-testing/timewarp-testing.csproj" />
      </Folder>
    </Solution>
    """;

  public static Task ListedProjects_Should_ResolveToFullPaths()
  {
    string root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "repo"));

    HashSet<string> projects = DotNetTestArguments.SolutionProjects(Slnx, root);

    projects.ShouldContain(Path.GetFullPath(Path.Combine(root, "tests/foundation/foundation-domain-tests/foundation-domain-tests.csproj")));
    projects.ShouldContain(Path.GetFullPath(Path.Combine(root, "tests/common/timewarp-testing/timewarp-testing.csproj")));
    projects.Count.ShouldBe(2);
    return Task.CompletedTask;
  }

  public static Task UnlistedAggregator_Should_NotBeInSolution()
  {
    string root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "repo"));

    HashSet<string> projects = DotNetTestArguments.SolutionProjects(Slnx, root);

    projects.ShouldNotContain(Path.GetFullPath(Path.Combine(root, "tests/container-apps/web/web-jaribu-tests/web-jaribu-tests.csproj")));
    return Task.CompletedTask;
  }
}
