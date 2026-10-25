#region Purpose
// Fails when a tracked file outside the excluded trees contains a client name.
#endregion

#region Design
// The name list is documentation/developer/client-names.txt. Blank lines and # comments
// are not patterns: git grep -F -f would search for the comment text. Tokens go to a temp
// pattern file, then git grep -n -z -i -I -F searches the work tree. Pathspecs skip
// documentation/, the kanban tree, and skills/*/analysis/. The list lives under
// documentation/, so it is not a hit, and the template pack leaves that tree out.
// This file does not read template.json or the template project. A hit is
// path:line: matched <name>, using the list's casing. dev build and dev lint-skills
// call this scan. ganda repo audit has no word-list check.
#endregion

namespace DevCli.Services;

using TimeWarp.Amuru;

/// <summary>Tracked-file client-name scan shared by <c>dev check-client-names</c>, <c>dev build</c>, and <c>dev lint-skills</c>.</summary>
internal static class ClientNameScan
{
  internal const string NamesFileRelativePath = "documentation/developer/client-names.txt";

  internal const int MinimumNameLength = 4;

  private const int GitGrepNoMatchExitCode = 1;

  internal readonly record struct Hit(string RelativePath, int Line, string Name);

  internal sealed record ScanOutcome(string? Error, IReadOnlyList<Hit> Hits)
  {
    internal static ScanOutcome Failed(string error) => new(error, []);
  }

  /// <summary>Writes failures and sets <see cref="Environment.ExitCode"/> when the scan does not pass.</summary>
  internal static async Task<bool> ReportAsync(
    string repoRoot,
    Action<string> info,
    Action<string> error,
    CancellationToken cancellationToken)
  {
    ScanOutcome outcome = await ScanAsync(repoRoot, cancellationToken);
    if (outcome.Error is not null)
    {
      error(outcome.Error);
      Environment.ExitCode = 1;
      return false;
    }

    if (outcome.Hits.Count == 0)
    {
      info("Client-name scan passed.");
      return true;
    }

    error("Client names in tracked files:");
    foreach (Hit hit in outcome.Hits)
    {
      error($"{hit.RelativePath}:{hit.Line}: matched {hit.Name}");
    }

    Environment.ExitCode = 1;
    return false;
  }

  internal static async Task<ScanOutcome> ScanAsync(string repoRoot, CancellationToken cancellationToken)
  {
    if (!TryReadNames(repoRoot, out IReadOnlyList<string> names, out string? nameError))
    {
      return ScanOutcome.Failed(nameError ?? "Client-name list could not be read.");
    }

    string patternFile = Path.Combine(Path.GetTempPath(), "client-names-" + Guid.NewGuid().ToString("N") + ".txt");
    try
    {
      await File.WriteAllLinesAsync(patternFile, names, cancellationToken);
      CommandOutput output = await Shell.Builder("git")
        .WithArguments
        (
          "grep",
          "-n",
          "-z",
          "-i",
          "-I",
          "-F",
          "-f",
          patternFile,
          "--",
          ".",
          ":!documentation",
          ":!kanban",
          ":!skills/*/analysis/**"
        )
        .WithWorkingDirectory(repoRoot)
        .WithNoValidation()
        .CaptureAsync(cancellationToken);

      if (output.ExitCode == GitGrepNoMatchExitCode)
      {
        return new ScanOutcome(null, []);
      }

      if (output.ExitCode != 0)
      {
        string detail = string.IsNullOrWhiteSpace(output.Combined) ? "git grep failed." : output.Combined.Trim();
        return ScanOutcome.Failed(detail);
      }

      List<Hit> hits = ParseHits(output.Stdout, names);
      if (hits.Count == 0)
      {
        return ScanOutcome.Failed("git grep reported a match that could not be parsed.");
      }

      return new ScanOutcome(null, hits);
    }
    catch (IOException exception)
    {
      return ScanOutcome.Failed(exception.Message);
    }
    finally
    {
      try
      {
        File.Delete(patternFile);
      }
      catch (IOException)
      {
        // A leftover temp pattern file is outside the repo and is not a scan hit.
      }
    }
  }

  internal static bool TryReadNames(string repoRoot, out IReadOnlyList<string> names, out string? error)
  {
    string path = Path.Combine(repoRoot, NamesFileRelativePath);
    if (!File.Exists(path))
    {
      names = [];
      error = $"Client-name list is missing: {NamesFileRelativePath}";
      return false;
    }

    List<string> list = [];
    foreach (string raw in File.ReadAllLines(path))
    {
      string line = raw.Trim();
      if (line.Length == 0 || line.StartsWith('#', StringComparison.Ordinal))
      {
        continue;
      }

      if (line.Length < MinimumNameLength || line.Any(char.IsWhiteSpace))
      {
        names = [];
        error =
          $"Client-name list entries must be single tokens of at least {MinimumNameLength} characters ({NamesFileRelativePath}).";
        return false;
      }

      list.Add(line);
    }

    if (list.Count == 0)
    {
      names = [];
      error = $"Client-name list is empty: {NamesFileRelativePath}";
      return false;
    }

    names = list;
    error = null;
    return true;
  }

  private static List<Hit> ParseHits(string stdout, IReadOnlyList<string> names)
  {
    List<Hit> hits = [];
    string[] records = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    foreach (string record in records)
    {
      string[] fields = record.Split('\0');
      if (fields.Length < 3 || !int.TryParse(fields[1], out int line))
      {
        continue;
      }

      string text = fields[2];
      foreach (string name in names)
      {
        if (text.Contains(name, StringComparison.OrdinalIgnoreCase))
        {
          hits.Add(new Hit(fields[0].Replace('\\', '/'), line, name));
        }
      }
    }

    hits.Sort(static (left, right) =>
    {
      int path = string.CompareOrdinal(left.RelativePath, right.RelativePath);
      if (path != 0)
      {
        return path;
      }

      int lineCompare = left.Line.CompareTo(right.Line);
      return lineCompare != 0 ? lineCompare : string.CompareOrdinal(left.Name, right.Name);
    });

    return hits;
  }
}
