#region Purpose
// Runs the green-key ledger for `dev workflow --phase classify`: computes the key, reads the force
// trailer, and looks up each needed gate's marker with `gh api`.
#endregion

#region Design
// The rules are services/green-key.cs (GreenKey); this file only gathers facts from git, dotnet, gh,
// the event payload, and the runner. Lookup lists artifacts by exact name (one `gh api` call per gate
// the path rules turned on), which needs `actions: read` and GH_TOKEN on the classify job. A run:
// step has no ACTIONS_RUNTIME_TOKEN, so this never writes an artifact: the workflow's green-key job
// uploads markers after the gated jobs pass, keyed by the green_key output this run writes.
// Any failure (no key, gh missing or refused) is logged and reads as a miss, so the gate runs: a
// ledger problem can cost a rebuild but never skips one. Every line starts with green-key-v1 and
// also goes to GITHUB_STEP_SUMMARY.
#endregion

namespace DevCli.Services;

using System.ComponentModel;
using System.Security.Cryptography;

/// <summary>Gathers the green-key facts for one classify run.</summary>
internal static class GreenKeyLedger
{
  private const string ArtifactJq = ".artifacts[] | {id,expired,created_at,run_id:.workflow_run.id,head_sha:.workflow_run.head_sha}";

  /// <summary>
  /// Computes the key and the gates that already passed on it. Only gates <paramref name="pathJobs"/>
  /// turned on are looked up.
  /// </summary>
  internal static async Task<GreenKeyObservation> ObserveAsync(
    ITerminal terminal,
    string repoRoot,
    WorkflowJobs pathJobs,
    CancellationToken ct)
  {
    string runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID") ?? "local";
    try
    {
      return await ObserveCoreAsync(terminal, repoRoot, pathJobs, runId, ct);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or Win32Exception)
    {
      Log(terminal, $"{GreenKey.FormatId} error={OneLine(exception.Message)} run={runId}. Every gate runs.");
      return GreenKeyObservation.None;
    }
  }

  private static async Task<GreenKeyObservation> ObserveCoreAsync(
    ITerminal terminal,
    string repoRoot,
    WorkflowJobs pathJobs,
    string runId,
    CancellationToken ct)
  {
    string? eventPath = Environment.GetEnvironmentVariable("GITHUB_EVENT_PATH");
    string? eventJson = File.Exists(eventPath) ? await File.ReadAllTextAsync(eventPath, ct) : null;
    string? repository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
    GreenKeyEventClass eventClass = GreenKey.ClassifyEvent(
      Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME"),
      repository,
      GreenKey.ReadPullRequestHead(eventJson, "repo"));

    CommandOutput listed = await Git(repoRoot, "ls-tree", "-r", "HEAD").CaptureAsync(ct);
    if (!listed.Success)
    {
      Log(terminal, $"{GreenKey.FormatId} error=git-ls-tree-failed run={runId}. Every gate runs.");
      return GreenKeyObservation.None;
    }

    IReadOnlyList<GreenKeyEntry> tree = GreenKey.ParseLsTree(listed.Stdout);
    string sdk = await SdkVersionAsync(ct);
    string runner = GreenKey.RunnerDescription(
      Environment.GetEnvironmentVariable("GREEN_KEY_RUNNER_IMAGE"),
      Environment.GetEnvironmentVariable("RUNNER_ENVIRONMENT"),
      Environment.GetEnvironmentVariable("ImageOS"),
      Environment.GetEnvironmentVariable("ImageVersion"),
      await DpkgStatusHashAsync(ct));
    string key = GreenKey.Hash(GreenKey.Manifest(sdk, runner, tree));

    // The force trailer lives on the pull request head, not the synthetic merge commit.
    string head = GreenKey.ReadPullRequestHead(eventJson, "sha") ?? "HEAD";
    CommandOutput trailers = await Git(repoRoot, "log", "-1", $"--format=%(trailers:key={GreenKey.ForceTrailerKey},valueonly)", head).CaptureAsync(ct);
    bool force = trailers.Success && GreenKey.IsForceFullRun(trailers.Stdout);

    Log(terminal, $"{GreenKey.FormatId} event={eventClass.Reason} candidate={Bool(eventClass.Candidate)} force={Bool(force)} key={key} run={runId}");
    Log(terminal, $"{GreenKey.FormatId} sdk={sdk} runner={runner} tree={tree.Count}");

    List<string> hits = [];
    if (eventClass.Candidate && !force && !string.IsNullOrWhiteSpace(repository))
    {
      (string Gate, bool Needed)[] gates = [(GreenKey.GateCi, pathJobs.Ci), (GreenKey.GateSkill, pathJobs.Skill), (GreenKey.GateTemplate, pathJobs.Template)];
      foreach ((string gate, bool needed) in gates)
      {
        if (!needed)
        {
          Log(terminal, $"{GreenKey.FormatId} gate={gate} lookup=not-needed");
          continue;
        }

        (string lookup, GreenKeyMarker? marker) = await FindAsync(repository, GreenKey.ArtifactName(gate, key), ct);
        if (marker is null)
        {
          Log(terminal, $"{GreenKey.FormatId} gate={gate} lookup={lookup} earlier=miss. {gate} runs.");
          continue;
        }

        hits.Add(gate);
        Log(terminal, $"{GreenKey.FormatId} gate={gate} lookup={lookup} earlier=hit trusted-run={marker.RunId} trusted-commit={marker.Commit}. {gate} skips; add the trailer `{GreenKey.ForceTrailerKey}: {GreenKey.ForceTrailerValue}` to run it.");
      }
    }

    return new GreenKeyObservation(key, eventClass.Candidate, hits);
  }

