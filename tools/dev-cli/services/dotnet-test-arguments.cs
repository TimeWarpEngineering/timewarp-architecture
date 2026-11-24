#region Purpose
// Pure argument construction for each `dotnet test` that `dev test` runs.
#endregion

#region Design
// Kept free of Amuru/Terminal so tests/tools/dev-cli-tests can Compile-include this file. The
// `dotnet build` / `dotnet test` verbs and their typed options (-c Release, --no-build,
// --no-restore) are DotNet.Build() / DotNet.Test() calls in test-command.cs (AMURU0001); this file
// holds only the arguments that follow them.
// Task 370: `dev test` runs projects in parallel, and concurrent `dotnet test` builds would race on
// the obj/ and bin/ folders of projects they share. So nothing builds inside `dotnet test`: a build
// phase runs first, one build at a time, and every `dotnet test` gets --no-build --no-restore.
// Build phase: default `dev test` builds the Release solution (a developer never runs stale
// binaries). `dev test --no-build` skips that for callers that just built the solution in Release
// (`dev workflow`, task 365). Either way, the selected projects under tests/ that the .slnx does not
// list are built one at a time: the Jaribu family aggregators and timewarp-testing-tests. They stay
// out of the .slnx on purpose (the solution build never compiles co-located runfiles).
// Task 487: every `dotnet test` also collects MTP code coverage (CoverageFlags) into
// artifacts/coverage/<project>/coverage.cobertura.xml; CI uploads that folder (ganda audit ci-code-coverage).
// Task 516: `dev test --project` builds only the selected projects (BuildPlan), one at a time, instead
// of the whole solution, so a targeted run stays fast and still never builds inside `dotnet test`.
// --filter-class / --filter-method go after the coverage flags to Jaribu's MTP options, which each
// take one substring (class FullName / method name, ignore-case).
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

  /// <summary>
  /// MTP code-coverage flags (Microsoft.Testing.Extensions.CodeCoverage, referenced by tests/Directory.Build.props).
  /// The results directory is <see cref="CoverageRoot"/>; <see cref="PlatformArguments"/> appends one folder per project.
  /// </summary>
  internal const string CoverageFlags =
    "--results-directory artifacts/coverage --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml";

  /// <summary>Repo-relative folder CI uploads as the coverage artifact.</summary>
  internal const string CoverageRoot = "artifacts/coverage";

  /// <summary>
  /// Arguments after the typed <c>dotnet test -c Release --no-build --no-restore</c> options (the
  /// build phase already built and restored every project). MTP coverage flags follow <c>--</c>;
  /// <paramref name="resultsDirectory"/> replaces <see cref="CoverageRoot"/> so each project writes
  /// its own Cobertura file (parallel projects never share one).
  /// </summary>
  internal static string[] PlatformArguments(string resultsDirectory, string? filterClass = null, string? filterMethod = null)
  {
    List<string> arguments = ["--"];
    string[] flags = CoverageFlags.Split(' ');
    flags[1] = resultsDirectory;
    arguments.AddRange(flags);
    if (!string.IsNullOrWhiteSpace(filterClass)) arguments.AddRange(["--filter-class", filterClass]);
    if (!string.IsNullOrWhiteSpace(filterMethod)) arguments.AddRange(["--filter-method", filterMethod]);
    return [.. arguments];
  }

  /// <summary>
  /// The serial build phase for <paramref name="projects"/>. A full run builds the solution unless
  /// <paramref name="noBuild"/>, then each project the solution does not list. A run narrowed by
  /// <c>--project</c> (<paramref name="selectedByName"/>) skips the solution build and builds each
  /// selected project instead; with <paramref name="noBuild"/> it builds only the out-of-solution ones.
  /// </summary>
  internal static TestBuildPlan BuildPlan(IReadOnlyList<string> projects, HashSet<string> solutionProjects, bool noBuild, bool selectedByName)
  {
    bool buildSolution = !noBuild && !selectedByName;
    bool buildEverySelected = !noBuild && selectedByName;
    return new(
      buildSolution,
      [.. projects.Where(project => buildEverySelected || !solutionProjects.Contains(Path.GetFullPath(project)))]);
  }
}

/// <summary>Whether the build phase builds the solution, and which projects it then builds one at a time.</summary>
internal sealed record TestBuildPlan(bool Solution, string[] Projects);
