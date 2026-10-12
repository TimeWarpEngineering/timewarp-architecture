#region Purpose
// Fail if any test source under tests/ resolves a path into the monorepo's kanban/ folder.
#endregion

#region Design
// Task 316: two suites globbed kanban/**/<task>-* for screenshots, so every run rewrote review
// evidence in closed task folders and threw in generated apps (no kanban/). Shots now go through
// PlaywrightScreenshot to test output. This source scan keeps any test from reaching back into
// kanban/. It matches a string literal that starts with kanban as a path segment: "kanban",
// "kanban/..." or "kanban\...". This file holds the pattern and is skipped.
#endregion

namespace ScreenshotPathGuard_;

using System.Text.RegularExpressions;

[TestTag("Unit")]
public partial class TestSources_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TestSources_Should_>();

  public static Task Not_Reference_Kanban_As_A_Path()
  {
    string testsDirectory = FindTestsDirectory();
    string self = Path.Combine(testsDirectory, "container-apps", "web", "web-spa-playwright-tests", "screenshot-path-guard-tests.cs");

    List<string> offenders = [];
    foreach (string file in Directory.EnumerateFiles(testsDirectory, "*.cs", SearchOption.AllDirectories))
    {
      string relative = Path.GetRelativePath(testsDirectory, file);
      if (IsBuildOutput(relative) || string.Equals(Path.GetFullPath(file), Path.GetFullPath(self), StringComparison.Ordinal))
      {
        continue;
      }

      if (KanbanPathLiteral().IsMatch(File.ReadAllText(file)))
      {
        offenders.Add(relative);
      }
    }

    offenders.ShouldBeEmpty(
      "Tests must not resolve paths into kanban/. Write shots with PlaywrightScreenshot.PathFor. Offenders: "
      + string.Join(", ", offenders));
    return Task.CompletedTask;
  }

  [GeneratedRegex("\"kanban(\"|/|\\\\)", RegexOptions.IgnoreCase)]
  private static partial Regex KanbanPathLiteral();

  private static bool IsBuildOutput(string relativePath)
  {
    string[] segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    return segments.Contains("bin") || segments.Contains("obj");
  }

  private static string FindTestsDirectory()
  {
    DirectoryInfo? dir = new(AppContext.BaseDirectory);
    while (dir is not null)
    {
      string tests = Path.Combine(dir.FullName, "tests");
      if (File.Exists(Path.Combine(dir.FullName, "source", "Directory.Build.props")) && Directory.Exists(tests))
      {
        return tests;
      }

      dir = dir.Parent;
    }

    throw new InvalidOperationException("Could not locate the tests directory from " + AppContext.BaseDirectory);
  }
}
