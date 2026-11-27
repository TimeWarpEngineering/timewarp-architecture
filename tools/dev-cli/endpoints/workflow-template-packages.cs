#region Purpose
// The template repository's master Packages-* steps of `dev workflow`: pack and verify the packable
// set, pack-only (carry forward or build and pack), and the carry-forward lookup classify runs.
#endregion
#region Design
// Template-only (task 533): .template.config/template.json excludes this file. The rules (which runs
// stage packages, which earlier run may be carried) are services/release-packages.cs; this file runs
// git, dotnet, and gh around them.
//
// Pack: artifacts/packages is emptied, then each project of IPackableProjectService
// (ArchitecturePackableProjectService, 12 packages) is packed in Release into it, and
// CiRunPromotion.VerifyPackageSet must find exactly {PackageId}.{Version}.nupkg for each (Version from
// source/Directory.Build.props). workflow.yml then uploads artifacts/packages as staged-packages, and
// the ci job republishes it as Packages-<run_number> once every gate passed.
// Pack-only (`--phase pack`): with --carry-run, the live Packages-* of that run is downloaded into
// artifacts/packages and verified; a failed download or a set that does not match falls back to
// clean -> build -> pack. Without it, clean -> build -> pack. No test step: the tree already passed
// ci on this green key, or the push changed only skills.
// Carry-forward lookup: every green-key-v1-ci-<key> marker names the run that passed ci on this key.
// Each marker run's event and artifact list come from gh; ReleasePackages.SelectCarryRun picks one.
// Any gh failure reads as no carry run, so pack-only builds.
#endregion

namespace DevCli.Commands;

using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

/// <summary>Packs, verifies, downloads, and carries the template repository's packages.</summary>
internal sealed class WorkflowTemplatePackages
{
  private readonly ITerminal Terminal;
  private readonly IRepoCleanService RepoCleanService;
  private readonly IPackableProjectService PackableProjectService;
  private readonly string RepoRoot;
  private readonly CancellationToken Ct;

  public WorkflowTemplatePackages(
    ITerminal terminal,
    IRepoCleanService repoCleanService,
    IPackableProjectService packableProjectService,
    string repoRoot,
    CancellationToken ct)
  {
    Terminal = terminal;
    RepoCleanService = repoCleanService;
    PackableProjectService = packableProjectService;
    RepoRoot = repoRoot;
    Ct = ct;
  }

  internal string PackagesDirectory => Path.Combine(RepoRoot, "artifacts", "packages");

  /// <summary>`--phase pack`: carry <paramref name="carryRun"/>'s packages forward, else clean -> build -> pack.</summary>
  public async Task RunPackOnlyAsync(long? carryRun)
  {
    Environment.ExitCode = 0;
    if (carryRun is long runId)
    {
      Terminal.WriteLine($"Pipeline: carry forward run {runId} -> verify\n");
      if (await CarryForwardAsync(runId))
      {
        Terminal.WriteLine("\nPipeline SUCCEEDED".Green());
        return;
      }

      Terminal.WriteLine($"Run {runId} could not be carried forward. Packing this commit instead.".Yellow());
      Environment.ExitCode = 0;
    }

    Terminal.WriteLine("Pipeline: clean -> build -> pack\n");
    if (!await WorkflowCommand.RunStepAsync(Terminal, "Clean", new CleanCommand.Handler(Terminal, RepoCleanService).Handle(new CleanCommand(), Ct))) return;
    if (!await WorkflowCommand.RunStepAsync(Terminal, "Build", new BuildCommand.Handler(Terminal).Handle(new BuildCommand(), Ct))) return;
    if (!await PackAsync()) return;

    Terminal.WriteLine("\nPipeline SUCCEEDED".Green());
  }

  /// <summary>Packs the packable set into an emptied artifacts/packages and verifies the file set.</summary>
  public async Task<bool> PackAsync()
  {
    (IReadOnlyList<PackableProject> projects, string? version) = await ReadSetAsync();
    if (version is null) return false;

    ResetPackagesDirectory();
    foreach (PackableProject project in projects)
    {
      Terminal.WriteLine($"\nPacking {project.PackageId} ({Path.GetRelativePath(RepoRoot, project.ProjectPath)})...");
      int exitCode = await DotNet.Pack(project.ProjectPath)
        .WithConfiguration("Release")
        .WithOutput(PackagesDirectory)
        .WithNoValidation()
        .RunAsync(Ct);

      if (exitCode != 0)
      {
        return Fail($"Pack failed for {project.PackageId}.", exitCode);
      }
    }

    return Verify(projects, version);
  }

