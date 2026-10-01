#region Purpose
// Stop the AppHost and delete every Docker volume it created (`dev db nuke --yes`): next dev run starts empty.
#endregion

#region Design
// Wraps `aspire stop --apphost <csproj> --force --volumes` (Aspire CLI 13.6+, task 266). Unlike
// `dev db reset` (drop + migrate inside the RUNNING server; the volume survives), nuke stops the
// AppHost and removes the volumes themselves, so the next `dev run` re-creates postgres from
// nothing: web-migrations re-applies every migration and seeds re-run.
//
// Safety, in order:
//   1. Resolve this AppHost's volumes from Docker BEFORE acting (DbNuke.VolumeNamePrefix — per
//      checkout path, so a sibling worktree's data is never touched). Docker unreachable → stop.
//   2. Without --yes: print exactly what would be destroyed (the AppHost stop, every volume name
//      and every container that references each volume — stopped ones to remove, running ones the
//      stop must end) and exit 1. Nothing runs.
//   3. Aspire CLI version guard, shared with dev run -lp (AspireCli → AspireRun.ValidateCliVersion);
//      a pre-13.6 CLI has no --volumes and is refused with the update command.
//   4. aspire stop --force --volumes, then a Docker sweep of whatever still carries the prefix:
//      Aspire only removes volumes it recorded as owned, and a volume that predates 13.6
//      ownership records is adopted and left intact (see db-nuke.cs Design). The sweep is the
//      intersection with the list resolved in step 1 — the same names step 2 prints, and that the
//      --yes path prints before stopping — so it never reaches a volume the operator was not shown.
//   5. Before `docker volume rm`, the containers still referencing those volumes (Docker refuses
//      the rm while any container, even an exited one, mounts the volume — task 269). Stopped
//      leftovers are `docker rm`'d and printed; if any is still running, nuke refuses, names it,
//      and removes nothing. Never `docker rm -f`. Containers are found per volume, so only
//      containers holding this checkout's volumes are ever touched.
// Pure argument/listing/parsing/cleanup-decision/refusal logic lives in services/db-nuke.cs
// (dev-cli-tests gate it without Aspire or Docker).
#endregion

namespace DevCli.Commands;

[NuruRoute("nuke", Description = "Stop the AppHost and delete its Docker volumes (aspire stop --force --volumes; requires --yes). Unlike `db reset`, the volume itself is removed")]
[NuruRouteExample("db nuke", Description = "List the AppHost stop and volumes that would be destroyed, without acting")]
[NuruRouteExample("db nuke --yes", Description = "Stop the AppHost and delete its volumes; next dev run starts empty")]
internal sealed class DbNukeCommand : DbGroup, ICommand<Unit>
{
  [Option("yes", "y", Description = "Confirm: stop the AppHost and permanently delete its volumes (required)")]
  public bool Yes { get; set; }

  internal sealed class Handler : ICommandHandler<DbNukeCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private CancellationToken Ct;
    private string RepoRoot = null!;
    private string AppHostProject = null!;
    private string VolumePrefix = null!;
    private string[] Volumes = [];

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(DbNukeCommand command, CancellationToken ct)
    {
      Ct = ct;
      if (!FindAppHost()) return Value;

      string[]? volumes = await ListVolumesAsync();
      if (volumes is null) return Value;
      Volumes = volumes;

      if (!command.Yes)
      {
        Dictionary<string, VolumeContainer[]>? containersByVolume = await ListContainersAsync(Volumes);
        if (containersByVolume is null) return Value;

        foreach (string line in DbNuke.BuildRefusalLines(AppHostProject, Volumes, containersByVolume))
        {
          Terminal.WriteLine(line);
        }

        Environment.ExitCode = 1;
        return Value;
      }

      string? versionError = await AspireCli.ValidateVersionAsync(
        RepoRoot, DbNuke.MinimumCliVersion, DbNuke.Requirement, Ct);
      if (versionError is not null)
      {
        Fail($"Error: {versionError}");
        return Value;
      }

      if (!await StopAppHostAsync()) return Value;
      if (!await SweepRemainingVolumesAsync()) return Value;

      Terminal.WriteLine("AppHost stopped and its volumes removed.".Green());
      Terminal.WriteLine("Next `dev run` starts empty: migrations re-apply and seeds re-run.");
      return Value;
    }

    private bool FindAppHost()
    {
      string? root = Git.FindRoot();
      if (root is null)
      {
        return Fail("Error: could not find repository root.");
      }

      RepoRoot = root;
      AppHostProject = Path.GetFullPath(Path.Combine(root, AspireRun.AppHostProject));
      if (!File.Exists(AppHostProject))
      {
        return Fail($"Error: AppHost project not found at {AppHostProject}.");
      }

      VolumePrefix = DbNuke.VolumeNamePrefix(AppHostProject);
      return true;
    }

