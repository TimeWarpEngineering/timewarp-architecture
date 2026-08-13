#region Purpose
// Shared AppHost targeting for `dev db *` — resource names and aspire resource invocation.
#endregion

#region Design
// Same constraints as the original db-update command (task 159):
//   - Must run against the LIVE AppHost (dynamic postgres port; only the graph knows the connection).
//   - --apphost pins this repo so a second AppHost on the machine cannot be targeted by accident.
//   - Resource names are string-agreed with AppHost constants.cs (CLI cannot reference AppHost assembly).
//   - web-migrations only exists when the postgres template flag is on.
// Aspire.Hosting.EntityFrameworkCore commands on web-migrations:
//   ef-database-update | ef-database-drop | ef-database-reset | ef-database-status
#endregion

namespace DevCli.Commands;

/// <summary>Constants + runner for Aspire EF migration resource commands.</summary>
internal static class DbAppHost
{
  internal const string AppHostProject =
    "source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj";

  /// <summary>Equals <c>WebMigrationsResourceName</c> in AppHost constants.cs.</summary>
  internal const string MigrationsResourceName = "web-migrations";

  internal const string CommandUpdate = "ef-database-update";
  internal const string CommandDrop = "ef-database-drop";
  internal const string CommandReset = "ef-database-reset";
  internal const string CommandStatus = "ef-database-status";

  internal static Task<int> RunUpdateAsync(ITerminal terminal, CancellationToken cancellationToken) =>
    RunMigrationsCommandAsync(
      terminal,
      CommandUpdate,
      $"Applying pending EF migrations via '{MigrationsResourceName}' on the running AppHost...",
      cancellationToken);

  internal static async Task<int> RunMigrationsCommandAsync(
    ITerminal terminal,
    string aspireCommand,
    string progressLine,
    CancellationToken cancellationToken)
  {
    if (!TryFindRepoRoot(terminal, out string repoRoot))
    {
      return 1;
    }

    string project = Path.Combine(repoRoot, AppHostProject);

    terminal.WriteLine(progressLine);
    CommandOutput result = await Shell.Builder("aspire")
      .WithArguments(
        "resource",
        MigrationsResourceName,
        aspireCommand,
        "--apphost",
        project,
        "--non-interactive",
        "--nologo")
      .WithWorkingDirectory(repoRoot)
      .WithNoValidation()
      .PassthroughAsync(cancellationToken)
      .ConfigureAwait(false);

    if (!result.Success)
    {
      terminal.WriteErrorLine(
        $"'{aspireCommand}' on '{MigrationsResourceName}' failed with code {result.ExitCode}.".Red());
      terminal.WriteErrorLine(
        "Is the AppHost running? Start it with `dev run` first — this command executes against the live resource graph.");
      terminal.WriteErrorLine(
        "If postgres is excluded from the template, the web-migrations resource does not exist.");
      return result.ExitCode == 0 ? 1 : result.ExitCode;
    }

    return 0;
  }

  private static bool TryFindRepoRoot(ITerminal terminal, out string repoRoot)
  {
    string? root = Git.FindRoot();
    if (root is null)
    {
      terminal.WriteErrorLine("Error: could not find repository root.");
      repoRoot = "";
      return false;
    }

    repoRoot = root;
    return true;
  }
}
