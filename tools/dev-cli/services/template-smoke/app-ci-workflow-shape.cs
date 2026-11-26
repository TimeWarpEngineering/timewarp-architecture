#region Purpose
// Shape check for the generated app's CI workflow: it parses as block YAML, and it has the app's jobs
// and phases and nothing that belongs to the template repository.
#endregion
#region Design
// Task 531. The source is .github/app-workflows/workflow.yml (outside .github/workflows/, so it never
// runs in this repository); .template.config/template.json renames it to
// .github/workflows/workflow.yml at generation. dev-cli-tests run Check over the source, and
// template-smoke runs it over every generated variant's copy.
// No YAML parser ships with this repository or the dev CLI, so ParseKeyPaths reads the block-style
// subset a GitHub workflow uses: two-space indentation, no tabs, `key:` and `key: value` mappings,
// `- ` sequence items (an item may open a mapping), block scalars (`|`, `>-`) and one-line quoted or
// flow values. It reports a bad indent, a line that is neither key nor item, an unclosed one-line
// value, and a duplicate key in one mapping (GitHub rejects all of these), and returns every key path
// ("jobs/ci-group/strategy/matrix/include/-/name"; a sequence item is "-").
// Check then requires: on.pull_request with no paths or paths-ignore filter (ganda pr merge treats a
// branch as CI-configured from that trigger and refuses a pull request that registers no check); the
// jobs classify, ci-group, ci (no name override, so the check is named ci) and green-key; each
// `dev workflow` phase CI runs; setup-ci as the only local action; no Aspire.Cli install outside
// setup-ci (AspireVersionSources). It refuses the template repository's jobs (release, skill-lint,
// template-smoke) and text that only the template repository has: its NuGet release, its template
// content roots, its skill lint, and its self-hosted runner labels.
// Pure text (no Amuru, no Terminal), so dev-cli-tests Compile-include this file.
#endregion

namespace DevCli.Services;

using System.Text.RegularExpressions;

/// <summary>Parses and checks the generated app's CI workflow.</summary>
internal static partial class AppCiWorkflowShape
{
  /// <summary>Where the app's workflow lives in this repository.</summary>
  internal const string SourceRelativePath = ".github/app-workflows/workflow.yml";

  /// <summary>Where generation puts it in the app.</summary>
  internal const string AppRelativePath = ".github/workflows/workflow.yml";

  private const string SetupActionName = "setup-ci";

  internal const string DeployJob = "deploy";

  internal static readonly string[] RequiredJobs = ["classify", "ci-group", "ci", "green-key", DeployJob];

  /// <summary>The one deploy line the deploy job runs.</summary>
  internal const string DeployCommandLine = "dotnet run --file tools/dev-cli/dev.cs -- deploy --target $env:DEPLOY_TARGET --yes --no-browser";

  /// <summary>The condition classify and ci carry so a deploy run runs no CI.</summary>
  internal const string SkipOnDeployRun = "github.event_name != 'release' && !inputs.deploy";

  internal static readonly string[] TemplateOnlyJobs = ["release", "skill-lint", "template-smoke"];

  internal static readonly string[] RequiredPhases =
  [
    "--phase classify",
    "--phase test --group fast",
    "--phase test --group integration",
    "--phase publish",
  ];

  // Compared ignoring case.
  internal static readonly string[] TemplateOnlyText =
  [
    "template-smoke",
    "lint-skills",
    "vally",
    "nuget/login",
    "NUGET_API_KEY",
    ".template.config",
    "timewarp-templates",
    "templateRepository",
    "#if",
    "wslc",
    "twe-001",
  ];