  /// <summary>
  /// The packable set and the source version. Version is null (with the error printed and the exit
  /// code set) when either cannot be read.
  /// </summary>
  public async Task<(IReadOnlyList<PackableProject> Projects, string? Version)> ReadSetAsync()
  {
    string? version = PropsVersionReader.Read(RepoRoot);
    if (string.IsNullOrWhiteSpace(version))
    {
      Fail("Could not read <Version> from source/Directory.Build.props.");
      return ([], null);
    }

    IReadOnlyList<PackableProject> projects = await PackableProjectService.GetPackableProjectsAsync(RepoRoot, Ct);
    if (projects.Count == 0)
    {
      Fail("No packable projects found.");
      return ([], null);
    }

    Terminal.WriteLine($"Packable set ({projects.Count}) at {version}: {string.Join(", ", projects.Select(project => project.PackageId))}");
    return (projects, version);
  }

  /// <summary>True when artifacts/packages holds exactly {PackageId}.{version}.nupkg for each project.</summary>
  public bool Verify(IReadOnlyList<PackableProject> projects, string version)
  {
    string[] files = Directory.Exists(PackagesDirectory) ? Directory.GetFiles(PackagesDirectory, "*.nupkg") : [];
    PackageSetVerification verification = CiRunPromotion.VerifyPackageSet([.. files.Select(Path.GetFileName).OfType<string>()], projects, version);
    if (verification.IsMatch)
    {
      Terminal.WriteLine($"Package set verified: {projects.Count} packages at {version}.".Green());
      return true;
    }

    if (verification.Missing.Count > 0) Terminal.WriteErrorLine($"Missing package(s): {string.Join(", ", verification.Missing)}".Red());
    if (verification.Unexpected.Count > 0) Terminal.WriteErrorLine($"Unexpected package(s): {string.Join(", ", verification.Unexpected)}".Red());
    return Fail("The package set does not match the packable set.");
  }

  /// <summary>
  /// Lists <paramref name="runId"/>'s artifacts. Null (with the error printed) when gh cannot list them.
  /// </summary>
  public async Task<IReadOnlyList<RunArtifact>?> ListRunArtifactsAsync(long runId)
  {
    CommandOutput listed;
    try
    {
      listed = await Gh.Api($"repos/{{owner}}/{{repo}}/actions/runs/{Id(runId)}/artifacts?per_page=100")
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation()
        .CaptureAsync(Ct);
    }
    catch (Win32Exception)
    {
      Terminal.WriteErrorLine("The gh CLI could not be launched. On a runner workflow.yml sets GH_TOKEN; locally install gh and run `gh auth login`.".Red());
      return null;
    }

    if (!listed.Success)
    {
      Terminal.WriteErrorLine($"Could not list the artifacts of run {runId}: {listed.Stderr.Trim()}".Red());
      return null;
    }

    return JsonSerializer.Deserialize(listed.Stdout, DevCliJsonContext.Default.RunArtifactListResponse)?.Artifacts ?? [];
  }

  /// <summary>Downloads <paramref name="artifactName"/> of <paramref name="runId"/> into an emptied artifacts/packages.</summary>
  public async Task<bool> DownloadAsync(long runId, string artifactName)
  {
    ResetPackagesDirectory();
    Terminal.WriteLine($"Downloading {artifactName} from run {runId}...");
    int exitCode = await Gh.Run().Download(Id(runId))
      .WithName(artifactName)
      .WithDirectory(PackagesDirectory)
      .WithWorkingDirectory(RepoRoot)
      .WithNoValidation()
      .RunAsync(Ct);

    return exitCode == 0 || Fail($"Download of {artifactName} from run {runId} failed.", exitCode);
  }

