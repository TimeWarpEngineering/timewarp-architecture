#region Purpose
// Maps `dev test <file…>` arguments to what runs: a Jaribu runfile, or a test project under tests/
// filtered to the test classes the file declares.
#endregion

#region Design
// Task 523: CI runs the playwright group only nightly (task 502), so a walk gate that adds or edits
// a Playwright class must run that class itself. Its files are not runfiles, so `dotnet run --file`
// cannot run one; `dev test <file>` does.
// Order of checks for one file:
//   1. Missing file: an error naming it.
//   2. A line that is exactly the JARIBU_MULTI guard (RunfileGuard) makes it a co-located runfile,
//      run with `dotnet run --file`. The guard text is built from two parts so the dotnet-new
//      conditional processor never sees a directive in this file (TWA0008).
//   3. Otherwise the owning project is the deepest test project whose folder holds the file. None:
//      an error naming the file.
//   4. Test classes are the classes the file declares whose names some RegisterTests<T>() call in
//      the project registers. Reading the whole project (not only this file) keeps a partial test
//      class whose RegisterTests sits in another part. None: an error naming the file and saying
//      to pass the test class file (a helper such as drawer-page-asserts.cs lands here).
// Each class becomes one Jaribu MTP `--filter-class` value, the option `dev test --filter-class`
// already forwards. It takes exactly one substring of the class FullName, so `dev test` runs the
// project once per class. The value is `Namespace.Class` for a top-level class and `+Class` for a
// nested one (FullName joins nesting with '+'). A class is nested when its declaration is indented
// deeper than the file's least-indented class. A substring can still match a longer class name in
// the same project (`Ns.Foo` matches `Ns.FooBar`); test class names here do not collide that way.
// Classes are found by declaration text, not Roslyn: the dev CLI is AOT and does not carry Roslyn.
// A comment line that starts with `//` never matches the declaration pattern, and raw string
// literals (`"""…"""`, often test fixture text) are removed before either scan.
// Several files merge per project in argument order, classes deduplicated.
// Kept free of Amuru/Terminal so tests/tools/dev-cli-tests can Compile-include this file.
#endregion

namespace DevCli.Services;

using System.Text.RegularExpressions;

internal static partial class TestFileSelection
{
  /// <summary>The guard line that marks a co-located Jaribu runfile.</summary>
  internal static readonly string RunfileGuard = string.Concat("#", "if !JARIBU_MULTI");

  /// <summary>
  /// Resolves <paramref name="files"/> (absolute or relative to <paramref name="workingDirectory"/>).
  /// <paramref name="readFile"/> returns a file's text, or null when it does not exist.
  /// <paramref name="projectSources"/> returns the text of every .cs file in a project's folder.
  /// </summary>
  internal static TestFileSelectionResult Select(
    IReadOnlyList<string> files,
    string workingDirectory,
    string repoRoot,
    IReadOnlyList<string> projects,
    Func<string, string?> readFile,
    Func<string, IEnumerable<string>> projectSources)
  {
    ArgumentNullException.ThrowIfNull(files);
    ArgumentNullException.ThrowIfNull(readFile);
    ArgumentNullException.ThrowIfNull(projectSources);

    List<string> runfiles = [];
    List<TestFileProjectRun> projectRuns = [];
    List<string> errors = [];
    Dictionary<string, HashSet<string>> registeredByProject = new(StringComparer.Ordinal);

    foreach (string file in files)
    {
      string path = Path.GetFullPath(Path.Combine(workingDirectory, file));
      string shown = Relative(repoRoot, path);
      string? text = readFile(path);
      if (text is null)
      {
        errors.Add($"{shown} does not exist.");
        continue;
      }

      if (IsRunfile(text))
      {
        if (!runfiles.Contains(path, StringComparer.Ordinal)) runfiles.Add(path);
        continue;
      }

      string? project = OwningProject(projects, path);
      if (project is null)
      {
        errors.Add(
          $"{shown} is in no test project under tests/ and is not a Jaribu runfile (no '{RunfileGuard}' guard). " +
          "Pass a test file from a test project, or a co-located runfile.");
        continue;
      }

      if (!registeredByProject.TryGetValue(project, out HashSet<string>? registered))
      {
        registered = Registered(projectSources(Path.GetDirectoryName(project) ?? string.Empty));
        registeredByProject[project] = registered;
      }

      string[] classes = ClassFilters(text, registered);
      if (classes.Length == 0)
      {
        errors.Add(
          $"{shown} declares no test class (no class in it is registered with RegisterTests<T>() in " +
          $"{Path.GetFileNameWithoutExtension(project)}). It looks like a helper; pass the test class file instead.");
        continue;
      }

      int index = projectRuns.FindIndex(run => run.Project == project);
      if (index < 0)
      {
        projectRuns.Add(new(project, classes));
      }
      else
      {
        projectRuns[index] = projectRuns[index] with { ClassFilters = [.. projectRuns[index].ClassFilters.Union(classes, StringComparer.Ordinal)] };
      }
    }

    return new([.. runfiles], [.. projectRuns], [.. errors]);
  }