  /// <summary>Every problem with <paramref name="yaml"/>; empty when it parses and has the app's shape.</summary>
  internal static IReadOnlyList<string> Check(string yaml)
  {
    ArgumentNullException.ThrowIfNull(yaml);
    List<string> errors = [];
    HashSet<string> paths = ParseKeyPaths(yaml, errors);
    if (errors.Count > 0) return errors;

    foreach (string required in (string[])["on", "jobs", "on/pull_request"])
    {
      if (!paths.Contains(required)) errors.Add($"Missing {required.Replace('/', '.')}.");
    }

    foreach (string filter in (string[])["paths", "paths-ignore"])
    {
      if (paths.Contains($"on/pull_request/{filter}")) errors.Add($"on.pull_request has a {filter} filter: every pull request must report ci.");
    }

    errors.AddRange(RequiredJobs.Where(job => !paths.Contains($"jobs/{job}")).Select(job => $"Missing job {job}."));
    errors.AddRange(TemplateOnlyJobs.Where(job => paths.Contains($"jobs/{job}")).Select(job => $"Has the template repository's {job} job."));
    if (paths.Contains("jobs/ci/name")) errors.Add("Job ci sets a name: the required check must be named ci.");

    errors.AddRange(RequiredPhases.Where(phase => !yaml.Contains(phase, StringComparison.Ordinal)).Select(phase => $"Does not run `dev workflow {phase}`."));
    errors.AddRange(TemplateOnlyText.Where(text => yaml.Contains(text, StringComparison.OrdinalIgnoreCase)).Select(text => $"Contains template-repository text '{text}'."));

    string[] localActions = [.. LocalAction().Matches(yaml).Select(match => match.Groups["name"].Value).Distinct(StringComparer.Ordinal)];
    if (!localActions.Contains(SetupActionName, StringComparer.Ordinal)) errors.Add($"Does not use ./.github/actions/{SetupActionName}.");
    errors.AddRange(localActions.Where(name => name != SetupActionName).Select(name => $"Uses local action {name}, which does not ship."));

    if (AspireVersionSources.WorkflowInstallError(yaml) is { } aspireError) errors.Add(aspireError);

    errors.AddRange(DeployJobErrors(yaml, paths));

    return errors;
  }

  /// <summary>Deploying or minting an OIDC token outside the deploy job, then problems with that job when it exists.</summary>
  internal static IEnumerable<string> DeployJobErrors(string yaml, HashSet<string> paths)
  {
    ArgumentNullException.ThrowIfNull(yaml);
    ArgumentNullException.ThrowIfNull(paths);
    string[] outside = [.. OutsideJob(yaml, DeployJob)];
    if (outside.Any(line => line.Contains("id-token", StringComparison.OrdinalIgnoreCase)))
      yield return "id-token appears outside the deploy job.";

    foreach (string line in Commands(outside).Where(line => DeployInvocation().IsMatch(line)))
      yield return $"Deploys outside the deploy job: {line}";

    if (!paths.Contains($"jobs/{DeployJob}")) yield break;

    if (!ReleasePublished().IsMatch(yaml)) yield return "on.release must be types: [published].";
    if (!paths.Contains("on/workflow_dispatch/inputs/deploy")) yield return "on.workflow_dispatch has no deploy input.";

    string[] deploy = JobLines(yaml, DeployJob);
    string condition = deploy.Select(line => line.Trim()).FirstOrDefault(line => line.StartsWith("if:", StringComparison.Ordinal)) ?? "";
    if (!condition.Contains("github.event_name == 'release'", StringComparison.Ordinal)
      || !condition.Contains("github.event_name == 'workflow_dispatch' && inputs.deploy", StringComparison.Ordinal))
    {
      yield return "Job deploy must run only on a published release or a dispatch with deploy on.";
    }

    if (condition.Contains("push", StringComparison.Ordinal) || condition.Contains("pull_request", StringComparison.Ordinal))
      yield return "Job deploy must not run on push or pull_request: a merge never deploys.";

    string deployText = string.Join('\n', deploy);
    foreach (string required in (string[])["environment: Production", "id-token: write", "uses: azure/login@"])
    {
      if (!deployText.Contains(required, StringComparison.Ordinal)) yield return $"Job deploy does not have '{required}'.";
    }

    foreach (string secretLogin in (string[])["client-secret", "creds:"])
    {
      if (yaml.Contains(secretLogin, StringComparison.Ordinal)) yield return $"Contains '{secretLogin}': Azure login is OIDC with no client secret.";
    }

    string[] invocations = [.. Commands(deploy).Where(line => DeployInvocation().IsMatch(line))];
    if (invocations.Length != 1 || invocations[0] != DeployCommandLine)
      yield return $"Job deploy must run one `{DeployCommandLine}`.";

    foreach (string job in (string[])["classify", "ci"])
    {
      if (!string.Join('\n', JobLines(yaml, job)).Contains(SkipOnDeployRun, StringComparison.Ordinal))
        yield return $"Job {job} must skip a deploy run ({SkipOnDeployRun}).";
    }
  }

