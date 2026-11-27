#region Purpose
// The template repository's own `dev workflow` paths: the path-gated classify outputs (with the
// master Packages-* decision and the attestation check) and the generated-files check.
#endregion
#region Design
// Template-only (task 530). .template.config/template.json excludes this file from generated apps,
// and WorkflowCommand reaches it only inside templateRepository regions. The generated app's
// workflow keeps clean -> build -> test -> publish all and a classify that writes ci alone.
//
// Release mode is WorkflowTemplateRelease (workflow-template-release.cs), and the master pack,
// pack-only, and carry-forward are WorkflowTemplatePackages (workflow-template-packages.cs), both
// task 533. The packable set is IPackableProjectService (ArchitecturePackableProjectService): no
// project list here.
//
// Classify writes these outputs:
//   ci        PullRequestBuildClassifier.RequiresBuild (TimeWarp.Nuru.DevCli) with this repo's
//             additions in .timewarp/dev.jsonc pullRequestBuild, over the same changed paths. A
//             push applies the pull request rule to its own diff (DecideAsync builds for every
//             event but pull_request), so a skills-only push skips ci-group as a skills-only pull
//             request does. An unreadable dev.jsonc on a push builds.
//   skill     WorkflowPathClasses.IsSkillPath over the changed paths (gates skill-lint).
//   template  WorkflowPathClasses.IsTemplatePath over the changed paths (gates template-smoke).
//   ci_legs   WorkflowPathClasses.ClassifyCiLegs (task 534): the JSON list of ci-group matrix legs.
//             ci (fast) always; ci (integration) and ci (publish) unless a pull request changed
//             only fast-only dev CLI paths and non-build paths. Every other event runs every leg.
//   ci_marker true only when ci is on and every leg runs: workflow.yml uploads the ci green-key
//             marker only then, so a pull request that skipped a leg cannot let the master push
//             skip ci-group. The writer passed to GreenKey.OutputLines counts ci only then too.
//   packages, pack, carry_run  ReleasePackages.OutputLines (services/release-packages.cs): whether
//             this run publishes Packages-*, whether pack-only runs (ci-group off), and the earlier
//             run pack-only carries forward (WorkflowTemplatePackages.FindCarryRunAsync).
// Attestation: classify verifies the ganda-audit note on HEAD's tree once per run (RunAttestationAsync,
// the DevCli AttestationVerifyRunner). Mode: --attestation, then dev.jsonc attestation.mode, else warn.
// Warn prints an advisory; require fails classify, so ci fails. Release mode calls the same method
// with require as the default.
// Green key (services/green-key.cs, services/green-key-ledger.cs): classify then turns off each of
// ci, skill, and template whose gate already passed on this tree, and writes green_key and
// green_key_writer (a candidate with a key and a gate left on) for workflow.yml's green-key job,
// which uploads the markers.
// A pull request takes the classifier's base...head diff (the job checks out full history). A push
// fetches its before and after commits when missing (--depth=1 only on a shallow checkout, so a
// local full clone never turns shallow) and diffs their trees. Any diff that cannot be read
// (new branch, force push, failed fetch, local run) turns every job on.
//
// Generated files: `dev regenerate-generated` rewrites the feature filename grammar outputs and the
// ingress prefix golden, and goldens/ingress plus the four grammar outputs must show no diff and no
// untracked file. A hand-edited output, or a grammar JSON or contract change that skipped
// regenerate, fails here.
#endregion

namespace DevCli.Commands;

using System.Text.Json;

/// <summary>The template repository's classify and generated-files steps of <c>dev workflow</c>.</summary>
internal sealed class WorkflowTemplateRepository
{
  private const string BuildConfigFile = ".timewarp/dev.jsonc";
  private const string IngressGoldenDirectory = "goldens/ingress";

  private readonly ITerminal Terminal;
  private readonly string RepoRoot;
  private readonly CancellationToken Ct;

  public WorkflowTemplateRepository(ITerminal terminal, string repoRoot, CancellationToken ct)
  {
    Terminal = terminal;
    RepoRoot = repoRoot;
    Ct = ct;
  }

