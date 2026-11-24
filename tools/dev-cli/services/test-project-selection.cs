#region Purpose
// Resolves `dev test --project` names to the test projects under tests/ that run.
#endregion

#region Design
// Task 516: agents ran raw `dotnet test <csproj>` from the repo root to target one project, which
// fails on .NET 11 MTP (the runner lives in each project's global.json) and races on shared obj/.
// `dev test --project` gives them the same targeted run through the safe path.
// Each name is matched case-insensitively. An exact project stem (file name without .csproj) wins.
// Otherwise the name must be a substring of exactly one project's stem or repo-relative path
// ('/' separators). No match and several matches both fail: the error lists the test projects or the
// candidates, so the caller can retry with a full name instead of running the wrong suites.
// Values may repeat (`-p a -p b`) or be comma-separated (`-p a,b`); the result keeps discovery order.
// Kept free of Amuru/Terminal so tests/tools/dev-cli-tests can Compile-include this file.
#endregion

namespace DevCli.Services;

internal static class TestProjectSelection
{
  /// <summary>Splits repeated and comma-separated <c>--project</c> values into trimmed names.</summary>
  internal static string[] Names(IEnumerable<string> values) =>
  [
    .. values
      .SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
      .Distinct(StringComparer.OrdinalIgnoreCase),
  ];

  /// <summary>
  /// The projects (full paths, from <paramref name="projects"/>) that <paramref name="names"/> select,
  /// or an error naming what to pass instead.
  /// </summary>
  internal static TestProjectSelectionResult Select(IReadOnlyList<string> projects, string repoRoot, IReadOnlyList<string> names)
  {
    HashSet<string> selected = new(StringComparer.Ordinal);
    foreach (string name in names)
    {
      string[] matches = Matches(projects, repoRoot, name);
      if (matches.Length == 0)
      {
        return new([], $"--project '{name}' matches no test project. Test projects: {string.Join(", ", projects.Select(Stem))}");
      }

      if (matches.Length > 1)
      {
        return new([], $"--project '{name}' matches several test projects: {string.Join(", ", matches.Select(Stem))}. Pass a full project name.");
      }

      selected.Add(matches[0]);
    }

    return new([.. projects.Where(selected.Contains)], null);
  }

  private static string[] Matches(IReadOnlyList<string> projects, string repoRoot, string name)
  {
    string wanted = name.Replace('\\', '/');
    if (wanted.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
    {
      wanted = wanted[..^".csproj".Length];
    }

    string[] exact = [.. projects.Where(project => string.Equals(Stem(project), wanted, StringComparison.OrdinalIgnoreCase))];
    if (exact.Length > 0) return exact;

    return
    [
      .. projects.Where(project =>
        Stem(project).Contains(wanted, StringComparison.OrdinalIgnoreCase)
        || Path.GetRelativePath(repoRoot, project).Replace('\\', '/').Contains(wanted, StringComparison.OrdinalIgnoreCase)),
    ];
  }

  private static string Stem(string project) => Path.GetFileNameWithoutExtension(project);
}

/// <summary>Selected project paths, or <see cref="Error"/> when the names do not select cleanly.</summary>
internal sealed record TestProjectSelectionResult(string[] Projects, string? Error);
