#region Purpose
// Run the test suite
#endregion
#region Design
// Executes dotnet test for every csproj under tests/ (not the .slnx — generated apps exclude
// feature-flagged test trees from disk, so globbing keeps the same semantics).
// Handler stores Ct and RepoRoot as fields so private methods are zero-parameter.
// Streams per-project output via Amuru RunAsync by default; --quiet uses CaptureAsync.
//
// Task 145-007: all suite-shaped projects are Jaribu Microsoft.Testing.Platform. On .NET 11,
// `dotnet test <csproj-path>` / `--project` fail for MTP ("Testing with VSTest target is no longer
// supported"). Always run bare `dotnet test -c Release` with cwd = project directory, which
// picks up the project-local global.json test.runner. Root global.json must NOT set a runner
// (co-located runfiles and other tooling still use the default VSTest path when needed).
// Projects still run ONE AT A TIME: integration suites share one InProcTestPorts base.
// Task 330: the base is resolved ONCE here (InProcTestPorts, compiled in from timewarp-testing:
// TIMEWARP_TEST_PORT_BASE if set, else derived from this checkout's root) and exported to every
// child `dotnet test`, so a suite and anything it spawns agree on one port set and two worktrees
// running `dev test` at once get different bases.
// Task 365: default builds every project so a developer never runs stale binaries. --no-build is
// for a caller that just ran the Release solution build (`dev workflow`): .slnx-listed projects
// get --no-build --no-restore; the out-of-solution aggregators and timewarp-testing-tests still
// build inside their own `dotnet test` (DotNetTestArguments).
#endregion

namespace DevCli.Commands;

[NuruRoute("test", Description = "Run the test suite")]
internal sealed class TestCommand : ICommand<Unit>
{
  [Option("quiet", "q", Description = "Hide test output unless a project fails")]
  public bool Quiet { get; set; }

  [Option("no-build", Description = "Skip build/restore for projects the Release solution build already built")]
  public bool NoBuild { get; set; }

  internal sealed class Handler : ICommandHandler<TestCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private TestCommand Command = null!;
    private CancellationToken Ct;
    private string RepoRoot = null!;
    private string PortBase = null!;
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
      // Run test projects ONE AT A TIME. Integration suites share one InProcTestPorts base
      // (exported below), so concurrent hosts in the same process tree would collide.
      // Globbing the tests/ tree (rather than the .slnx) keeps this correct in a generated app where
      // feature-flagged test projects are physically excluded.
      string testsDirectory = Path.Combine(RepoRoot, "tests");
      string[] projects = Directory
        .GetFiles(testsDirectory, "*.csproj", SearchOption.AllDirectories)
        .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                 && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        .OrderBy(p => p, StringComparer.Ordinal)
        .ToArray();

      string? overrideValue = Environment.GetEnvironmentVariable(InProcTestPorts.EnvironmentVariableName);
      string? checkoutRoot = InProcTestPorts.FindCheckoutRoot([RepoRoot]);
      (int basePort, InProcTestPorts.BaseSource source) = InProcTestPorts.ResolveBase(overrideValue, checkoutRoot);
      PortBase = basePort.ToString(System.Globalization.CultureInfo.InvariantCulture);
      Terminal.WriteLine($"In-proc test ports: {InProcTestPorts.Describe(basePort, source, checkoutRoot)}");

      if (Command.NoBuild)
      {
        string solutionFile = Path.Combine(RepoRoot, "timewarp-architecture.slnx");
        SolutionProjects = DotNetTestArguments.SolutionProjects(await File.ReadAllTextAsync(solutionFile, Ct), RepoRoot);
      }

      bool allPassed = true;
      foreach (string project in projects)
      {
        Terminal.WriteLine($"\nTesting {Path.GetFileNameWithoutExtension(project)}...");
        CommandResult command = BuildTestCommand(project);

        if (!await ExecuteAsync(command))
          allPassed = false;
      }

      if (!allPassed)
      {
        Terminal.WriteErrorLine("Tests failed!".Red());
        Environment.ExitCode = 1;
        return false;
      }

      return true;
    }

    /// <summary>
    /// All suite-shaped projects are Microsoft.Testing.Platform (Jaribu): bare
    /// <c>dotnet test -c Release</c> with cwd = project directory so project-local
    /// global.json supplies the MTP runner (task 145-007). --no-build applies only to projects
    /// the solution build covered (task 365).
    /// </summary>
    private CommandResult BuildTestCommand(string project)
    {
      string projectDir = Path.GetDirectoryName(project)
        ?? throw new InvalidOperationException($"No directory for project path: {project}");

      return Shell.Builder("dotnet")
        .WithArguments(DotNetTestArguments.For(Command.NoBuild, SolutionProjects.Contains(Path.GetFullPath(project))))
        .WithWorkingDirectory(projectDir)
        .WithEnvironmentVariable(InProcTestPorts.EnvironmentVariableName, PortBase)
        .WithNoValidation()
        .Build();
    }

    private async Task<bool> ExecuteAsync(CommandResult command)
    {
      if (Command.Quiet)
      {
        CommandOutput result = await command.CaptureAsync(Ct);
        if (!result.Success)
        {
          Terminal.WriteErrorLine(result.Combined);
          return false;
        }

        return true;
      }

      int exitCode = await command.RunAsync(Ct);
      return exitCode == 0;
    }
  }
}
