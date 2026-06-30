#region Purpose
// Run the test suite
#endregion
#region Design
// Executes dotnet test for all tests in the repository
// Handler stores Ct and RepoRoot as fields so private methods are zero-parameter
#endregion

namespace DevCli.Commands;

[NuruRoute("test", Description = "Run the test suite")]
internal sealed class TestCommand : ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<TestCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private CancellationToken Ct;
    private string RepoRoot = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(TestCommand command, CancellationToken ct)
    {
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
        CommandOutput result = await Shell.Builder("dotnet")
          .WithArguments("test", project, "--configuration", "Release")
          .WithWorkingDirectory(RepoRoot)
          .WithNoValidation()
          .RunAndCaptureAsync(Ct);

        if (!result.Success)
        {
          Terminal.WriteErrorLine(result.Combined);
          allPassed = false;
        }
      }

      if (!allPassed)
      {
        Terminal.WriteErrorLine("Tests failed!".Red());
        Environment.ExitCode = 1;
        return false;
      }

      return true;
    }
  }
}
