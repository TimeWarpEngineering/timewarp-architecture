#region Purpose
// Green-key ledger rules for `dev workflow --phase classify`: which tracked paths the key hashes, the
// manifest and its hash, the marker artifact name per gate, the force trailer, which events may skip,
// and which path-gated jobs a marker hit turns off.
#endregion

#region Design
// Ported from timewarp-ganda tasks 469 and 470 (GreenKeyManifest, GreenKeyEvent, GreenKeySkip,
// GreenKeyMarkers), cut to what this workflow needs. Pure rules (no Amuru, no git), so dev-cli-tests
// Compile-include this file; services/green-key-ledger.cs runs git and gh.
//
// Key: SHA-256 (lowercase hex) of a UTF-8, LF manifest:
//   green-key-v1
//   sdk <dotnet --version>
//   runner <runner description>
//   tree <path> <git object sha>   (one per key input from `git ls-tree -r HEAD`, ordinal path order)
// A layout change bumps FormatId, so old markers cannot hit. Key inputs are every tracked blob and
// submodule entry except kanban task content: a file under a kanban column folder or
// kanban/task-examples/. The template packs kanban/*.md and the column .gitkeep files, so those stay
// inputs. That exclusion is what lets a master merge hit: the walk lands kanban commits after the
// pull request run, and the tree otherwise matches. Everything else (documentation/, skills/, the
// workflow, global.json) stays an input, because some CI job reads it.
//
// Runner: GREEN_KEY_RUNNER_IMAGE when set. Otherwise RUNNER_ENVIRONMENT plus, on a GitHub-hosted
// runner, ImageOS and ImageVersion, and on a self-hosted (wslc) runner the SHA-256 of
// /var/lib/dpkg/status, the way ganda describes an image that publishes no id. It describes the
// classify job's runner, which is every gated job's runner except ci (integration): that leg needs
// Docker and always runs hosted, so its image is not in the key. The workflow pulls no container
// image by tag (Testcontainers' postgres tag is a string in the tree), so there is no image line.
//
// Gates: ci (the ci-group matrix), skill (skill-lint), template (template-smoke). A marker is a
// GitHub Actions artifact named green-key-v1-<gate>-<key>. Artifacts are repo-scoped, so a master
// push sees a pull request's marker (the Actions cache is branch-scoped and would not). Each gate
// runs the whole of its check over the tree, whatever the diff, so a marker means "this gate passed
// on this tree" and a later run of the same tree may skip that gate.
//
// Candidates: a push (the workflow pushes only master and feature/**) and a same-repo pull request.
// Only a candidate looks up or writes a marker. A fork pull request never skips and never writes
// (fork artifacts land in the base repo). release, schedule, and workflow_dispatch are excluded, so
// a manual dispatch (mode full) is always a full run.
// Force trailer: `Green-Key: full-run` on the head commit (pull_request.head.sha for a pull request,
// not the synthetic merge commit) turns lookups off. That run still writes markers when it passes.
#endregion

namespace DevCli.Services;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

/// <summary>Green-key manifest, marker, and skip rules for <c>dev workflow --phase classify</c>.</summary>
internal static class GreenKey
{
  internal const string FormatId = "green-key-v1";
  internal const string ForceTrailerKey = "Green-Key";
  internal const string ForceTrailerValue = "full-run";

  internal const string GateCi = "ci";
  internal const string GateSkill = "skill";
  internal const string GateTemplate = "template";

  /// <summary>GITHUB_OUTPUT name of the key. Empty when no key was computed.</summary>
  internal const string KeyOutputName = "green_key";

  /// <summary>GITHUB_OUTPUT name that lets the green-key job upload markers (true or false).</summary>
  internal const string WriterOutputName = "green_key_writer";

  private const string KanbanRoot = "kanban/";

  private static readonly string[] KanbanColumns = ["backlog", "to-do", "in-progress", "done", "archived"];

  /// <summary>True when <paramref name="path"/> (repo-relative) is hashed into the key.</summary>
  internal static bool IsKeyInputPath(string path)
  {
    path = path.Replace('\\', '/').Trim();
    if (path.StartsWith("./", StringComparison.Ordinal)) path = path[2..];
    if (path.Length == 0) return false;
    if (!path.StartsWith(KanbanRoot, StringComparison.Ordinal)) return true;

    string[] segments = path[KanbanRoot.Length..].Split('/');
    if (segments.Length == 1) return true; // kanban/overview.md, readme.md, task-template.md

    return segments.Length == 2 && KanbanColumns.Contains(segments[0], StringComparer.Ordinal) && segments[1] == ".gitkeep";
  }

