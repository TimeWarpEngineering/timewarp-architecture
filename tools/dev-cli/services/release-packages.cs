#region Purpose
// The template repository's release package rules: which projects can ship, and which master runs
// publish a Packages-* artifact and how (task 533).
#endregion
#region Design
// Template-only: .template.config/template.json excludes this file. Pure (no Amuru, no DevCli
// content), so dev-cli-tests Compile-include it.
//
// Packable set. ArchitecturePackableProjectService (services/architecture-packable-project-service.cs)
// asks MSBuild IsPackable and PackageId of CandidateProjectPaths: every *.csproj under source/ (the
// TimeWarp.Nuru.DevCli PackableProjectService scan, minus bin/ and obj/) plus the template pack
// project, which lives outside source/. That is the one list check-version, dev release, the master
// pack, and the release verify and push read. Nothing here names a package id.
//
// Master packages. StagesPackages is true for a push, or a dispatch, on refs/heads/master: that run
// must end with Packages-<run_number>, which `dev release` and the release job's locate-run need. A
// pull request never stages: its synthetic merge commit is not the commit a release promotes, and
// its uploads only use up the org's 500 MB Actions storage. A staging run with ci-group on packs in
// the ci (publish) leg. With ci-group off (a green-key hit, or a skills-only push, whose template
// package still changes), pack-only runs: it carries forward a live Packages-* of an earlier non-PR
// run with this run's green key when classify found one, else it builds and packs this commit.
// pack-only is its own job, so it never writes a green-key-v1-ci marker.
#endregion

namespace DevCli.Services;

/// <summary>Packable-project discovery and the master Packages-* decision.</summary>
internal static class ReleasePackages
{
  /// <summary>The dotnet-new template pack (TimeWarp.Architecture), outside source/.</summary>
  internal const string TemplateProjectRelativePath = "timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj";

  /// <summary>The prefix the release job's locate-run and `dev release` look for.</summary>
  internal const string PackagesArtifactPrefix = "Packages-";

  internal const string MasterRef = "refs/heads/master";

  internal const string PackagesOutputName = "packages";
  internal const string PackOnlyOutputName = "pack";
  internal const string CarryRunOutputName = "carry_run";

  /// <summary>Every *.csproj under source/ (outside bin/ and obj/) and the template pack project, full paths, sorted.</summary>
  internal static IReadOnlyList<string> CandidateProjectPaths(string repoRoot)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(repoRoot);

    List<string> paths = [];
    string sourceDirectory = Path.Combine(repoRoot, "source");
    if (Directory.Exists(sourceDirectory))
    {
      paths.AddRange(Directory.EnumerateFiles(sourceDirectory, "*.csproj", SearchOption.AllDirectories).Where(path => !IsBuildOutputPath(path)));
    }

    string template = Path.GetFullPath(Path.Combine(repoRoot, TemplateProjectRelativePath));
    if (File.Exists(template)) paths.Add(template);

    paths.Sort(StringComparer.Ordinal);
    return paths;
  }

  /// <summary>True when the run must publish Packages-* (a push or a dispatch on master).</summary>
  internal static bool StagesPackages(string? eventName, string? refName) =>
    refName == MasterRef && eventName is "push" or "workflow_dispatch";

  /// <summary>
  /// classify's packages, pack, and carry_run lines. pack is true when the run stages packages and
  /// ci-group is off; carry_run is set only then.
  /// </summary>
  internal static string[] OutputLines(bool stages, bool ciRuns, long? carryRun)
  {
    bool packOnly = stages && !ciRuns;
    string carry = packOnly && carryRun is > 0 ? carryRun.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";
    return
    [
      $"{PackagesOutputName}={Bool(stages)}",
      $"{PackOnlyOutputName}={Bool(packOnly)}",
      $"{CarryRunOutputName}={carry}",
    ];
  }

  /// <summary>
  /// The first run that can be carried forward: not this run, not a pull_request run (its artifacts
  /// come from a synthetic merge tree), and with a live Packages-* artifact. Newest first.
  /// </summary>
  internal static long? SelectCarryRun(IEnumerable<CarryCandidate> candidates, long currentRunId)
  {
    ArgumentNullException.ThrowIfNull(candidates);

    return candidates
      .Where(candidate => candidate.RunId != currentRunId && candidate.Event != "pull_request" && candidate.HasLivePackages)
      .OrderByDescending(candidate => candidate.RunId)
      .Select(candidate => (long?)candidate.RunId)
      .FirstOrDefault();
  }

  private static bool IsBuildOutputPath(string path)
  {
    string normalized = path.Replace('\\', '/');
    return normalized.Contains("/bin/", StringComparison.Ordinal) || normalized.Contains("/obj/", StringComparison.Ordinal);
  }

  private static string Bool(bool value) => value ? "true" : "false";
}

/// <summary>One earlier run that passed the ci gate on this run's green key.</summary>
internal readonly record struct CarryCandidate(long RunId, string Event, bool HasLivePackages);
