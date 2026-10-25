#region Purpose
// Fails when a client name from the unpacked list appears in packed template content.
#endregion

#region Design
// The name list is documentation/developer/client-names.txt. That tree is an unconditional
// exclude in .template.config/template.json, and timewarp-templates packs by allow-list, so
// the list itself is not in the nupkg. ganda repo audit has no word-list check, so this scan
// is the gate. It walks source/, tests/, and skills/ except skills/**/analysis/**, plus tools/
// only when the pack allow-list includes tools/dev-cli/dev.cs. Matching is a case-insensitive
// substring, the same match as a full-tree search, not a diff. dev build runs the scan, and
// CI's fast job runs that build. dev lint-skills runs it too, because a skills-only PR does
// not start the fast job.
#endregion

namespace DevCli.Services;

using System.Text;
using System.Text.Json;
using System.Xml.Linq;

/// <summary>Packed-content client-name scan shared by <c>dev check-client-names</c>, <c>dev build</c>, and <c>dev lint-skills</c>.</summary>
internal static class ClientNameScan
{
  internal const string NamesFileRelativePath = "documentation/developer/client-names.txt";

  internal const string TemplateJsonRelativePath = ".template.config/template.json";

  internal const string TemplateProjectRelativePath =
    "timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj";

  internal const string ToolsProbeRelativePath = "tools/dev-cli/dev.cs";

  internal const int MinimumNameLength = 4;

  internal readonly record struct Hit(string RelativePath, int Line, string Name);

  internal sealed record ScanOutcome(string? Error, IReadOnlyList<Hit> Hits)
  {
    internal static ScanOutcome Failed(string error) => new(error, []);
  }

  /// <summary>Writes failures and sets <see cref="Environment.ExitCode"/> when the scan does not pass.</summary>
  internal static bool Report(string repoRoot, Action<string> info, Action<string> error)
  {
    ScanOutcome outcome = Scan(repoRoot);
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

    error("Client names in packed content:");
    foreach (Hit hit in outcome.Hits)
    {
      error($"{hit.RelativePath}:{hit.Line}: matched {hit.Name}");
    }

    Environment.ExitCode = 1;
    return false;
  }