  /// <summary>Key inputs from <c>git ls-tree -r HEAD</c> output: blob and submodule entries only.</summary>
  internal static IReadOnlyList<GreenKeyEntry> ParseLsTree(string? stdout)
  {
    List<GreenKeyEntry> entries = [];
    string text = (stdout ?? "").Replace("\r\n", "\n", StringComparison.Ordinal);
    foreach (string line in text.Split('\n'))
    {
      int tab = line.IndexOf('\t', StringComparison.Ordinal);
      if (tab <= 0 || tab == line.Length - 1) continue;

      string[] meta = line[..tab].Split(' ');
      string path = line[(tab + 1)..];
      if (meta.Length < 3 || meta[1] is not ("blob" or "commit") || !IsKeyInputPath(path)) continue;

      entries.Add(new GreenKeyEntry(path, meta[2]));
    }

    return entries;
  }

  /// <summary>The canonical manifest. Tree lines are sorted by ordinal path, so input order does not matter.</summary>
  internal static string Manifest(string sdkVersion, string runner, IEnumerable<GreenKeyEntry> tree)
  {
    ArgumentNullException.ThrowIfNull(tree);
    StringBuilder builder = new();
    builder.Append(FormatId).Append('\n');
    builder.Append("sdk ").Append(OneLine(sdkVersion, "unknown")).Append('\n');
    builder.Append("runner ").Append(OneLine(runner, "unknown")).Append('\n');
    foreach (GreenKeyEntry entry in tree.OrderBy(entry => entry.Path, StringComparer.Ordinal))
    {
      builder.Append("tree ").Append(entry.Path).Append(' ').Append(entry.Blob).Append('\n');
    }

    return builder.ToString();
  }

