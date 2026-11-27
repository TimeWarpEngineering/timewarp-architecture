#region Purpose
// The template repository's release mode of `dev workflow`: promote the master run's tested
// Packages-* to nuget.org, then smoke the published template.
#endregion
#region Design
// Template-only (task 533): .template.config/template.json excludes this file. A generated app's
// release mode refuses (workflow-command.cs).
//
// The org's DevCli promotion pipeline (timewarp-nuru workflow-command.release.cs), with this repo's
// packable set and smoke:
//   1 tag-gate           on a release event, TagAssertion: the tag is v{Version}. Tag pin: when
//                        v{Version} already exists it must point at HEAD (a break-glass resume must not
//                        mix two commits under one version). HEAD must be an ancestor of origin/master
//                        (fetch-depth: 0). Attestation: the ganda-audit note on HEAD's tree, mode from
//                        --attestation, then dev.jsonc attestation.mode, else require.
//   2 check-version      the DevCli CheckVersionCommand over the 12-package set: refuses a version
//                        nuget.org already has in full.
//   3 locate-run         successful workflow.yml runs at HEAD (gh run list), CiRunPromotion order:
//                        pull_request runs never, push runs first, newest first.
//   4 download-artifact  the first candidate with a live Packages-* (CiRunPromotion
//                        SelectPackagesArtifact), into an emptied artifacts/packages.
//   5 verify             CiRunPromotion.VerifyPackageSet: exactly one {PackageId}.{Version}.nupkg per
//                        packable project, nothing else.
//   6 push               `dotnet nuget push --skip-duplicate` per package with the OIDC key, so a
//                        partial publish resumes.
// Then template-publish-smoke: wait for nuget.org, generate from the published template, restore and
// build from nuget.org only. A failure fails the release run after the push.
// No clean, build, or test: the packages are the bytes the master run built and tested. Without an
// API key the pipeline stops after verify (a local dry run), and the smoke does not run.
#endregion

namespace DevCli.Commands;

using System.ComponentModel;
using System.Text.Json;

/// <summary>Release mode: tag-gate -> check-version -> locate-run -> download-artifact -> verify -> push -> template-publish-smoke.</summary>
internal sealed class WorkflowTemplateRelease
{
  private const string MasterRef = "origin/master";

  private readonly ITerminal Terminal;
  private readonly IRepoConfigService ConfigService;
  private readonly NuGetVersionService NuGetVersionService;
  private readonly IPackableProjectService PackableProjectService;
  private readonly WorkflowTemplatePackages Packages;
  private readonly string RepoRoot;
  private readonly CancellationToken Ct;

  public WorkflowTemplateRelease(
    ITerminal terminal,
    IRepoConfigService configService,
    NuGetVersionService nuGetVersionService,
    IPackableProjectService packableProjectService,
    WorkflowTemplatePackages packages,
    string repoRoot,
    CancellationToken ct)
  {
    Terminal = terminal;
    ConfigService = configService;
    NuGetVersionService = nuGetVersionService;
    PackableProjectService = packableProjectService;
    Packages = packages;
    RepoRoot = repoRoot;
    Ct = ct;
  }

