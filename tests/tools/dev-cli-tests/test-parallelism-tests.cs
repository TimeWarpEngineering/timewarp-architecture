#region Purpose
// Pins `dev test` parallelism: worker counts, distinct per-worker port bases, group caps, start order,
// and the AppHost lane: derived classification plus guards over the monorepo's tests/ and source/ trees.
#endregion

namespace TestParallelism_;

using TimeWarp.Architecture.Testing;

public class PortBases_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<PortBases_Given_>();

  public static Task DerivedBase_Should_WalkTheRingWithoutSharingAPort()
  {
    const int start = InProcTestPorts.DerivedRangeStart + (5 * InProcTestPorts.DerivedStep);

    int[] bases = TestParallelism.PortBases(start, InProcTestPorts.BaseSource.Derived, InProcTestPorts.DerivedSlotCount);

    bases[0].ShouldBe(start);
    bases.Distinct().Count().ShouldBe(InProcTestPorts.DerivedSlotCount);
    foreach (int basePort in bases)
    {
      basePort.ShouldBeGreaterThanOrEqualTo(InProcTestPorts.DerivedRangeStart);
      (basePort + InProcTestPorts.PortSpan).ShouldBeLessThan(InProcTestPorts.DerivedRangeEnd);
    }

    return Task.CompletedTask;
  }

  public static Task OverrideBase_Should_StepUpByDerivedStep()
  {
    TestParallelism.PortBases(7000, InProcTestPorts.BaseSource.Override, 3)
      .ShouldBe([7000, 7000 + InProcTestPorts.DerivedStep, 7000 + (2 * InProcTestPorts.DerivedStep)]);
    return Task.CompletedTask;
  }

  public static Task OverrideNearTop_Should_LimitSlotsToValidPorts()
  {
    const int basePort = 65535 - InProcTestPorts.PortSpan - InProcTestPorts.DerivedStep;

    TestParallelism.SlotLimit(basePort, InProcTestPorts.BaseSource.Override).ShouldBe(2);
    int[] bases = TestParallelism.PortBases(basePort, InProcTestPorts.BaseSource.Override, 2);
    (bases[^1] + InProcTestPorts.PortSpan).ShouldBeLessThanOrEqualTo(65535);
    Should.Throw<ArgumentOutOfRangeException>(
      () => TestParallelism.PortBases(basePort, InProcTestPorts.BaseSource.Override, 3));
    return Task.CompletedTask;
  }
}

public class Limits_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Limits_Given_>();

  public static Task DefaultMaxParallel_Should_BeCpuCountCappedBySlots()
  {
    TestParallelism.DefaultMaxParallel(processorCount: 4, slotLimit: 13).ShouldBe(4);
    TestParallelism.DefaultMaxParallel(processorCount: 32, slotLimit: 13).ShouldBe(13);
    TestParallelism.DefaultMaxParallel(processorCount: 0, slotLimit: 13).ShouldBe(1);
    return Task.CompletedTask;
  }

  public static Task OnlyIntegration_Should_HaveAGroupCap()
  {
    TestParallelism.GroupCap(TestGroups.Integration, 3).ShouldBe(3);
    TestParallelism.GroupCap(TestGroups.Fast, 3).ShouldBeNull();
    TestParallelism.GroupCap(TestGroups.Playwright, 3).ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Order_Should_StartPlaywrightThenIntegrationThenFast()
  {
    (string, string, bool)[] ordered = TestParallelism.Order(
      [
        ("b-fast", TestGroups.Fast, false),
        ("a-fast", TestGroups.Fast, false),
        ("z-integration", TestGroups.Integration, false),
        ("web-spa-playwright-tests", TestGroups.Playwright, false),
      ]);

    ordered.Select(p => p.Item1).ShouldBe(["web-spa-playwright-tests", "z-integration", "a-fast", "b-fast"]);
    return Task.CompletedTask;
  }

  public static Task Order_Should_StartAppHostSuitesFirstWithinTheirGroup()
  {
    (string, string, bool)[] ordered = TestParallelism.Order(
      [
        ("a-integration", TestGroups.Integration, false),
        ("z-apphost", TestGroups.Integration, true),
        ("m-apphost", TestGroups.Integration, true),
      ]);

    ordered.Select(p => p.Item1).ShouldBe(["m-apphost", "z-apphost", "a-integration"]);
    return Task.CompletedTask;
  }
}

public partial class BootsAppHost_Given_
{
  // Projects in the monorepo that must run in the exclusive AppHost lane. Pinned so a derivation
  // regression (for example a regex that stops matching) shows up as a diff, not a silent flake.
  private static readonly string[] ExpectedAppHostSuites =
  [
    "tests/container-apps/api/api-server-integration-tests/api-server-integration-tests.csproj",
    "tests/container-apps/aspire/aspire-tests/aspire-tests.csproj",
    "tests/container-apps/web/web-spa-integration-tests/web-spa-integration-tests.csproj",
  ];

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<BootsAppHost_Given_>();