    private async Task<string[]?> ListVolumesAsync()
    {
      CommandOutput list = await Shell.Builder("docker")
        .WithArguments(DbNuke.BuildVolumeListArguments(VolumePrefix))
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation()
        .CaptureAsync(Ct);

      if (!list.Success)
      {
        Terminal.WriteErrorLine(list.Combined);
        Fail("Error: could not list Docker volumes (is Docker running?). Nothing was stopped or removed.");
        return null;
      }

      return DbNuke.FilterAppHostVolumes(list.Stdout, VolumePrefix);
    }

    private async Task<bool> StopAppHostAsync()
    {
      Terminal.WriteLine($"Stopping AppHost {AppHostProject} and removing its Aspire-owned volumes...");
      foreach (string volume in Volumes)
      {
        Terminal.WriteLine($"  will delete: {volume}");
      }

      CommandOutput stop = await Shell.Builder("aspire")
        .WithArguments(DbNuke.BuildStopArguments(AppHostProject))
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation()
        .PassthroughAsync(Ct);

      if (!stop.Success)
      {
        Terminal.WriteErrorLine($"`aspire stop --force --volumes` failed with code {stop.ExitCode}.".Red());
        Environment.ExitCode = stop.ExitCode == 0 ? 1 : stop.ExitCode;
        return false;
      }

      return true;
    }

    private async Task<bool> SweepRemainingVolumesAsync()
    {
      string[]? listed = await ListVolumesAsync();
      if (listed is null) return false;

      // Only volumes resolved (and printed) before acting; anything that appeared since is left alone.
      string[] remaining = [.. listed.Intersect(Volumes, StringComparer.Ordinal)];
      if (remaining.Length == 0) return true;

      if (!await RemoveStoppedContainersAsync(remaining)) return false;

      Terminal.WriteLine($"Removing {remaining.Length} volume(s) `aspire stop` left behind: {string.Join(", ", remaining)}");
      CommandOutput remove = await Shell.Builder("docker")
        .WithArguments(DbNuke.BuildVolumeRemoveArguments(remaining))
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation()
        .CaptureAsync(Ct);

      if (!remove.Success)
      {
        Terminal.WriteErrorLine(remove.Combined);
        return Fail("Error: `docker volume rm` failed. A container may still use the volume; stop it and re-run `dev db nuke --yes`.");
      }

      return true;
    }

    // Docker refuses `volume rm` while any container mounts the volume, even an exited one.
    private async Task<bool> RemoveStoppedContainersAsync(string[] volumes)
    {
      Dictionary<string, VolumeContainer[]>? containersByVolume = await ListContainersAsync(volumes);
      if (containersByVolume is null) return false;

      ContainerCleanupPlan plan = DbNuke.PlanContainerCleanup(containersByVolume.Values.SelectMany(containers => containers));
      if (!plan.CanProceed)
      {
        foreach (string line in DbNuke.BuildRunningContainerRefusalLines(plan.Running))
        {
          Terminal.WriteErrorLine(line.Red());
        }

        Environment.ExitCode = 1;
        return false;
      }

      if (plan.Stopped.Length == 0) return true;

      Terminal.WriteLine($"Removing {plan.Stopped.Length} stopped container(s) that still hold the volume(s):");
      foreach (VolumeContainer container in plan.Stopped)
      {
        Terminal.WriteLine($"  {DbNuke.Describe(container)}");
      }

      CommandOutput remove = await Shell.Builder("docker")
        .WithArguments(DbNuke.BuildContainerRemoveArguments(plan.Stopped.Select(container => container.Id)))
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation()
        .CaptureAsync(Ct);

      if (!remove.Success)
      {
        Terminal.WriteErrorLine(remove.Combined);
        return Fail("Error: `docker rm` failed. The remaining volume(s) were not removed; re-run `dev db nuke --yes`.");
      }

      return true;
    }

    private async Task<Dictionary<string, VolumeContainer[]>?> ListContainersAsync(IEnumerable<string> volumes)
    {
      Dictionary<string, VolumeContainer[]> containersByVolume = new(StringComparer.Ordinal);
      foreach (string volume in volumes)
      {
        CommandOutput list = await Shell.Builder("docker")
          .WithArguments(DbNuke.BuildContainerListArguments(volume))
          .WithWorkingDirectory(RepoRoot)
          .WithNoValidation()
          .CaptureAsync(Ct);

        if (!list.Success)
        {
          Terminal.WriteErrorLine(list.Combined);
          Fail("Error: could not list Docker containers (is Docker running?). Nothing further was removed.");
          return null;
        }

        containersByVolume[volume] = DbNuke.ParseContainers(list.Stdout);
      }

      return containersByVolume;
    }

    private bool Fail(string message)
    {
      Terminal.WriteErrorLine(message.Red());
      Environment.ExitCode = 1;
      return false;
    }
  }
}
