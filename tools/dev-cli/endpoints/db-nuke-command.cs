#region Purpose
// Stop the AppHost and delete its Aspire-owned volumes (`dev db nuke --yes`): next dev run starts empty.
#endregion

#region Design
// Wraps `aspire stop --apphost <csproj> --force --volumes` (Aspire CLI 13.6+, task 266) and reacts
// to its exit code (task 284). Unlike `dev db reset` (drop + migrate inside the RUNNING server; the
// volume survives), nuke stops the AppHost and Aspire removes the volumes it owns, so the next
// `dev run` re-creates postgres from nothing: web-migrations re-applies every migration and seeds
// re-run.
//
// Order:
//   1. Aspire CLI version guard, shared with dev run -lp (AspireCli → AspireRun.ValidateCliVersion);
//      a pre-13.6 CLI has no --volumes and is refused with the update command.
//   2. Without --yes: print what would happen and exit 1. Nothing runs.
//   3. aspire stop --force --volumes; nuke exits with its exit code.
//   4. A hint for a pre-13.6 volume Aspire adopted rather than owned (see db-nuke.cs Design). The dev
//      CLI never reconstructs Aspire's volume names or removes container-runtime state itself.
// Pure argument/refusal/hint text lives in services/db-nuke.cs (dev-cli-tests gate it without Aspire).
#endregion

namespace DevCli.Commands;

[NuruRoute("nuke", Description = "Stop the AppHost and delete its Aspire-owned volumes (aspire stop --force --volumes; requires --yes). Unlike `db reset`, the volume itself is removed")]
[NuruRouteExample("db nuke", Description = "Show what nuke would do, without acting")]
[NuruRouteExample("db nuke --yes", Description = "Stop the AppHost and delete its volumes; next dev run starts empty")]
internal sealed class DbNukeCommand : DbGroup, ICommand<Unit>
{
  [Option("yes", "y", Description = "Confirm: stop the AppHost and permanently delete its volumes (required)")]
  public bool Yes { get; set; }

  internal sealed class Handler : ICommandHandler<DbNukeCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(DbNukeCommand command, CancellationToken ct)
    {
      string? repoRoot = Git.FindRoot();
      if (repoRoot is null)
      {
        Fail("Error: could not find repository root.");
        return Unit.Value;
      }

      string appHostProject = Path.GetFullPath(Path.Combine(repoRoot, AspireRun.AppHostProject));
      if (!File.Exists(appHostProject))
      {
        Fail($"Error: AppHost project not found at {appHostProject}.");
        return Unit.Value;
      }

      string? versionError = await AspireCli.ValidateVersionAsync(
        repoRoot, DbNuke.MinimumCliVersion, DbNuke.Requirement, ct);
      if (versionError is not null)
      {
        Fail($"Error: {versionError}");
        return Unit.Value;
      }

      if (!command.Yes)
      {
        foreach (string line in DbNuke.BuildRefusalLines(appHostProject))
        {
          Terminal.WriteLine(line);
        }

        Environment.ExitCode = 1;
        return Unit.Value;
      }

      Terminal.WriteLine($"Stopping AppHost {appHostProject} and removing its Aspire-owned volumes...");
      CommandOutput stop = await Shell.Builder("aspire")
        .WithArguments(DbNuke.BuildStopArguments(appHostProject))
        .WithWorkingDirectory(repoRoot)
        .WithNoValidation()
        .PassthroughAsync(ct);

      if (stop.Success)
      {
        Terminal.WriteLine("AppHost stopped and its Aspire-owned volumes removed.".Green());
        Terminal.WriteLine("Next `dev run` starts empty: migrations re-apply and seeds re-run.");
      }
      else
      {
        Terminal.WriteErrorLine($"`aspire stop --force --volumes` failed with code {stop.ExitCode}.".Red());
      }

      foreach (string line in DbNuke.BuildAdoptedVolumeHintLines(
        Environment.GetEnvironmentVariable(DbNuke.ContainerRuntimeVariable)))
      {
        Terminal.WriteLine(line);
      }

      Environment.ExitCode = stop.ExitCode;
      return Unit.Value;
    }

    private void Fail(string message)
    {
      Terminal.WriteErrorLine(message.Red());
      Environment.ExitCode = 1;
    }
  }
}
