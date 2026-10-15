#region Purpose
// Pure argument construction for each `dotnet test` that `dev test` runs.
#endregion

#region Design
// Kept free of Amuru/Terminal so tests/tools/dev-cli-tests can Compile-include this file.
// Default `dev test` builds every project (`dotnet test -c Release`) so a developer never runs
// stale binaries. `dev test --no-build` is for callers that just built the solution in Release
// (`dev workflow`, task 365): projects listed in the .slnx get `--no-build --no-restore`.
// Projects under tests/ that the .slnx does not list are the Jaribu family aggregators and
// timewarp-testing-tests. They stay out of the .slnx on purpose (the solution build never
// compiles co-located runfiles), so the solution build did not build them; they keep building
// inside their own `dotnet test` even under --no-build.
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

  internal static string[] For(bool noBuild, bool builtBySolution)
  {
    return noBuild && builtBySolution
      ? ["test", "-c", "Release", "--no-build", "--no-restore"]
      : ["test", "-c", "Release"];
  }
}