  /// <summary>The lines of <paramref name="yaml"/> with job <paramref name="job"/>'s block removed.</summary>
  internal static IEnumerable<string> OutsideJob(string yaml, string job)
  {
    ArgumentNullException.ThrowIfNull(yaml);
    bool inside = false;
    foreach (string line in yaml.Split('\n'))
    {
      if (line == $"  {job}:") inside = true;
      else if (inside && IsBlockEnd(line)) inside = false;

      if (!inside) yield return line;
    }
  }

  // The lines of the job's block: its key line through the line before the next line at indent two or less.
  private static string[] JobLines(string yaml, string job)
  {
    string[] lines = yaml.Split('\n');
    int start = Array.IndexOf(lines, $"  {job}:");
    if (start < 0) return [];

    int end = start + 1;
    while (end < lines.Length && !IsBlockEnd(lines[end])) end++;
    return lines[start..end];
  }

  private static bool IsBlockEnd(string line) =>
    line.TrimEnd().Length > 0 && line.Length - line.TrimStart().Length <= 2;

  // Lines that are not full-line comments, trimmed.
  private static IEnumerable<string> Commands(IEnumerable<string> lines) =>
    lines.Select(line => line.Trim()).Where(line => line.Length > 0 && line[0] != '#');

  /// <summary>
  /// Key paths of the block YAML in <paramref name="yaml"/>. Parse errors go to
  /// <paramref name="errors"/>, each with its line number.
  /// </summary>
  internal static HashSet<string> ParseKeyPaths(string yaml, List<string> errors)
  {
    ArgumentNullException.ThrowIfNull(yaml);
    ArgumentNullException.ThrowIfNull(errors);
    HashSet<string> paths = new(StringComparer.Ordinal);
    if (yaml.Contains('\r', StringComparison.Ordinal)) errors.Add("Line endings are not LF.");
    if (yaml.Contains('\t', StringComparison.Ordinal)) errors.Add("Contains a tab: YAML indentation is spaces.");
    if (errors.Count > 0) return paths;

    List<Frame> frames = [new Frame(0, "", Sequence: false)];
    (string Path, int Indent)? opener = null;
    int? blockIndent = null;
    string[] lines = yaml.Split('\n');
    for (int index = 0; index < lines.Length; index++)
    {
      string line = lines[index].TrimEnd();
      int indent = line.Length - line.TrimStart().Length;
      if (blockIndent is { } scalarIndent)
      {
        if (line.Length == 0 || indent > scalarIndent) continue;
        blockIndent = null;
      }

      string content = line.TrimStart();
      if (content.Length == 0 || content[0] == '#') continue;

      string at = $"Line {index + 1}: ";
      if (indent % 2 != 0)
      {
        errors.Add(at + "indentation is not a multiple of two spaces.");
        return paths;
      }

      bool item = content == "-" || content.StartsWith("- ", StringComparison.Ordinal);
      if (!Enter(frames, ref opener, indent, item))
      {
        errors.Add(at + "indentation does not match an open mapping or sequence.");
        return paths;
      }

      Frame frame = frames[^1];
      string text = content;
      if (item)
      {
        if (!frame.Sequence)
        {
          errors.Add(at + "a sequence item where a mapping key belongs.");
          return paths;
        }

        string itemPath = Join(frame.Path, "-");
        text = content.Length == 1 ? "" : content[2..].TrimStart();
        if (text.Length == 0)
        {
          opener = (itemPath, indent);
          continue;
        }

        if (!KeyLine().IsMatch(text))
        {
          if (!ValueCloses(text)) errors.Add(at + "a quoted or flow value does not close on its line.");
          continue;
        }

        frame = new Frame(indent + 2, itemPath, Sequence: false);
        frames.Add(frame);
      }
      else if (frame.Sequence)
      {
        errors.Add(at + "a mapping key where a sequence item belongs.");
        return paths;
      }

      Match key = KeyLine().Match(text);
      if (!key.Success)
      {
        errors.Add(at + "not a key, a sequence item, or a comment.");
        return paths;
      }

      string name = key.Groups["key"].Value.Trim('\'', '"');
      if (!frame.Keys.Add(name))
      {
        errors.Add(at + $"duplicate key '{name}'.");
        continue;
      }

      string path = Join(frame.Path, name);
      paths.Add(path);
      string value = WithoutComment(key.Groups["value"].Value);
      if (value.Length == 0)
      {
        opener = (path, frame.Indent);
      }
      else if (value[0] is '|' or '>')
      {
        blockIndent = frame.Indent;
      }
      else if (!ValueCloses(value))
      {
        errors.Add(at + "a quoted or flow value does not close on its line.");
      }
    }

    return paths;
  }