  /// <summary>True when a line of <paramref name="text"/> is the <see cref="RunfileGuard"/>.</summary>
  internal static bool IsRunfile(string text) =>
    text.Split('\n').Any(line => line.Trim() == RunfileGuard);

  /// <summary>The deepest project in <paramref name="projects"/> whose folder holds <paramref name="file"/>.</summary>
  internal static string? OwningProject(IEnumerable<string> projects, string file) =>
    projects
      .Where(project => Path.GetDirectoryName(Path.GetFullPath(project)) is string directory
        && file.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
      .OrderByDescending(project => Path.GetDirectoryName(Path.GetFullPath(project))?.Length ?? 0)
      .FirstOrDefault();

  /// <summary>
  /// The <c>--filter-class</c> value for each class <paramref name="text"/> declares whose name is in
  /// <paramref name="registered"/>, in declaration order.
  /// </summary>
  internal static string[] ClassFilters(string text, IReadOnlySet<string> registered)
  {
    ArgumentNullException.ThrowIfNull(registered);
    text = WithoutRawStrings(text);
    Match nsMatch = NamespaceDeclaration().Match(text);
    string? ns = nsMatch.Success ? nsMatch.Groups["ns"].Value : null;
    Match[] declarations = [.. ClassDeclaration().Matches(text).Cast<Match>()];
    if (declarations.Length == 0) return [];

    int topIndent = declarations.Min(d => d.Groups["indent"].Length);
    return
    [
      .. declarations
        .Where(d => registered.Contains(d.Groups["name"].Value))
        .Select(d => d.Groups["indent"].Length > topIndent
          ? "+" + d.Groups["name"].Value
          : ns is null ? d.Groups["name"].Value : $"{ns}.{d.Groups["name"].Value}")
        .Distinct(StringComparer.Ordinal),
    ];
  }

  /// <summary>Every type name passed to <c>RegisterTests&lt;T&gt;()</c> in <paramref name="sources"/>.</summary>
  internal static HashSet<string> Registered(IEnumerable<string> sources) =>
    [.. sources.SelectMany(source => RegisterTestsCall().Matches(WithoutRawStrings(source)).Select(m => m.Groups["name"].Value))];

  // A raw string literal (test fixture text) may hold class declarations and RegisterTests calls.
  private static string WithoutRawStrings(string text) => RawStringLiteral().Replace(text, "\"\"");

  private static string Relative(string repoRoot, string path) =>
    Path.GetRelativePath(repoRoot, path).Replace('\\', '/');

  [GeneratedRegex("(?<quotes>\"{3,})[\\s\\S]*?\\k<quotes>(?!\")", RegexOptions.CultureInvariant)]
  private static partial Regex RawStringLiteral();

  [GeneratedRegex(@"^\s*namespace\s+(?<ns>[A-Za-z_][\w.]*)", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
  private static partial Regex NamespaceDeclaration();

  [GeneratedRegex(
    @"^(?<indent>[ \t]*)(?:(?:public|internal|private|protected|sealed|static|abstract|partial|file|unsafe)\s+)*(?:record\s+)?class\s+(?<name>[A-Za-z_]\w*)",
    RegexOptions.Multiline | RegexOptions.CultureInvariant)]
  private static partial Regex ClassDeclaration();

  [GeneratedRegex(@"RegisterTests<\s*(?:[\w.]+\.)?(?<name>[A-Za-z_]\w*)\s*>", RegexOptions.CultureInvariant)]
  private static partial Regex RegisterTestsCall();
}

/// <summary>One test project and the <c>--filter-class</c> values to run in it, one run each.</summary>
internal sealed record TestFileProjectRun(string Project, string[] ClassFilters);

/// <summary>What <c>dev test &lt;file…&gt;</c> runs, or the errors that stop it.</summary>
internal sealed record TestFileSelectionResult(string[] Runfiles, TestFileProjectRun[] ProjectRuns, string[] Errors);