  public async Task RunAsync(string? apiKey, string? attestationMode)
  {
    Terminal.WriteLine("Pipeline: tag-gate -> check-version -> locate-run -> download-artifact -> verify -> push -> template-publish-smoke\n");
    Environment.ExitCode = 0;

    Step(1, "Release gate");
    string? version = PropsVersionReader.Read(RepoRoot);
    if (string.IsNullOrWhiteSpace(version))
    {
      Abort("could not read <Version> from source/Directory.Build.props");
      return;
    }

    string? headSha = await GitAsync("rev-parse", "HEAD");
    if (headSha is null || !await TagGateAsync(version, headSha) || !await AttestAsync(attestationMode)) return;

    Step(2, "Check version");
    var checkVersion = new CheckVersionCommand.Handler(Terminal, NuGetVersionService, ConfigService, PackableProjectService);
    await checkVersion.Handle(new CheckVersionCommand(), Ct);
    if (Environment.ExitCode != 0)
    {
      Abort(CheckVersionAbort.FormatAbortReason(checkVersion.Outcome, checkVersion.GitHubPackagesFailure));
      return;
    }

    (IReadOnlyList<PackableProject> projects, string? setVersion) = await Packages.ReadSetAsync();
    if (setVersion is null)
    {
      Abort("no packable set");
      return;
    }

    Step(3, "Locate CI run");
    IReadOnlyList<CiRunSummary>? runs = await LocateRunsAsync(headSha);
    if (runs is null) return;

    Step(4, "Download artifact");
    if (!await DownloadAsync(runs, headSha)) return;

    Step(5, "Verify package set");
    if (!Packages.Verify(projects, setVersion))
    {
      Abort($"the downloaded Packages-* does not match the packable set. A run from before the version bump has the old version: re-run CI on {Short(headSha)}");
      return;
    }

    Step(6, "Push to nuget.org");
    string? key = string.IsNullOrWhiteSpace(apiKey) ? Environment.GetEnvironmentVariable("NUGET_API_KEY") : apiKey;
    if (string.IsNullOrWhiteSpace(key))
    {
      Terminal.WriteLine("No API key supplied: verified, not pushed. The template-publish-smoke gate is skipped.".Yellow());
      Terminal.WriteLine("\nPipeline SUCCEEDED".Green());
      return;
    }

    if (!await PushAsync(projects, setVersion, key)) return;

    if (!await WorkflowCommand.RunStepAsync(Terminal, "Template publish smoke", new TemplatePublishSmokeCommand.Handler(Terminal).Handle(new TemplatePublishSmokeCommand(), Ct))) return;

    Terminal.WriteLine("\nPipeline SUCCEEDED — packages published to nuget.org".Green());
  }