  // Moves to the frame a line at indent belongs to: opens the pending key's child, or closes frames.
  private static bool Enter(List<Frame> frames, ref (string Path, int Indent)? opener, int indent, bool item)
  {
    (string Path, int Indent)? pending = opener;
    opener = null;
    if (pending is { } open && (indent > open.Indent || (item && indent == open.Indent)))
    {
      frames.Add(new Frame(indent, open.Path, Sequence: item));
      return true;
    }

    while (frames.Count > 1 && frames[^1].Indent > indent) frames.RemoveAt(frames.Count - 1);

    // A sequence written at its key's indent ends at the next key of that mapping.
    if (!item && frames.Count > 1 && frames[^1].Sequence && frames[^2].Indent == indent) frames.RemoveAt(frames.Count - 1);

    return frames[^1].Indent == indent;
  }

  private static bool ValueCloses(string value)
  {
    value = WithoutComment(value);
    if (value.Length == 0) return true;

    return value[0] switch
    {
      '\'' => value.Length > 1 && value[^1] == '\'',
      '"' => value.Length > 1 && value[^1] == '"',
      '[' => value[^1] == ']',
      '{' => value[^1] == '}',
      _ => Count(value, "${{") == Count(value, "}}"),
    };
  }

  // Drops a trailing ` # comment` outside quotes and expressions.
  private static string WithoutComment(string value)
  {
    bool single = false;
    bool @double = false;
    int expression = 0;
    for (int index = 0; index < value.Length; index++)
    {
      char character = value[index];
      if (character == '\'' && !@double) single = !single;
      else if (character == '"' && !single) @double = !@double;
      else if (string.CompareOrdinal(value, index, "${{", 0, 3) == 0) expression++;
      else if (string.CompareOrdinal(value, index, "}}", 0, 2) == 0 && expression > 0) expression--;
      else if (character == '#' && !single && !@double && expression == 0 && (index == 0 || value[index - 1] == ' '))
        return value[..index].TrimEnd();
    }

    return value.Trim();
  }

  private static int Count(string text, string token)
  {
    int count = 0;
    for (int index = text.IndexOf(token, StringComparison.Ordinal); index >= 0; index = text.IndexOf(token, index + token.Length, StringComparison.Ordinal))
      count++;
    return count;
  }

  private static string Join(string parent, string name) => parent.Length == 0 ? name : $"{parent}/{name}";

  [GeneratedRegex(@"^(?<key>[A-Za-z0-9_][A-Za-z0-9_.\-]*|'[^']+'|""[^""]+""):(?:\s+(?<value>.*))?$", RegexOptions.CultureInvariant)]
  private static partial Regex KeyLine();

  [GeneratedRegex(@"uses:\s*\./\.github/actions/(?<name>[A-Za-z0-9_.\-]+)", RegexOptions.CultureInvariant)]
  private static partial Regex LocalAction();

  /// <summary>
  /// A dev deploy, deprovision, or open verb (bin/dev or the dev.cs runfile; `deploy` covers `deploy migrate`), or a raw
  /// aspire deploy or destroy.
  /// </summary>
  [GeneratedRegex(@"(\bdev(\.cs)?\s+(--\s+)?(deploy|deprovision|open)\b)|(\baspire\s+(deploy|destroy)\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
  internal static partial Regex DeployInvocation();

  [GeneratedRegex(@"^  release:\n    types: \[published\]$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
  private static partial Regex ReleasePublished();

  private sealed record Frame(int Indent, string Path, bool Sequence)
  {
    public HashSet<string> Keys { get; } = new(StringComparer.Ordinal);
  }
}
