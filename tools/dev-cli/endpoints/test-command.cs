#region Purpose
// Run the test suite
#endregion
#region Design
// Executes dotnet test for every csproj under tests/ (not the .slnx — generated apps exclude
// feature-flagged test trees from disk, so globbing keeps the same semantics).
// Handler stores Ct and RepoRoot as fields so private methods are zero-parameter.
// Streams per-project output via Amuru RunAsync by default; --quiet uses CaptureAsync.
//
// Task 145-007: all suite-shaped projects are Jaribu Microsoft.Testing.Platform. On .NET 10,
// `dotnet test <csproj-path>` / `--project` fail for MTP ("Testing with VSTest target is no longer
// supported"). Always run bare `dotnet test -c Release` with cwd = project directory, which
// picks up the project-local global.json test.runner. Root global.json must NOT set a runner
// (co-located runfiles and other tooling still use the default VSTest path when needed).
// Projects still run ONE AT A TIME: integration suites share fixed ports
// (web=7000, api=7255, yarp=8443).
#endregion

namespace DevCli.Commands;

[NuruRoute("test", Description = "Run the test suite")]
internal sealed class TestCommand : ICommand<Unit>
{
  [Option("quiet", "q", Description = "Hide test output unless a project fails")]
  public bool Quiet { get; set; }

  internal sealed class Handler : ICommandHandler<TestCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private TestCommand Command = null!;
    private CancellationToken Ct;
    private string RepoRoot = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(TestCommand command, CancellationToken ct)
    {
      Command = command;
      Ct = ct;

      if (!FindRepoRoot()) return Value;
      if (!await TestAsync()) return Value;

      Terminal.WriteLine("\nTests completed successfully!".Green());
      return Value;
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
      // Run test projects ONE AT A TIME. The integration suites spin up real web/api/yarp hosts on
      // FIXED ports (web=7000, api=7255 shared by the web + api suites, yarp=8443), so running the
      // whole solution at once lets concurrent hosts collide on those ports and fail spuriously.
      // Globbing the tests/ tree (rather than the .slnx) keeps this correct in a generated app where
      // feature-flagged test projects are physically excluded.
      string testsDirectory = Path.Combine(RepoRoot, "tests");
      string[] projects = Directory
        .GetFiles(testsDirectory, "*.csproj", SearchOption.AllDirectories)
        .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                 && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        .OrderBy(p => p, StringComparer.Ordinal)
        .ToArray();

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
    /// global.json supplies the MTP runner (task 145-007).
    /// </summary>
    private static CommandResult BuildTestCommand(string project)
    {
      string projectDir = Path.GetDirectoryName(project)
        ?? throw new InvalidOperationException($"No directory for project path: {project}");

      return Shell.Builder("dotnet")
        .WithArguments("test", "-c", "Release")
        .WithWorkingDirectory(projectDir)
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