  // Exact-name artifact list. lookup is ok, failed (gh exited non-zero), or unavailable (no gh).
  private static async Task<(string Lookup, GreenKeyMarker? Marker)> FindAsync(string repository, string name, CancellationToken ct)
  {
    CommandOutput listed;
    try
    {
      listed = await Gh.Api($"repos/{repository}/actions/artifacts?per_page=100&name={Uri.EscapeDataString(name)}")
        .WithPaginate()
        .WithJq(ArtifactJq)
        .WithNoValidation()
        .CaptureAsync(ct);
    }
    catch (Win32Exception)
    {
      return ("unavailable", null);
    }

    if (!listed.Success) return ("failed", null);

    return ("ok", GreenKey.SelectMarker(listed.Stdout.Split('\n')));
  }

  private static async Task<string> SdkVersionAsync(CancellationToken ct)
  {
    try
    {
      CommandOutput version = await DotNet.WithVersion().WithNoValidation().CaptureAsync(ct);
      return version.Success && !string.IsNullOrWhiteSpace(version.Stdout) ? version.Stdout.Trim() : "unknown";
    }
    catch (Win32Exception)
    {
      return "unknown";
    }
  }

  private static async Task<string?> DpkgStatusHashAsync(CancellationToken ct)
  {
    const string statusPath = "/var/lib/dpkg/status";
    if (!File.Exists(statusPath)) return null;

    await using FileStream stream = File.OpenRead(statusPath);
    return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, ct));
  }

  private static ShellBuilder Git(string repoRoot, params string[] arguments) =>
    Shell.Builder("git").WithArguments(arguments).WithWorkingDirectory(repoRoot).WithNoValidation();

  private static void Log(ITerminal terminal, string line)
  {
    terminal.WriteLine(line);
    string? summary = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
    if (!string.IsNullOrWhiteSpace(summary)) File.AppendAllText(summary, $"`{line}`\n\n");
  }

  private static string Bool(bool value) => value ? "true" : "false";

  private static string OneLine(string value) => value.Replace('\r', ' ').Replace('\n', ' ');
}

/// <summary>
/// One classify run's ledger result. <c>Key</c> is empty when none was computed; <c>Writer</c> lets
/// the green-key job upload markers; <c>HitGates</c> lists the gates that already passed on the key.
/// </summary>
internal sealed record GreenKeyObservation(string Key, bool Writer, IReadOnlyList<string> HitGates)
{
  internal static GreenKeyObservation None { get; } = new("", false, []);
}