  public static Task TestingPackage_Should_MarkTheProject()
  {
    Dictionary<string, string> files = new()
    {
      [Full("t/a.csproj")] = """<Project><ItemGroup><PackageReference Include="Aspire.Hosting.Testing" /></ItemGroup></Project>""",
    };

    TestParallelism.BootsAppHost(Full("t/a.csproj"), files.GetValueOrDefault).ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task ClosedBoxHelperCompile_Should_MarkTheProject()
  {
    Dictionary<string, string> files = new()
    {
      [Full("t/a.csproj")] = """<Compile Include="..\common\closed-box-app-host\closed-box-app-host.cs" Link="x.cs" />""",
    };

    TestParallelism.BootsAppHost(Full("t/a.csproj"), files.GetValueOrDefault).ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task TransitiveProjectReference_Should_MarkTheProject()
  {
    Dictionary<string, string> files = new()
    {
      [Full("t/a/a.csproj")] = """<ProjectReference Include="..\b\b.csproj" />""",
      [Full("t/b/b.csproj")] = """<ProjectReference Include="../a/a.csproj" /><PackageReference Include="Aspire.Hosting.Testing" />""",
    };

    TestParallelism.BootsAppHost(Full("t/a/a.csproj"), files.GetValueOrDefault).ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task MentionInACommentOrOtherPackage_Should_NotMarkTheProject()
  {
    Dictionary<string, string> files = new()
    {
      [Full("t/a.csproj")] = """
        <!-- hosts launch via Aspire.Hosting.Testing -->
        <PackageReference Include="Aspire.Hosting.AppHost" />
        <ProjectReference Include="missing.csproj" />
        """,
    };

    TestParallelism.BootsAppHost(Full("t/a.csproj"), files.GetValueOrDefault).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task MonorepoAppHostSuites_Should_BeExactlyTheExpectedSet()
  {
    string root = RepoRoot();
    string[] classified = TestProjects(root)
      .Where(project => TestParallelism.BootsAppHost(Path.Combine(root, project), ReadIfExists))
      .ToArray();

    classified.ShouldBe(ExpectedAppHostSuites, ignoreOrder: true);
    return Task.CompletedTask;
  }

  // Independent of the csproj derivation: any project whose own sources (its folder plus literal
  // Compile includes) call an AppHost boot must be classified exclusive.
  public static Task EveryProjectThatBootsAnAppHostInSource_Should_BeClassifiedExclusive()
  {
    string root = RepoRoot();
    string[] booting = TestProjects(root)
      .Where(project => ProjectSources(Path.Combine(root, project)).Any(file => AppHostBoot().IsMatch(File.ReadAllText(file))))
      .ToArray();
    string[] missed = booting
      .Where(project => !TestParallelism.BootsAppHost(Path.Combine(root, project), ReadIfExists))
      .ToArray();

    booting.ShouldNotBeEmpty();
    missed.ShouldBeEmpty();
    return Task.CompletedTask;
  }

  // The *-jaribu-tests aggregators glob co-located tests from source/, which the scan above does not
  // follow, so no co-located test may boot an AppHost.
  public static Task CoLocatedSourceTests_Should_NotBootAnAppHost()
  {
    string root = RepoRoot();
    string[] offenders = Directory
      .EnumerateFiles(Path.Combine(root, "source"), "*-tests.cs", SearchOption.AllDirectories)
      .Where(file => !IsBuildOutput(file) && AppHostBoot().IsMatch(File.ReadAllText(file)))
      .Select(file => Path.GetRelativePath(root, file))
      .ToArray();

    offenders.ShouldBeEmpty();
    return Task.CompletedTask;
  }

  private static string Full(string relative) => Path.GetFullPath(Path.Combine(Path.GetTempPath(), "tw-372", relative));

  private static string? ReadIfExists(string path) => File.Exists(path) ? File.ReadAllText(path) : null;

  private static IEnumerable<string> ProjectSources(string projectPath)
  {
    string directory = Path.GetDirectoryName(projectPath)!;
    IEnumerable<string> own = Directory
      .EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
      .Where(file => !IsBuildOutput(file));
    IEnumerable<string> included = LiteralCompileInclude()
      .Matches(File.ReadAllText(projectPath))
      .Select(match => Path.GetFullPath(Path.Combine(directory, match.Groups["path"].Value.Replace('\\', '/'))))
      .Where(File.Exists);
    return own.Concat(included);
  }

  // A boot call, not a mention: Design regions and this guard name the types without calling them.
  [System.Text.RegularExpressions.GeneratedRegex(@"\b(DistributedApplicationTestingBuilder|ClosedBoxAppHost)\.CreateAsync\b")]
  private static partial System.Text.RegularExpressions.Regex AppHostBoot();

  [System.Text.RegularExpressions.GeneratedRegex(@"<Compile\s+Include=""(?<path>[^""*$]+\.cs)""")]
  private static partial System.Text.RegularExpressions.Regex LiteralCompileInclude();

  private static bool IsBuildOutput(string path)
  {
    string normalized = path.Replace('\\', '/');
    return normalized.Contains("/bin/", StringComparison.Ordinal) || normalized.Contains("/obj/", StringComparison.Ordinal);
  }

  private static string RepoRoot()
  {
    string? root = AppContext.BaseDirectory;
    while (root is not null && !File.Exists(Path.Combine(root, "timewarp-architecture.slnx")))
    {
      root = Path.GetDirectoryName(root);
    }

    root.ShouldNotBeNull();
    return root;
  }

  // Repo-relative paths of every project `dev test` would run (the tests/ glob, minus bin/obj).
  private static string[] TestProjects(string root) =>
    [.. Directory
      .GetFiles(Path.Combine(root, "tests"), "*.csproj", SearchOption.AllDirectories)
      .Where(project => !IsBuildOutput(project))
      .Select(project => Path.GetRelativePath(root, project).Replace('\\', '/'))];
}
