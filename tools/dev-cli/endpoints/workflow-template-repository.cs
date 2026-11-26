#region Purpose
// The template repository's own `dev workflow` paths: the NuGet release, the path-gated classify
// outputs, and the generated-files check.
#endregion
#region Design
// Template-only (task 530). .template.config/template.json excludes this file from generated apps,
// and WorkflowCommand reaches it only inside templateRepository regions. The generated app's
// workflow keeps clean -> build -> test -> publish all and a classify that writes ci alone.
//
// Release: clean -> build -> pack -> push -> template-publish-smoke. It deliberately does not run
// tests: they already ran on the PR/merge that produced master. A release publishes as long as it
// builds. Publishing is gated only by an API key being supplied (--api-key, from OIDC Trusted
// Publishing); without one, pack-only, and the post-publish template-publish-smoke gate is skipped
// (nothing was pushed). A dispatch therefore never publishes, and needs no break-glass confirm
// input. After a real push, template-publish-smoke waits for nuget.org flatcontainer, generates
// against the published template, asserts pins==version, restore+builds nuget.org-only; failure
// blocks the release. PackableProjects includes source/libraries (TimeWarp.Modules,
// TimeWarp.Identity, TimeWarp.402), foundation, TimeWarp.Architecture.{Attributes,Analyzers,
// Generators}, and the template package; single Version from source/Directory.Build.props.
// TimeWarp.Contracts is a PackageReference at the Directory.Packages.props pin. This list does not
// pack it.
//
// Classify writes three outputs:
//   ci        PullRequestBuildClassifier.RequiresBuild (TimeWarp.Nuru.DevCli) with this repo's
//             additions in .timewarp/dev.jsonc pullRequestBuild, over the same changed paths. A
//             push applies the pull request rule to its own diff (DecideAsync builds for every
//             event but pull_request), so a skills-only push skips ci-group as a skills-only pull
//             request does. An unreadable dev.jsonc on a push builds.
//   skill     WorkflowPathClasses.IsSkillPath over the changed paths (gates skill-lint).
//   template  WorkflowPathClasses.IsTemplatePath over the changed paths (gates template-smoke).
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

/// <summary>The template repository's release, classify, and generated-files steps of <c>dev workflow</c>.</summary>
internal sealed class WorkflowTemplateRepository
{
  // The publishable set — single repo version (source/Directory.Build.props). libraries (modules,
  // identity, 402) ship as leaf product packages; foundation-application depends on modules; analyzers/
  // generators/attributes ship as platform compile-time packages (task 092); the template is the
  // dotnet-new package.
  internal static readonly string[] PackableProjects =
  [
    "source/libraries/timewarp-modules/timewarp-modules.csproj",
    "source/libraries/timewarp-identity/timewarp-identity.csproj",
    "source/libraries/timewarp-402/timewarp-402.csproj",
    "source/foundation/foundation-domain/foundation-domain.csproj",
    "source/foundation/foundation-contracts/foundation-contracts.csproj",
    "source/foundation/foundation-application/foundation-application.csproj",
    "source/foundation/foundation-infrastructure/foundation-infrastructure.csproj",
    "source/foundation/foundation-server/foundation-server.csproj",
    "source/analyzers/timewarp-architecture-attributes/timewarp-architecture-attributes.csproj",
    "source/analyzers/timewarp-architecture-convention-analyzers/timewarp-architecture-convention-analyzers.csproj",
    "source/analyzers/timewarp-architecture-analyzers/timewarp-architecture-analyzers.csproj",
    "timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj",
  ];

  private const string BuildConfigFile = ".timewarp/dev.jsonc";
  private const string IngressGoldenDirectory = "goldens/ingress";

  private readonly ITerminal Terminal;
  private readonly IRepoCleanService RepoCleanService;
  private readonly string RepoRoot;
  private readonly CancellationToken Ct;

  public WorkflowTemplateRepository(ITerminal terminal, IRepoCleanService repoCleanService, string repoRoot, CancellationToken ct)
  {
    Terminal = terminal;
    RepoCleanService = repoCleanService;
    RepoRoot = repoRoot;
    Ct = ct;
  }

