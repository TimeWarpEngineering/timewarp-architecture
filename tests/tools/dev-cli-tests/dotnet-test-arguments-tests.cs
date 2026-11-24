#region Purpose
// Pins `dev test` argument construction: the MTP coverage arguments after the typed
// `dotnet test -c Release --no-build --no-restore` options, the --filter-class / --filter-method
// forwarding, and the serial build plan for full and --project runs.
#endregion

namespace DotNetTestArguments_;

public class Arguments_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Arguments_Given_>();

  public static Task PlatformArguments_Should_PutCoverageFlagsAfterDoubleDash()
  {
    DotNetTestArguments.PlatformArguments("artifacts/coverage/x").ShouldBe(
    [
      "--",
      "--results-directory", "artifacts/coverage/x", "--coverage",
      "--coverage-output-format", "cobertura", "--coverage-output", "coverage.cobertura.xml"
    ]);
    return Task.CompletedTask;
  }

  public static Task Filters_Should_FollowCoverageFlags()
  {
    DotNetTestArguments.PlatformArguments("artifacts/coverage/x", "Lockout_Given_", "Ok_").ShouldBe(
    [
      "--",
      "--results-directory", "artifacts/coverage/x", "--coverage",
      "--coverage-output-format", "cobertura", "--coverage-output", "coverage.cobertura.xml",
      "--filter-class", "Lockout_Given_", "--filter-method", "Ok_"
    ]);
    return Task.CompletedTask;
  }

  public static Task MethodFilterAlone_Should_OmitClassFilter()
  {
    string[] arguments = DotNetTestArguments.PlatformArguments("artifacts/coverage/x", null, "Ok_");

    arguments.ShouldNotContain("--filter-class");
    arguments[^2..].ShouldBe(["--filter-method", "Ok_"]);
    return Task.CompletedTask;
  }

  public static Task BlankFilters_Should_AddNothing()
  {
    DotNetTestArguments.PlatformArguments("artifacts/coverage/x", " ", "")
      .ShouldBe(DotNetTestArguments.PlatformArguments("artifacts/coverage/x"));
    return Task.CompletedTask;
  }
}

public class BuildPlan_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<BuildPlan_Given_>();

  private static readonly string Root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "repo"));
  private static readonly string InSolution = Path.Combine(Root, "tests/a/a-tests/a-tests.csproj");
  private static readonly string Aggregator = Path.Combine(Root, "tests/b/b-jaribu-tests/b-jaribu-tests.csproj");
  private static readonly HashSet<string> Solution = [InSolution];

  public static Task FullRun_Should_BuildSolutionThenOutOfSolutionProjects()
  {
    TestBuildPlan plan = DotNetTestArguments.BuildPlan([InSolution, Aggregator], Solution, noBuild: false, selectedByName: false);

    plan.Solution.ShouldBeTrue();
    plan.Projects.ShouldBe([Aggregator]);
    return Task.CompletedTask;
  }

  public static Task FullRunNoBuild_Should_BuildOnlyOutOfSolutionProjects()
  {
    TestBuildPlan plan = DotNetTestArguments.BuildPlan([InSolution, Aggregator], Solution, noBuild: true, selectedByName: false);

    plan.Solution.ShouldBeFalse();
    plan.Projects.ShouldBe([Aggregator]);
    return Task.CompletedTask;
  }

  public static Task SelectedRun_Should_BuildEachSelectedProjectAndSkipSolution()
  {
    TestBuildPlan plan = DotNetTestArguments.BuildPlan([InSolution, Aggregator], Solution, noBuild: false, selectedByName: true);

    plan.Solution.ShouldBeFalse();
    plan.Projects.ShouldBe([InSolution, Aggregator]);
    return Task.CompletedTask;
  }

  public static Task SelectedRunNoBuild_Should_BuildOnlyOutOfSolutionProjects()
  {
    TestBuildPlan plan = DotNetTestArguments.BuildPlan([InSolution], Solution, noBuild: true, selectedByName: true);

    plan.Solution.ShouldBeFalse();
    plan.Projects.ShouldBeEmpty();
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