  internal static ScanOutcome Scan(string repoRoot)
  {
    if (!TryReadNames(repoRoot, out IReadOnlyList<string> names, out string? nameError))
    {
      return ScanOutcome.Failed(nameError ?? "Client-name list could not be read.");
    }

    string templatePath = Path.Combine(repoRoot, TemplateJsonRelativePath);
    string projectPath = Path.Combine(repoRoot, TemplateProjectRelativePath);
    if (!File.Exists(templatePath) || !File.Exists(projectPath))
    {
      return ScanOutcome.Failed(
        $"Template pack definition is missing ({TemplateJsonRelativePath}, {TemplateProjectRelativePath}).");
    }

    string templateJson;
    string project;
    try
    {
      templateJson = File.ReadAllText(templatePath);
      project = File.ReadAllText(projectPath);
    }
    catch (IOException exception)
    {
      return ScanOutcome.Failed(exception.Message);
    }

    string? unpacked = UnpackedError(templateJson, project);
    if (unpacked is not null)
    {
      return ScanOutcome.Failed(unpacked);
    }

    List<string> roots = ["source", "tests", "skills"];
    if (PackIncludesRepoPath(project, ToolsProbeRelativePath))
    {
      roots.Add("tools");
    }

    return new ScanOutcome(null, FindHits(repoRoot, roots, names));
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

  /// <summary>Null when the name list is excluded by an unconditional template modifier and is absent from the pack allow-list.</summary>
  internal static string? UnpackedError(string templateJson, string templateProject)
  {
    bool covered;
    try
    {
      covered = UnconditionalExcludeCovers(templateJson, NamesFileRelativePath);
    }
    catch (JsonException exception)
    {
      return $"{TemplateJsonRelativePath} is not readable JSON: {exception.Message}";
    }

    if (!covered)
    {
      return
        $"{NamesFileRelativePath} is not covered by an unconditional exclude in {TemplateJsonRelativePath}.";
    }

    bool packed;
    try
    {
      packed = PackIncludesRepoPath(templateProject, NamesFileRelativePath);
    }
    catch (System.Xml.XmlException exception)
    {
      return $"{TemplateProjectRelativePath} is not readable XML: {exception.Message}";
    }

    if (packed)
    {
      return $"{NamesFileRelativePath} is included by the timewarp-templates pack.";
    }

    return null;
  }

  internal static bool UnconditionalExcludeCovers(string templateJson, string repoRelativePath)
  {
    using var document = JsonDocument.Parse(templateJson);
    if (!document.RootElement.TryGetProperty("sources", out JsonElement sources))
    {
      return false;
    }

    foreach (JsonElement source in sources.EnumerateArray())
    {
      if (!source.TryGetProperty("modifiers", out JsonElement modifiers))
      {
        continue;
      }

      foreach (JsonElement modifier in modifiers.EnumerateArray())
      {
        if (!IsUnconditional(modifier) || !modifier.TryGetProperty("exclude", out JsonElement exclude))
        {
          continue;
        }

        foreach (JsonElement pattern in exclude.EnumerateArray())
        {
          string? text = pattern.GetString();
          if (text is not null && GlobCovers(text, repoRelativePath))
          {
            return true;
          }
        }
      }
    }

    return false;
  }

  internal static bool PackIncludesRepoPath(string templateProject, string repoRelativePath)
  {
    var document = XDocument.Parse(templateProject);
    foreach (XElement item in document.Descendants())
    {
      if (item.Name.LocalName is not ("Content" or "None" or "Compile"))
      {
        continue;
      }

      string? include = (string?)item.Attribute("Include");
      if (string.IsNullOrWhiteSpace(include))
      {
        continue;
      }

      string? pack = (string?)item.Attribute("Pack");
      if (string.Equals(pack, "false", StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      string? exclude = (string?)item.Attribute("Exclude");
      foreach (string oneInclude in SplitList(include))
      {
        if (oneInclude.Contains('$', StringComparison.Ordinal))
        {
          return true;
        }

        if (!IncludeMatchesRepoPath(oneInclude, repoRelativePath))
        {
          continue;
        }

        if (exclude is not null && ExcludeCovers(exclude, repoRelativePath))
        {
          continue;
        }

        return true;
      }
    }

    return false;
  }

  internal static IReadOnlyList<Hit> FindHits(
    string repoRoot,
    IReadOnlyList<string> roots,
    IReadOnlyList<string> names)
  {
    List<Hit> hits = [];
    EnumerationOptions options = new()
    {
      RecurseSubdirectories = true,
      IgnoreInaccessible = true,
      AttributesToSkip = FileAttributes.ReparsePoint,
    };

    foreach (string root in roots)
    {
      string directory = Path.Combine(repoRoot, root);
      if (!Directory.Exists(directory))
      {
        continue;
      }

      foreach (string file in Directory.EnumerateFiles(directory, "*", options))
      {
        string relative = Path.GetRelativePath(repoRoot, file).Replace('\\', '/');
        if (Skip(relative))
        {
          continue;
        }

        Collect(file, relative, names, hits);
      }
    }

    hits.Sort(static (left, right) =>
    {
      int path = string.CompareOrdinal(left.RelativePath, right.RelativePath);
      if (path != 0)
      {
        return path;
      }

      int line = left.Line.CompareTo(right.Line);
      return line != 0 ? line : string.CompareOrdinal(left.Name, right.Name);
    });
    return hits;
  }

  internal static bool GlobCovers(string pattern, string relativePath)
  {
    string[] patternParts = pattern.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
    string[] pathParts = relativePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
    return Match(patternParts, 0, pathParts, 0);
  }

  private static void Collect(string file, string relative, IReadOnlyList<string> names, List<Hit> hits)
  {
    byte[] bytes;
    try
    {
      bytes = File.ReadAllBytes(file);
    }
    catch (IOException)
    {
      return;
    }

    if (bytes.AsSpan().Contains((byte)0))
    {
      return;
    }

    string text = Encoding.UTF8.GetString(bytes);
    string[] lines = text.Split('\n');
    for (int index = 0; index < lines.Length; index++)
    {
      string line = lines[index];
      foreach (string name in names)
      {
        if (line.Contains(name, StringComparison.OrdinalIgnoreCase))
        {
          hits.Add(new Hit(relative, index + 1, name));
        }
      }
    }
  }

  private static bool Skip(string relative)
  {
    if (string.Equals(relative, NamesFileRelativePath, StringComparison.Ordinal))
    {
      return true;
    }

    string[] parts = relative.Split('/');
    for (int index = 0; index < parts.Length - 1; index++)
    {
      if (parts[index] is "bin" or "obj" or "artifacts" or "node_modules")
      {
        return true;
      }

      if (parts[0] == "skills" && parts[index] == "analysis")
      {
        return true;
      }
    }

    return false;
  }

  private static bool IsUnconditional(JsonElement modifier)
  {
    if (!modifier.TryGetProperty("condition", out JsonElement condition))
    {
      return true;
    }

    string text = condition.GetString()?.Trim() ?? "";
    return text.Length == 0
      || text.Equals("true", StringComparison.OrdinalIgnoreCase)
      || text.Equals("(true)", StringComparison.OrdinalIgnoreCase);
  }

  private static bool ExcludeCovers(string exclude, string repoRelativePath)
  {
    foreach (string pattern in SplitList(exclude))
    {
      if (pattern.Contains('$', StringComparison.Ordinal))
      {
        continue;
      }

      if (IncludeMatchesRepoPath(pattern, repoRelativePath))
      {
        return true;
      }
    }

    return false;
  }

  private static bool IncludeMatchesRepoPath(string include, string repoRelativePath)
  {
    string projectDirectory = Path.GetDirectoryName(TemplateProjectRelativePath)!.Replace('\\', '/');
    string combined = projectDirectory + "/" + include.Replace('\\', '/');
    string[] parts = combined.Split('/', StringSplitOptions.RemoveEmptyEntries);
    List<string> stack = [];
    foreach (string part in parts)
    {
      if (part == ".")
      {
        continue;
      }

      if (part == "..")
      {
        if (stack.Count == 0 || stack[^1].Contains('*', StringComparison.Ordinal))
        {
          return false;
        }

        stack.RemoveAt(stack.Count - 1);
        continue;
      }

      stack.Add(part);
    }

    string[] path = repoRelativePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
    return Match([.. stack], 0, path, 0);
  }

  private static bool Match(string[] pattern, int patternIndex, string[] path, int pathIndex)
  {
    while (patternIndex < pattern.Length)
    {
      string part = pattern[patternIndex];
      if (part == "**")
      {
        if (patternIndex == pattern.Length - 1)
        {
          return true;
        }

        for (int skip = pathIndex; skip <= path.Length; skip++)
        {
          if (Match(pattern, patternIndex + 1, path, skip))
          {
            return true;
          }
        }

        return false;
      }

      if (pathIndex >= path.Length || !SegmentMatch(part, path[pathIndex]))
      {
        return false;
      }

      patternIndex++;
      pathIndex++;
    }

    return pathIndex == path.Length;
  }

  private static bool SegmentMatch(string pattern, string segment)
  {
    int patternIndex = 0;
    int segmentIndex = 0;
    int star = -1;
    int mark = -1;
    while (segmentIndex < segment.Length)
    {
      if (patternIndex < pattern.Length && (pattern[patternIndex] == '?' || pattern[patternIndex] == segment[segmentIndex]))
      {
        patternIndex++;
        segmentIndex++;
      }
      else if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
      {
        star = patternIndex;
        patternIndex++;
        mark = segmentIndex;
      }
      else if (star >= 0)
      {
        patternIndex = star + 1;
        mark++;
        segmentIndex = mark;
      }
      else
      {
        return false;
      }
    }

    while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
    {
      patternIndex++;
    }

    return patternIndex == pattern.Length;
  }

  private static string[] SplitList(string value) =>
    value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
