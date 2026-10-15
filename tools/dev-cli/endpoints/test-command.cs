#region Purpose
// Run the test suite
#endregion
#region Design
// Executes dotnet test for every csproj under tests/ (not the .slnx — generated apps exclude
// feature-flagged test trees from disk, so globbing keeps the same semantics).
// Handler stores Ct and RepoRoot as fields so private methods are zero-parameter.
// Buffers each project's output and prints it whole when that project finishes (task 370);
// --quiet prints only failing projects' output. A summary table closes the run.
//
// Task 145-007: all suite-shaped projects are Jaribu Microsoft.Testing.Platform. On .NET 11,
// `dotnet test <csproj-path>` / `--project` fail for MTP ("Testing with VSTest target is no longer
// supported"). Always run bare `dotnet test -c Release` with cwd = project directory, which
// picks up the project-local global.json test.runner. Root global.json must NOT set a runner
// (co-located runfiles and other tooling still use the default VSTest path when needed).
// Task 330: the base is resolved ONCE here (InProcTestPorts, compiled in from timewarp-testing:
// TIMEWARP_TEST_PORT_BASE if set, else derived from this checkout's root).
// Task 370: projects run in parallel, at most --max-parallel at once (default: CPU count) and at
// most --max-integration integration-group projects at once. Each worker owns one port base from
// TestParallelism.PortBases (the resolved base first) and exports it to its child `dotnet test`,
// so a suite and anything it spawns agree on one port set and concurrent suites never share one.
// Task 365 / 370: a serial build phase runs first (DotNetTestArguments): the Release solution build
// unless --no-build, then each selected out-of-solution project. Every `dotnet test` is
// --no-build --no-restore, so concurrent tests never race on shared obj/ and bin/ folders.
// Task 366: --group <fast|playwright|integration> runs one convention-derived group (TestGroups)
// so CI runs the groups as parallel jobs. With --group, any project that matches no group or
// several fails the run before a test starts.
#endregion

namespace DevCli.Commands;

using System.Diagnostics;
using System.Globalization;

[NuruRoute("test", Description = "Run the test suite")]
internal sealed class TestCommand : ICommand<Unit>
{
  [Option("quiet", "q", Description = "Hide test output unless a project fails")]
  public bool Quiet { get; set; }

  [Option("no-build", Description = "Skip the Release solution build (the caller just ran it)")]
  public bool NoBuild { get; set; }

  [Option("group", "g", Description = "Run one test group: fast, playwright, or integration (default: all)")]
  public string? Group { get; set; }

  [Option("max-parallel", "j", Description = "Test projects running at once (default: CPU count, capped by port slots)")]
  public int? MaxParallel { get; set; }

  [Option("max-integration", Description = "Integration-group projects running at once (default: 3; Docker and host heavy)")]
  public int? MaxIntegration { get; set; }

  internal sealed class Handler : ICommandHandler<TestCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private readonly Lock OutputLock = new();
    private TestCommand Command = null!;
    private CancellationToken Ct;
    private string RepoRoot = null!;
    private HashSet<string> SolutionProjects = [];

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(TestCommand command, CancellationToken ct)
    {
      Command = command;
      Ct = ct;

      if (!FindRepoRoot()) return Unit.Value;
      if (!await TestAsync()) return Unit.Value;

      Terminal.WriteLine("\nTests completed successfully!".Green());
      return Unit.Value;
    }

    private bool FindRepoRoot()
    {
      string? root = Git.FindRoot();
      if (root is null)
      {
        Terminal.WriteErrorLine("Error: could not find repository root.");
        Environment.ExitCode = 1;
        return false;
      }

      RepoRoot = root;
      return true;
    }