  private async Task<bool> TagGateAsync(string version, string headSha)
  {
    if (Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME") == "release")
    {
      TagAssertionResult tag = TagAssertion.Validate(Environment.GetEnvironmentVariable("GITHUB_REF_NAME"), version);
      if (!tag.IsValid) return Abort($"release tag does not match the source version: {tag.Error}");
      Terminal.WriteLine($"Tag assertion passed: {tag.ExpectedTag}");
    }
    else
    {
      Terminal.WriteLine("Tag assertion skipped: not a release event (break-glass or local). The tag pin still applies.");
    }

    string pin = $"v{version}";
    CommandOutput pinned = await Git("rev-parse", "-q", "--verify", $"refs/tags/{pin}^{{commit}}").CaptureAsync(Ct);
    if (pinned.Success)
    {
      string tagSha = pinned.Stdout.Trim();
      if (tagSha != headSha) return Abort($"tag {pin} is at {Short(tagSha)} and this run is at {Short(headSha)}. A partial-publish resume runs from the tag's commit; changed source needs a version bump");
      Terminal.WriteLine($"Tag pin passed: HEAD is at {pin}.");
    }
    else if (!string.IsNullOrWhiteSpace(pinned.Stderr))
    {
      return Abort($"tag pin check failed: {pinned.Stderr.Trim()}");
    }
    else
    {
      Terminal.WriteLine($"Tag pin: {pin} not yet tagged.");
    }

    if (await GitAsync("rev-parse", "--verify", MasterRef) is null) return Abort($"cannot resolve {MasterRef}; the release checkout needs full history (fetch-depth: 0)");

    CommandOutput ancestor = await Git("merge-base", "--is-ancestor", "HEAD", MasterRef).CaptureAsync(Ct);
    return ancestor.ExitCode switch
    {
      0 => true,
      1 => Abort($"HEAD is not an ancestor of {MasterRef}. Releases are cut from master commits"),
      _ => Abort($"ancestor check failed: {ancestor.Stderr.Trim()}"),
    };
  }

  private async Task<bool> AttestAsync(string? cliMode)
  {
    RepoConfig config = await ConfigService.GetConfigAsync(Ct);
    bool ok = await WorkflowTemplateRepository.RunAttestationAsync(Terminal, RepoRoot, cliMode ?? config.Attestation?.Mode, AttestationConfigResolver.DefaultReleaseMode, Ct);
    return ok || Abort("attestation required (mode=require) and not valid");
  }

  private async Task<IReadOnlyList<CiRunSummary>?> LocateRunsAsync(string headSha)
  {
    CommandOutput listed;
    try
    {
      listed = await Gh.Run().List()
        .WithWorkflow("workflow.yml")
        .WithCommit(headSha)
        .WithStatus("success")
        .WithJson("databaseId", "event", "headSha", "createdAt")
        .WithNoValidation()
        .CaptureAsync(Ct);
    }
    catch (Win32Exception)
    {
      Abort("the gh CLI could not be launched. On a runner workflow.yml sets GH_TOKEN; locally install gh and run `gh auth login`");
      return null;
    }

    if (!listed.Success)
    {
      Abort($"gh run list failed: {listed.Stderr.Trim()}");
      return null;
    }

    IReadOnlyList<CiRunSummary> runs = CiRunPromotion.OrderCandidateRuns(JsonSerializer.Deserialize(listed.Stdout, DevCliJsonContext.Default.ListCiRunSummary) ?? [], headSha);
    if (runs.Count == 0)
    {
      Abort($"no successful non-pull-request workflow.yml run at {Short(headSha)}. Only CI-tested packages ship: let the master run pass, or `gh run rerun <run-id>`");
      return null;
    }

    Terminal.WriteLine($"Found {runs.Count} candidate run(s) at {Short(headSha)}: {string.Join(", ", runs.Select(run => $"{run.DatabaseId} ({run.Event})"))}");
    return runs;
  }

  private async Task<bool> DownloadAsync(IReadOnlyList<CiRunSummary> runs, string headSha)
  {
    List<string> expired = [];
    foreach (CiRunSummary run in runs)
    {
      IReadOnlyList<RunArtifact>? artifacts = await Packages.ListRunArtifactsAsync(run.DatabaseId);
      if (artifacts is null) return Abort($"could not list the artifacts of run {run.DatabaseId}");

      PackagesArtifactOutcome selected = CiRunPromotion.SelectPackagesArtifact(artifacts);
      if (selected.Status == PackagesArtifactStatus.Expired) expired.Add($"run {run.DatabaseId}: {string.Join(", ", selected.ExpiredNames)}");
      if (selected.Status != PackagesArtifactStatus.Found) continue;

      return await Packages.DownloadAsync(run.DatabaseId, selected.Artifact!.Name) || Abort("download failed");
    }

    return expired.Count > 0
      ? Abort($"every candidate's Packages-* has expired ({string.Join("; ", expired)}). Make a fresh one: gh run rerun {runs[0].DatabaseId}")
      : Abort($"no candidate run at {Short(headSha)} uploaded a Packages-* artifact. Make one: gh run rerun {runs[0].DatabaseId}");
  }

  private async Task<bool> PushAsync(IReadOnlyList<PackableProject> projects, string version, string apiKey)
  {
    foreach (PackableProject project in projects)
    {
      string nupkg = Path.Combine(Packages.PackagesDirectory, $"{project.PackageId}.{version}.nupkg");
      Terminal.WriteLine($"Pushing {Path.GetFileName(nupkg)}...");
      int exitCode = await DotNet.NuGet()
        .WithWorkingDirectory(RepoRoot)
        .Push(nupkg)
        .WithApiKey(apiKey)
        .WithSource("https://api.nuget.org/v3/index.json")
        .WithSkipDuplicate()
        .RunAsync(Ct);

      if (exitCode != 0) return Abort($"push of {project.PackageId} failed (exit {exitCode}). Packages already pushed stay; re-run the release job to resume (--skip-duplicate)");
    }

    Terminal.WriteLine("All packages pushed.".Green());
    return true;
  }

  private void Step(int number, string title) =>
    Terminal.WriteLine($"\n=== Step {number}/6: {title} ===".Cyan());

  private bool Abort(string reason)
  {
    Terminal.WriteErrorLine($"\nPipeline ABORTED — {reason}.".Red());
    Environment.ExitCode = 1;
    return false;
  }

  private async Task<string?> GitAsync(params string[] arguments)
  {
    CommandOutput output = await Git(arguments).CaptureAsync(Ct);
    return output.Success ? output.Stdout.Trim() : null;
  }

  private ShellBuilder Git(params string[] arguments) =>
    Shell.Builder("git").WithArguments(arguments).WithWorkingDirectory(RepoRoot).WithNoValidation();

  private static string Short(string sha) => sha.Length > 7 ? sha[..7] : sha;
}
