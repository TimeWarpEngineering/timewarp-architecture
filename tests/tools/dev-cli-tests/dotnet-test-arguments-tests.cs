#region Purpose
// Pins `dev test` argument construction: the MTP coverage arguments after the typed
// `dotnet test -c Release --no-build --no-restore` options, the --filter-class / --filter-method
// forwarding, the serial build plan for full and --project runs, and the tests/ glob plus .slnx
// diff that `dev build` uses to build the unlisted Jaribu aggregators (task 526).
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

public class UnlistedProjects_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<UnlistedProjects_Given_>();

  private const string Slnx = """
    <Solution>
      <Folder Name="/tests/">
        <Project Path="tests/foundation/foundation-domain-tests/foundation-domain-tests.csproj" />
        <Project Path="tests/common/timewarp-testing/timewarp-testing.csproj" />
      </Folder>
    </Solution>
    """;

  private static string CreateFixture(params string[] relativeProjects)
  {
    string root = Path.Combine(Path.GetTempPath(), "tw-526-fixture", Guid.NewGuid().ToString("N"));
    foreach (string relative in relativeProjects)
    {
      string path = Path.Combine(root, relative);
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      File.WriteAllText(path, "<Project />");
    }

    return root;
  }

  public static Task TestProjects_Should_GlobCsprojOutsideObjAndBin_Sorted()
  {
    string root = CreateFixture(
      "tests/container-apps/web/web-jaribu-tests/web-jaribu-tests.csproj",
      "tests/foundation/foundation-domain-tests/foundation-domain-tests.csproj",
      "tests/foundation/foundation-domain-tests/obj/stale.csproj",
      "tests/foundation/foundation-domain-tests/bin/Release/stale.csproj");
    try
    {
      DotNetTestArguments.TestProjects(Path.Combine(root, "tests")).ShouldBe(
      [
        Path.GetFullPath(Path.Combine(root, "tests/container-apps/web/web-jaribu-tests/web-jaribu-tests.csproj")),
        Path.GetFullPath(Path.Combine(root, "tests/foundation/foundation-domain-tests/foundation-domain-tests.csproj"))
      ]);
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    return Task.CompletedTask;
  }

  public static Task TestProjects_Should_BeEmpty_When_TestsFolderIsMissing()
  {
    string root = Path.Combine(Path.GetTempPath(), "tw-526-fixture", Guid.NewGuid().ToString("N"));

    DotNetTestArguments.TestProjects(Path.Combine(root, "tests")).ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task AllFamilies_Should_YieldEveryAggregatorAndTestingTests()
  {
    string root = CreateFixture(
      "tests/common/timewarp-testing/timewarp-testing.csproj",
      "tests/common/timewarp-testing-tests/timewarp-testing-tests.csproj",
      "tests/container-apps/api/api-jaribu-tests/api-jaribu-tests.csproj",
      "tests/container-apps/web/web-jaribu-tests/web-jaribu-tests.csproj",
      "tests/foundation/foundation-domain-tests/foundation-domain-tests.csproj");
    try
    {
      string[] unlisted = DotNetTestArguments.UnlistedProjects(
        DotNetTestArguments.TestProjects(Path.Combine(root, "tests")),
        DotNetTestArguments.SolutionProjects(Slnx, root));

      unlisted.Select(Path.GetFileNameWithoutExtension)
        .ShouldBe(["timewarp-testing-tests", "api-jaribu-tests", "web-jaribu-tests"]);
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    return Task.CompletedTask;
  }

  // A generated app with --web false has no web tree and no timewarp-testing-tests on disk.
  public static Task FamilyFlaggedOff_Should_YieldOnlyWhatShipped()
  {
    string root = CreateFixture(
      "tests/common/timewarp-testing/timewarp-testing.csproj",
      "tests/container-apps/api/api-jaribu-tests/api-jaribu-tests.csproj",
      "tests/foundation/foundation-domain-tests/foundation-domain-tests.csproj");
    try
    {
      string[] unlisted = DotNetTestArguments.UnlistedProjects(
        DotNetTestArguments.TestProjects(Path.Combine(root, "tests")),
        DotNetTestArguments.SolutionProjects(Slnx, root));

      unlisted.Select(Path.GetFileNameWithoutExtension).ShouldBe(["api-jaribu-tests"]);
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    return Task.CompletedTask;
  }

  public static Task NoAggregators_Should_YieldNothing()
  {
    string root = CreateFixture(
      "tests/common/timewarp-testing/timewarp-testing.csproj",
      "tests/foundation/foundation-domain-tests/foundation-domain-tests.csproj");
    try
    {
      DotNetTestArguments.UnlistedProjects(
        DotNetTestArguments.TestProjects(Path.Combine(root, "tests")),
        DotNetTestArguments.SolutionProjects(Slnx, root)).ShouldBeEmpty();
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    return Task.CompletedTask;
  }
}