    private async Task<bool> TestAsync()
    {
      // Globbing the tests/ tree (rather than the .slnx) keeps this correct in a generated app where
      // feature-flagged test projects are physically excluded.
      string testsDirectory = Path.Combine(RepoRoot, "tests");
      string[] projects = Directory
        .GetFiles(testsDirectory, "*.csproj", SearchOption.AllDirectories)
        .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                 && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        .OrderBy(p => p, StringComparer.Ordinal)
        .ToArray();

      if (Command.Group is not null)
      {
        string[]? grouped = SelectGroup(projects);
        if (grouped is null) return false;
        projects = grouped;
      }

      string? overrideValue = Environment.GetEnvironmentVariable(InProcTestPorts.EnvironmentVariableName);
      string? checkoutRoot = InProcTestPorts.FindCheckoutRoot([RepoRoot]);
      (int basePort, InProcTestPorts.BaseSource source) = InProcTestPorts.ResolveBase(overrideValue, checkoutRoot);
      Terminal.WriteLine($"In-proc test ports: {InProcTestPorts.Describe(basePort, source, checkoutRoot)}");

      int slotLimit = TestParallelism.SlotLimit(basePort, source);
      int maxParallel = Command.MaxParallel ?? TestParallelism.DefaultMaxParallel(Environment.ProcessorCount, slotLimit);
      int maxIntegration = Command.MaxIntegration ?? TestParallelism.DefaultIntegrationMaxParallel;
      if (maxParallel < 1 || maxParallel > slotLimit || maxIntegration < 1)
      {
        Terminal.WriteErrorLine(
          $"--max-parallel must be 1..{slotLimit} (one port base each) and --max-integration at least 1.".Red());
        Environment.ExitCode = 1;
        return false;
      }

      string solutionFile = Path.Combine(RepoRoot, "timewarp-architecture.slnx");
      SolutionProjects = DotNetTestArguments.SolutionProjects(await File.ReadAllTextAsync(solutionFile, Ct), RepoRoot);
      if (!await BuildAsync(solutionFile, projects)) return false;

      int[] portBases = TestParallelism.PortBases(basePort, source, maxParallel);
      Terminal.WriteLine(
        $"Running {projects.Length} test projects, up to {maxParallel} at once " +
        $"(integration: {maxIntegration}); port bases {string.Join(", ", portBases)}");

      ProjectResult[] results = await RunAllAsync(projects, portBases, maxIntegration);
      WriteSummary(results);

      if (results.Any(r => !r.Passed))
      {
        Terminal.WriteErrorLine("Tests failed!".Red());
        Environment.ExitCode = 1;
        return false;
      }

      return true;
    }

    // Serial build phase (DotNetTestArguments): solution unless --no-build, then out-of-solution projects.
    private async Task<bool> BuildAsync(string solutionFile, string[] projects)
    {
      if (!Command.NoBuild)
      {
        Terminal.WriteLine($"\nBuilding {Path.GetFileName(solutionFile)} (Release)...");
        CommandResult build = DotNet.Build()
          .WithProject(solutionFile)
          .WithConfiguration("Release")
          .WithNoValidation()
          .Build();
        if (!await BuildStepAsync(build, Path.GetFileName(solutionFile))) return false;
      }

      foreach (string project in projects.Where(p => !SolutionProjects.Contains(Path.GetFullPath(p))))
      {
        Terminal.WriteLine($"Building {Path.GetFileNameWithoutExtension(project)} (not in the solution)...");
        CommandResult build = Shell.Builder("dotnet")
          .WithArguments(DotNetTestArguments.Build())
          .WithWorkingDirectory(ProjectDirectory(project))
          .WithNoValidation()
          .Build();
        if (!await BuildStepAsync(build, Path.GetFileNameWithoutExtension(project))) return false;
      }

      return true;
    }

    private async Task<bool> BuildStepAsync(CommandResult build, string name)
    {
      CommandOutput result = await build.CaptureAsync(Ct);
      if (result.Success) return true;

      Terminal.WriteErrorLine(result.Combined);
      Terminal.WriteErrorLine($"Build failed: {name}".Red());
      Environment.ExitCode = 1;
      return false;
    }

    // Dispatcher: whenever a worker's port base is free, start the first pending project in start
    // order (TestParallelism.Order) whose group cap has room, so the order holds under the caps.
    private async Task<ProjectResult[]> RunAllAsync(string[] projects, int[] portBases, int maxIntegration)
    {
      List<(string Project, string Group)> pending = [.. TestParallelism.Order(projects.Select(p => (p, GroupOf(p))))];
      Queue<int> freeBases = new(portBases);
      Dictionary<string, int> runningPerGroup = [];
      Dictionary<Task<ProjectResult>, (int PortBase, string Group)> running = [];
      List<ProjectResult> results = [];

      while (pending.Count > 0 || running.Count > 0)
      {
        while (freeBases.Count > 0)
        {
          int next = pending.FindIndex(p =>
            TestParallelism.GroupCap(p.Group, maxIntegration) is not int cap
            || runningPerGroup.GetValueOrDefault(p.Group) < cap);
          if (next < 0) break;

          (string project, string group) = pending[next];
          pending.RemoveAt(next);
          int portBase = freeBases.Dequeue();
          runningPerGroup[group] = runningPerGroup.GetValueOrDefault(group) + 1;
          running[RunOneAsync(project, group, portBase)] = (portBase, group);
        }

        Task<ProjectResult> finished = await Task.WhenAny(running.Keys);
        (int finishedBase, string finishedGroup) = running[finished];
        running.Remove(finished);
        freeBases.Enqueue(finishedBase);
        runningPerGroup[finishedGroup]--;
        results.Add(await finished);
      }

      return [.. results];
    }