  /// <summary>Lowercase SHA-256 hex of <paramref name="manifest"/>.</summary>
  internal static string Hash(string manifest)
  {
    ArgumentNullException.ThrowIfNull(manifest);
    return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(manifest)));
  }

  /// <summary>True for a 64-character hex digest.</summary>
  internal static bool IsKey(string? key) => key is { Length: 64 } && key.All(char.IsAsciiHexDigit);

  /// <summary>Marker artifact name for one gate and key.</summary>
  internal static string ArtifactName(string gate, string key) => $"{FormatId}-{gate}-{key.ToLowerInvariant()}";

  /// <summary>The runner line. See the Design region for the order of sources.</summary>
  internal static string RunnerDescription(
    string? overrideImage,
    string? environment,
    string? imageOs,
    string? imageVersion,
    string? dpkgStatusSha256)
  {
    if (!string.IsNullOrWhiteSpace(overrideImage)) return OneLine(overrideImage, "unknown");

    List<string> parts = [OneLine(environment, "unknown-environment")];
    if (!string.IsNullOrWhiteSpace(imageVersion)) parts.Add($"image:{OneLine(imageOs, "unknown-os")}:{OneLine(imageVersion, "")}");
    if (!string.IsNullOrWhiteSpace(dpkgStatusSha256)) parts.Add("dpkg:" + OneLine(dpkgStatusSha256, "").ToLowerInvariant());
    return string.Join('|', parts);
  }

  /// <summary>
  /// Whether this event may look up and write markers. <paramref name="headRepository"/> is
  /// pull_request.head.repo.full_name (null when the event has none).
  /// </summary>
  internal static GreenKeyEventClass ClassifyEvent(string? eventName, string? repository, string? headRepository) =>
    eventName switch
    {
      "push" => new GreenKeyEventClass(true, "push"),
      "pull_request" when !string.IsNullOrEmpty(repository) && string.Equals(headRepository, repository, StringComparison.Ordinal)
        => new GreenKeyEventClass(true, "same-repo-pull-request"),
      "pull_request" => new GreenKeyEventClass(false, "fork-pull-request"),
      null or "" => new GreenKeyEventClass(false, "local"),
      _ => new GreenKeyEventClass(false, eventName),
    };

  /// <summary>Reads pull_request.head.<paramref name="property"/> (sha) or head.repo.full_name (property "repo").</summary>
  internal static string? ReadPullRequestHead(string? json, string property)
  {
    if (string.IsNullOrWhiteSpace(json)) return null;

    try
    {
      using var document = JsonDocument.Parse(json);
      if (document.RootElement.ValueKind != JsonValueKind.Object
        || !document.RootElement.TryGetProperty("pull_request", out JsonElement pullRequest)
        || pullRequest.ValueKind != JsonValueKind.Object
        || !pullRequest.TryGetProperty("head", out JsonElement head)
        || head.ValueKind != JsonValueKind.Object)
      {
        return null;
      }

      JsonElement value;
      if (property == "repo")
      {
        if (!head.TryGetProperty("repo", out JsonElement repo) || repo.ValueKind != JsonValueKind.Object
          || !repo.TryGetProperty("full_name", out value))
        {
          return null;
        }
      }
      else if (!head.TryGetProperty(property, out value))
      {
        return null;
      }

      return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
    catch (JsonException)
    {
      return null;
    }
  }

  /// <summary>
  /// True when any line of <paramref name="trailerValues"/> (<c>git log -1
  /// --format=%(trailers:key=Green-Key,valueonly)</c>) is <c>full-run</c>, ignoring case.
  /// </summary>
  internal static bool IsForceFullRun(string? trailerValues) =>
    !string.IsNullOrWhiteSpace(trailerValues)
    && trailerValues.Split('\n').Any(line => string.Equals(line.Trim(), ForceTrailerValue, StringComparison.OrdinalIgnoreCase));

  /// <summary>
  /// The earliest live marker among artifact rows, one JSON object per line
  /// (<c>{id, expired, created_at, run_id, head_sha}</c>). Null when none is live.
  /// </summary>
  internal static GreenKeyMarker? SelectMarker(IEnumerable<string> jsonLines)
  {
    ArgumentNullException.ThrowIfNull(jsonLines);
    GreenKeyMarker? best = null;
    foreach (string line in jsonLines)
    {
      if (!TryParseRow(line, out GreenKeyMarker row, out bool expired) || expired) continue;
      if (best is null || row.Created < best.Created || (row.Created == best.Created && row.ArtifactId < best.ArtifactId)) best = row;
    }

    return best;
  }

  /// <summary>Turns off each path-gated job whose gate hit a marker.</summary>
  internal static WorkflowJobs ApplyHits(WorkflowJobs jobs, IReadOnlyCollection<string> hitGates)
  {
    ArgumentNullException.ThrowIfNull(hitGates);
    return new WorkflowJobs(
      jobs.Ci && !hitGates.Contains(GateCi),
      jobs.Skill && !hitGates.Contains(GateSkill),
      jobs.Template && !hitGates.Contains(GateTemplate));
  }

  private static bool TryParseRow(string line, out GreenKeyMarker row, out bool expired)
  {
    row = null!;
    expired = false;
    if (string.IsNullOrWhiteSpace(line)) return false;

    try
    {
      using var document = JsonDocument.Parse(line);
      JsonElement root = document.RootElement;
      if (root.ValueKind != JsonValueKind.Object
        || !root.TryGetProperty("id", out JsonElement id) || !id.TryGetInt64(out long artifactId))
      {
        return false;
      }

      expired = root.TryGetProperty("expired", out JsonElement expiredElement) && expiredElement.ValueKind == JsonValueKind.True;
      DateTimeOffset created = DateTimeOffset.MaxValue;
      if (ReadString(root, "created_at") is string createdText
        && DateTimeOffset.TryParse(createdText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset parsed))
      {
        created = parsed;
      }

      long runId = root.TryGetProperty("run_id", out JsonElement run) && run.ValueKind == JsonValueKind.Number && run.TryGetInt64(out long value) ? value : 0;
      row = new GreenKeyMarker(artifactId, runId, ReadString(root, "head_sha") ?? "", created);
      return true;
    }
    catch (JsonException)
    {
      return false;
    }
  }

  private static string? ReadString(JsonElement element, string name) =>
    element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

  private static string OneLine(string? value, string fallback)
  {
    string text = (value ?? "").Replace("\r", "", StringComparison.Ordinal).Replace("\n", "", StringComparison.Ordinal).Trim();
    return text.Length == 0 ? fallback : text;
  }
}

/// <summary>One hashed tree entry.</summary>
internal readonly record struct GreenKeyEntry(string Path, string Blob);

/// <summary>Whether an event is a ledger candidate, and why.</summary>
internal readonly record struct GreenKeyEventClass(bool Candidate, string Reason);

/// <summary>An earlier green marker: its artifact, run, and commit.</summary>
internal sealed record GreenKeyMarker(long ArtifactId, long RunId, string Commit, DateTimeOffset Created);
