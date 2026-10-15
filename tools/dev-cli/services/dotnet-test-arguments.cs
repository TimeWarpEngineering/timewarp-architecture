#region Purpose
// Pure argument construction for each `dotnet test` that `dev test` runs.
#endregion

#region Design
// Kept free of Amuru/Terminal so tests/tools/dev-cli-tests can Compile-include this file.
// Task 370: `dev test` runs projects in parallel, and concurrent `dotnet test` builds would race on
// the obj/ and bin/ folders of projects they share. So nothing builds inside `dotnet test`: a build
// phase runs first, one build at a time, and every `dotnet test` gets --no-build --no-restore.
// Build phase: default `dev test` builds the Release solution (a developer never runs stale
// binaries). `dev test --no-build` skips that for callers that just built the solution in Release
// (`dev workflow`, task 365). Either way, the selected projects under tests/ that the .slnx does not
// list are built one at a time: the Jaribu family aggregators and timewarp-testing-tests. They stay
// out of the .slnx on purpose (the solution build never compiles co-located runfiles).
#endregion

namespace DevCli.Services;

using System.Xml.Linq;

internal static class DotNetTestArguments
{
  /// <summary>
  /// Full paths of every project the .slnx lists. Template conditionals are XML comments, so
  /// a monorepo .slnx still lists every project and a generated one lists only what shipped.
  /// </summary>
  internal static HashSet<string> SolutionProjects(string slnxContent, string solutionDirectory)
  {
    return XDocument.Parse(slnxContent)
      .Descendants("Project")
      .Select(project => (string?)project.Attribute("Path"))
      .OfType<string>()
      .Select(path => Path.GetFullPath(Path.Combine(solutionDirectory, path)))
      .ToHashSet(StringComparer.Ordinal);
  }

  /// <summary>Build arguments for one project the solution build does not cover (cwd = project directory).</summary>
  internal static string[] Build() => ["build", "-c", "Release"];

  /// <summary>Test arguments: the build phase already built and restored every project.</summary>
  internal static string[] Test() => ["test", "-c", "Release", "--no-build", "--no-restore"];
}