  /// <summary>Writes ci, skill, and template to GITHUB_OUTPUT. A diff that cannot be read turns every job on.</summary>
  public async Task ClassifyAsync(PullRequestBuildDecision decision)
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
    GreenKeyObservation green = await GreenKeyLedger.ObserveAsync(Terminal, RepoRoot, pathJobs, Ct);
    WorkflowJobs jobs = GreenKey.ApplyHits(pathJobs, green.HitGates);
    await WorkflowCommand.WriteOutputsAsync(
      Terminal,
      [
        $"ci={WorkflowCommand.Bool(jobs.Ci)}",
        $"skill={WorkflowCommand.Bool(jobs.Skill)}",
        $"template={WorkflowCommand.Bool(jobs.Template)}",
        $"{GreenKey.KeyOutputName}={green.Key}",
        $"{GreenKey.WriterOutputName}={WorkflowCommand.Bool(green.Writer && GreenKey.IsKey(green.Key) && (jobs.Ci || jobs.Skill || jobs.Template))}",
      ],
      Ct);
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

  /// <summary>Release: NO test step — publish as long as it builds; post-push template-publish-smoke blocks on real publish.</summary>
  public async Task RunReleaseAsync(string? apiKey)
  {
    Terminal.WriteLine("Pipeline: clean -> build -> pack -> push -> template-publish-smoke\n");
    Environment.ExitCode = 0;

    if (!await WorkflowCommand.RunStepAsync(Terminal, "Clean", new CleanCommand.Handler(Terminal, RepoCleanService).Handle(new CleanCommand(), Ct))) return;
    if (!await WorkflowCommand.RunStepAsync(Terminal, "Build", new BuildCommand.Handler(Terminal).Handle(new BuildCommand(), Ct))) return;
    if (!await PackAsync()) return;

    string? resolvedKey = apiKey ?? Environment.GetEnvironmentVariable("NUGET_API_KEY");
    bool willPublish = !string.IsNullOrWhiteSpace(resolvedKey);
    if (!await PushAsync(apiKey)) return;

    if (willPublish)
    {
      if (!await WorkflowCommand.RunStepAsync(
            Terminal,
            "Template publish smoke",
            new TemplatePublishSmokeCommand.Handler(Terminal)
              .Handle(new TemplatePublishSmokeCommand(), Ct)))
      {
        return;
      }
    }

    Terminal.WriteLine("\nPipeline SUCCEEDED".Green());
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

  private async Task<bool> PackAsync()
  {
    string outputDir = Path.Combine(RepoRoot, "artifacts", "packages");
    Directory.CreateDirectory(outputDir);

    foreach (string relativeProject in PackableProjects)
    {
      Terminal.WriteLine($"\nPacking {relativeProject}...");
      int exitCode = await DotNet.Pack(Path.Combine(RepoRoot, relativeProject))
        .WithConfiguration("Release")
        .WithOutput(outputDir)
        .WithNoValidation()
        .RunAsync(Ct);

      if (exitCode != 0)
      {
        Terminal.WriteErrorLine($"Pack failed for {relativeProject}!".Red());
        Environment.ExitCode = exitCode;
        return false;
      }
    }

    return true;
  }

  private async Task<bool> PushAsync(string? apiKey)
  {
    apiKey ??= Environment.GetEnvironmentVariable("NUGET_API_KEY");
    if (string.IsNullOrWhiteSpace(apiKey))
    {
      Terminal.WriteLine("\nNo API key supplied — pack-only (skipping push).".Yellow());
      return true;
    }

    string glob = Path.Combine(RepoRoot, "artifacts", "packages", "*.nupkg");
    Terminal.WriteLine($"\nPushing {glob} to nuget.org...");

    int exitCode = await DotNet.NuGet()
      .Push(glob)
      .WithApiKey(apiKey)
      .WithSource("https://api.nuget.org/v3/index.json")
      .WithSkipDuplicate()
      .RunAsync(Ct);

    if (exitCode != 0)
    {
      Terminal.WriteErrorLine("Push failed!".Red());
      Environment.ExitCode = exitCode;
      return false;
    }

    return true;
  }
}