    private async Task<ProjectResult> RunOneAsync(string project, string group, int portBase)
    {
      string name = Path.GetFileNameWithoutExtension(project);
      lock (OutputLock) Terminal.WriteLine($"Started  {name} (port base {portBase})");
      var stopwatch = Stopwatch.StartNew();
      CommandOutput output = await BuildTestCommand(project, portBase).CaptureAsync(Ct);
      ProjectResult result = new(name, group, output.Success, stopwatch.Elapsed, portBase);
      WriteProjectOutput(result, output);
      return result;
    }

    private void WriteProjectOutput(ProjectResult result, CommandOutput output)
    {
      lock (OutputLock)
      {
        string verdict = result.Passed ? "passed".Green() : "FAILED".Red();
        Terminal.WriteLine($"\n===== {result.Name}: {verdict} in {FormatDuration(result.Duration)} =====");
        if (!result.Passed)
          Terminal.WriteErrorLine(output.Combined);
        else if (!Command.Quiet)
          Terminal.WriteLine(output.Combined);
      }
    }

    private void WriteSummary(ProjectResult[] results)
    {
      int nameWidth = Math.Max("Project".Length, results.Max(r => r.Name.Length));
      int groupWidth = Math.Max("Group".Length, results.Max(r => r.Group.Length));
      Terminal.WriteLine($"\n{"Project".PadRight(nameWidth)}  {"Group".PadRight(groupWidth)}  Result  Duration  Port base");
      foreach (ProjectResult r in results.OrderBy(r => r.Name, StringComparer.Ordinal))
      {
        string verdict = r.Passed ? "passed".Green() : "FAILED".Red();
        Terminal.WriteLine(
          $"{r.Name.PadRight(nameWidth)}  {r.Group.PadRight(groupWidth)}  {verdict}  {FormatDuration(r.Duration),8}  {r.PortBase}");
      }

      Terminal.WriteLine($"{results.Count(r => r.Passed)} of {results.Length} projects passed.");
    }

    private static string FormatDuration(TimeSpan duration) =>
      duration.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + "s";

    // Groups gate --group strictly (SelectGroup); here a misfiled project just runs uncapped.
    private string GroupOf(string project)
    {
      string[] groups = TestGroups.GroupsOf(Path.GetRelativePath(RepoRoot, project));
      return groups.Length == 1 ? groups[0] : TestGroups.Fast;
    }

    // Null when the group is unknown or a project does not belong to exactly one group.
    private string[]? SelectGroup(string[] projects)
    {
      if (!TestGroups.IsGroup(Command.Group!))
      {
        Terminal.WriteErrorLine($"Unknown test group '{Command.Group}'. Groups: {string.Join(", ", TestGroups.Names)}".Red());
        Environment.ExitCode = 1;
        return null;
      }

      List<string> selected = [];
      bool wellFormed = true;
      foreach (string project in projects)
      {
        string relative = Path.GetRelativePath(RepoRoot, project);
        string[] groups = TestGroups.GroupsOf(relative);
        if (groups.Length != 1)
        {
          Terminal.WriteErrorLine($"{relative} must belong to exactly one test group; it matches [{string.Join(", ", groups)}]. See TestGroups.".Red());
          wellFormed = false;
        }
        else if (groups[0] == Command.Group)
        {
          selected.Add(project);
        }
      }

      if (!wellFormed)
      {
        Environment.ExitCode = 1;
        return null;
      }

      Terminal.WriteLine($"Test group '{Command.Group}': {selected.Count} of {projects.Length} projects");
      return [.. selected];
    }

    /// <summary>
    /// All suite-shaped projects are Microsoft.Testing.Platform (Jaribu): bare
    /// <c>dotnet test -c Release --no-build --no-restore</c> with cwd = project directory so
    /// project-local global.json supplies the MTP runner (task 145-007). The worker's port base
    /// goes to the child (task 370).
    /// </summary>
    private static CommandResult BuildTestCommand(string project, int portBase)
    {
      return Shell.Builder("dotnet")
        .WithArguments(DotNetTestArguments.Test())
        .WithWorkingDirectory(ProjectDirectory(project))
        .WithEnvironmentVariable(InProcTestPorts.EnvironmentVariableName, portBase.ToString(CultureInfo.InvariantCulture))
        .WithNoValidation()
        .Build();
    }

    private static string ProjectDirectory(string project) =>
      Path.GetDirectoryName(project)
        ?? throw new InvalidOperationException($"No directory for project path: {project}");

    private sealed record ProjectResult(string Name, string Group, bool Passed, TimeSpan Duration, int PortBase);
  }
}