  /// <summary>Writes ci, skill, template, and the ci-group legs to GITHUB_OUTPUT. A diff that cannot be read turns every job on.</summary>
  public async Task ClassifyAsync(PullRequestBuildDecision decision, string? attestationMode)
  {
    IReadOnlyList<string>? changed = await ReadChangedPathsAsync(decision);
    if (changed is null)
    {
      Terminal.WriteLine("Changed paths unknown. Every path-gated job runs.");
    }
    else
    {
      Terminal.WriteLine($"Changed paths ({changed.Count}):");
      foreach (string path in changed) Terminal.WriteLine($"  {path}");
    }

    (bool configRead, PullRequestBuildConfig? config) = changed is null ? (true, null) : await ReadBuildConfigAsync();
    WorkflowJobs pathJobs = WorkflowPathClasses.Classify(
      changed,
      paths => !configRead || PullRequestBuildClassifier.RequiresBuild(paths, config));
    bool pullRequest = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME") == PullRequestBuildClassifier.PullRequestEventName;
    CiLegs legs = WorkflowPathClasses.ClassifyCiLegs(
      changed,
      pullRequest,
      paths => !configRead || PullRequestBuildClassifier.RequiresBuild(paths, config));
    GreenKeyObservation green = await GreenKeyLedger.ObserveAsync(Terminal, RepoRoot, pathJobs, Ct);
    WorkflowJobs jobs = GreenKey.ApplyHits(pathJobs, green.HitGates);
    if (jobs.Ci && !legs.Full) Terminal.WriteLine($"ci-group legs: {string.Join(", ", legs.Names)} (no ci green-key marker).");

    bool stages = ReleasePackages.StagesPackages(Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME"), Environment.GetEnvironmentVariable("GITHUB_REF"));
    long? carryRun = stages && !jobs.Ci ? await WorkflowTemplatePackages.FindCarryRunAsync(Terminal, RepoRoot, green.Key, Ct) : null;

    string? mode = attestationMode ?? await ReadAttestationModeAsync();
    if (!await RunAttestationAsync(Terminal, RepoRoot, mode, AttestationConfigResolver.DefaultPrMode, Ct))
    {
      Terminal.WriteErrorLine("\nPipeline FAILED — attestation required (mode=require) and not valid".Red());
      Environment.ExitCode = 1;
      return;
    }

    await WorkflowCommand.WriteOutputsAsync(
      Terminal,
      [
        $"ci={WorkflowCommand.Bool(jobs.Ci)}",
        $"skill={WorkflowCommand.Bool(jobs.Skill)}",
        $"template={WorkflowCommand.Bool(jobs.Template)}",
        .. legs.OutputLines(jobs.Ci),
        .. GreenKey.OutputLines(green, jobs with { Ci = jobs.Ci && legs.Full }),
        .. ReleasePackages.OutputLines(stages, jobs.Ci, carryRun),
      ],
      Ct);
  }

  /// <summary>
  /// Verifies the ganda-audit note on HEAD's tree under <paramref name="rawMode"/> (null or blank:
  /// <paramref name="whenUnset"/>). False only when the mode is require and the note is not valid.
  /// </summary>
  internal static async Task<bool> RunAttestationAsync(ITerminal terminal, string repoRoot, string? rawMode, AttestationMode whenUnset, CancellationToken ct)
  {
    AttestationModeResolution resolution = AttestationConfigResolver.ResolveMode(rawMode);
    AttestationMode mode = AttestationConfigResolver.EffectiveMode(resolution, whenUnset);
    if (resolution.UnrecognizedValue is not null)
    {
      terminal.WriteLine($"Warning: unrecognized attestation mode '{resolution.UnrecognizedValue}', treated as {whenUnset.ToString().ToLowerInvariant()}. Valid: off, warn, require.".Yellow());
    }

    if (mode == AttestationMode.Off)
    {
      terminal.WriteLine("Attestation skipped (mode=off).");
      return true;
    }

    AttestationVerifyOutcome outcome;
    try
    {
      outcome = await AttestationVerifyRunner.VerifyAsync(repoRoot, "HEAD", keyOverride: null, ct);
    }
    catch (InvalidOperationException exception)
    {
      terminal.WriteErrorLine(exception.Message.Red());
      return mode != AttestationMode.Require;
    }

    string message = AttestationVerifyRunner.Describe(outcome);
    if (outcome.Status == AttestationVerificationStatus.Valid)
    {
      terminal.WriteLine(message.Green());
      return true;
    }

    if (mode == AttestationMode.Require)
    {
      terminal.WriteErrorLine($"Attestation required (mode=require): {message}".Red());
      return false;
    }

    terminal.WriteLine($"Attestation advisory (mode=warn, not enforced): {message}".Yellow());
    terminal.WriteLine("Set attestation.mode to require in .timewarp/dev.jsonc, or pass --attestation require, to enforce it.".Yellow());
    return true;
  }

  /// <summary>
  /// Regenerates the checked-in generated files, then fails on any tracked diff or untracked file under them.
  /// </summary>
  public async Task<bool> VerifyGeneratedFilesAsync()
  {
    if (!await WorkflowCommand.RunStepAsync(Terminal, "Regenerate generated files", new RegenerateGeneratedCommand.Handler(Terminal).Handle(new RegenerateGeneratedCommand(), Ct))) return false;

    string[] paths = [IngressGoldenDirectory, .. FeatureFilenameGrammarGenerator.OutputRelativePaths];
    CommandOutput diff = await GitCommand(["diff", "--exit-code", "--", .. paths]).PassthroughAsync(Ct);
    CommandOutput status = await GitCommand(["status", "--porcelain", "--", .. paths]).CaptureAsync(Ct);
    if (diff.Success && status.Success && string.IsNullOrWhiteSpace(status.Stdout)) return true;

    Terminal.WriteErrorLine($"Generated files changed after regenerate-generated ({string.Join(", ", paths)}). Run `dev regenerate-generated` and commit the result.".Red());
    if (!string.IsNullOrWhiteSpace(status.Stdout)) Terminal.WriteErrorLine(status.Stdout);
    Terminal.WriteErrorLine("\nPipeline FAILED — Generated files check failed".Red());
    Environment.ExitCode = 1;
    return false;
  }

  // attestation.mode from .timewarp/dev.jsonc; null (the context default) when absent or unreadable.
  private async Task<string?> ReadAttestationModeAsync()
  {
    string path = Path.Combine(RepoRoot, BuildConfigFile);
    if (!File.Exists(path)) return null;

    try
    {
      return RepoConfigService.Parse(await File.ReadAllTextAsync(path, Ct)).Attestation?.Mode;
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
    {
      Terminal.WriteErrorLine($"{BuildConfigFile} could not be read ({exception.Message}). Attestation uses its default.");
      return null;
    }
  }

  // pullRequestBuild from .timewarp/dev.jsonc, the same additions DecideAsync reads. False when
  // the file exists but cannot be read: the caller then builds.
  private async Task<(bool Read, PullRequestBuildConfig? Config)> ReadBuildConfigAsync()
  {
    string path = Path.Combine(RepoRoot, BuildConfigFile);
    if (!File.Exists(path)) return (true, null);

    try
    {
      return (true, RepoConfigService.Parse(await File.ReadAllTextAsync(path, Ct)).PullRequestBuild);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
    {
      Terminal.WriteErrorLine($"{BuildConfigFile} could not be read ({exception.Message}). ci runs.");
      return (false, null);
    }
  }

  // Pull request: the classifier's diff (empty with a build decision means it was not read).
  // Push: fetch before and after (the checkout is shallow) and diff their trees. Else unknown.
  private async Task<IReadOnlyList<string>?> ReadChangedPathsAsync(PullRequestBuildDecision decision)
  {
    string? eventName = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME");
    if (eventName == PullRequestBuildClassifier.PullRequestEventName)
    {
      return !decision.RequiresBuild || decision.ChangedPaths.Count > 0 ? decision.ChangedPaths : null;
    }

    if (eventName != "push") return null;

    string? eventPath = Environment.GetEnvironmentVariable("GITHUB_EVENT_PATH");
    string? json = File.Exists(eventPath) ? await File.ReadAllTextAsync(eventPath, Ct) : null;
    if (!WorkflowPathClasses.TryReadPushShas(json, out string before, out string after)) return null;

    if (!await HasCommitsAsync(before, after))
    {
      // --depth only on a shallow checkout (CI); on a full clone it would make the clone shallow.
      CommandOutput shallow = await GitCommand("rev-parse", "--is-shallow-repository").CaptureAsync(Ct);
      string[] fetch = shallow.Stdout.Trim() == "true"
        ? ["fetch", "--no-tags", "--depth=1", "origin", before, after]
        : ["fetch", "--no-tags", "origin", before, after];
      if (!(await GitCommand(fetch).CaptureAsync(Ct)).Success) return null;
    }

    CommandOutput diff = await GitCommand("diff", "--name-only", before, after).CaptureAsync(Ct);
    if (!diff.Success) return null;

    return diff.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
  }

  private async Task<bool> HasCommitsAsync(params string[] shas)
  {
    foreach (string sha in shas)
    {
      if (!(await GitCommand("cat-file", "-e", sha + "^{commit}").CaptureAsync(Ct)).Success) return false;
    }

    return true;
  }

  private ShellBuilder GitCommand(params string[] arguments) =>
    Shell.Builder("git").WithArguments(arguments).WithWorkingDirectory(RepoRoot).WithNoValidation();
}