  /// <summary>
  /// classify: the earlier non-PR run with a live Packages-* that passed ci on <paramref name="key"/>,
  /// or null. Never fails the job: a gh problem is logged and reads as none.
  /// </summary>
  internal static async Task<long?> FindCarryRunAsync(ITerminal terminal, string repoRoot, string key, CancellationToken ct)
  {
    string? repository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
    long currentRun = long.TryParse(Environment.GetEnvironmentVariable("GITHUB_RUN_ID"), NumberStyles.None, CultureInfo.InvariantCulture, out long run) ? run : 0;
    if (string.IsNullOrWhiteSpace(repository) || !GreenKey.IsKey(key)) return null;

    try
    {
      string marker = GreenKey.ArtifactName(GreenKey.GateCi, key);
      CommandOutput markers = await Gh.Api($"repos/{repository}/actions/artifacts?per_page=100&name={Uri.EscapeDataString(marker)}")
        .WithPaginate()
        .WithJq(".artifacts[] | .workflow_run.id")
        .WithWorkingDirectory(repoRoot)
        .WithNoValidation()
        .CaptureAsync(ct);
      if (!markers.Success) return LogNone(terminal, "marker lookup failed");

      List<CarryCandidate> candidates = [];
      foreach (long runId in markers.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 .Select(line => long.TryParse(line, NumberStyles.None, CultureInfo.InvariantCulture, out long id) ? id : 0)
                 .Where(id => id > 0 && id != currentRun)
                 .Distinct())
      {
        CommandOutput runEvent = await Gh.Api($"repos/{repository}/actions/runs/{Id(runId)}").WithJq(".event").WithWorkingDirectory(repoRoot).WithNoValidation().CaptureAsync(ct);
        if (!runEvent.Success) continue;

        string eventName = runEvent.Stdout.Trim();
        if (eventName == "pull_request")
        {
          candidates.Add(new CarryCandidate(runId, eventName, false));
          continue;
        }

        CommandOutput artifacts = await Gh.Api($"repos/{repository}/actions/runs/{Id(runId)}/artifacts?per_page=100").WithWorkingDirectory(repoRoot).WithNoValidation().CaptureAsync(ct);
        if (!artifacts.Success) continue;

        IReadOnlyList<RunArtifact> list = JsonSerializer.Deserialize(artifacts.Stdout, DevCliJsonContext.Default.RunArtifactListResponse)?.Artifacts ?? [];
        bool live = CiRunPromotion.SelectPackagesArtifact(list).Status == PackagesArtifactStatus.Found;
        candidates.Add(new CarryCandidate(runId, eventName, live));
      }

      long? carry = ReleasePackages.SelectCarryRun(candidates, currentRun);
      terminal.WriteLine(carry is null
        ? $"packages carry=none candidates={candidates.Count}. pack-only builds and packs this commit."
        : $"packages carry=run-{carry} candidates={candidates.Count}. pack-only republishes that run's Packages-*.");
      return carry;
    }
    catch (Exception exception) when (exception is Win32Exception or JsonException)
    {
      return LogNone(terminal, exception.Message);
    }
  }

  private async Task<bool> CarryForwardAsync(long runId)
  {
    IReadOnlyList<RunArtifact>? artifacts = await ListRunArtifactsAsync(runId);
    if (artifacts is null) return false;

    PackagesArtifactOutcome selected = CiRunPromotion.SelectPackagesArtifact(artifacts);
    if (selected.Status != PackagesArtifactStatus.Found)
    {
      Terminal.WriteErrorLine($"Run {runId} has no live {ReleasePackages.PackagesArtifactPrefix}* artifact.".Yellow());
      return false;
    }

    if (!await DownloadAsync(runId, selected.Artifact!.Name)) return false;

    (IReadOnlyList<PackableProject> projects, string? version) = await ReadSetAsync();
    return version is not null && Verify(projects, version);
  }

  private void ResetPackagesDirectory()
  {
    if (Directory.Exists(PackagesDirectory)) Directory.Delete(PackagesDirectory, recursive: true);
    Directory.CreateDirectory(PackagesDirectory);
  }

  private bool Fail(string message, int exitCode = 1)
  {
    Terminal.WriteErrorLine(message.Red());
    Environment.ExitCode = exitCode == 0 ? 1 : exitCode;
    return false;
  }

  private static long? LogNone(ITerminal terminal, string reason)
  {
    terminal.WriteLine($"packages carry=none ({reason}). pack-only builds and packs this commit.");
    return null;
  }

  private static string Id(long runId) => runId.ToString(CultureInfo.InvariantCulture);
}
