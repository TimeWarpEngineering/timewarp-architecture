#region Purpose
// Shared template smoke/publish-smoke harness core: repo state and platform suffix derivation.
#endregion
#region Design
// Task 131-003 / F-007: collapse duplication between template-smoke and template-publish-smoke.
// The harness is partial by responsibility: -platform-pins (rewrite and namespace-literal asserts),
// -generated-content (skills, kanban, deploy parameters, ingress port), -test-runs (co-located tests,
// Jaribu aggregators, generated suites), -generation (generate + SmokeOneAsync + environment), and
// template-smoke-initializer-assets (host-free import-graph check). Suffixes are derived once from
// the props SSOT and cached; a derivation failure sets ExitCode and fails closed.
#endregion

namespace DevCli.Services;

/// <summary>
/// Shared assert helpers + generate/restore/build skeleton for template smoke gates.
/// </summary>
internal sealed partial class TemplateSmokeHarness
{
  private readonly ITerminal Terminal;
  private readonly string RepoRoot;
  private IReadOnlyList<string>? ArchitectureSuffixes;

  public TemplateSmokeHarness(ITerminal terminal, string repoRoot)
  {
    Terminal = terminal;
    RepoRoot = repoRoot;
  }

  /// <summary>
  /// Ensures suffixes are derived and cached. Fail-closed (ExitCode + error line) on props drift.
  /// </summary>
  public bool TryEnsureArchitectureSuffixes(out IReadOnlyList<string> suffixes)
  {
    if (ArchitectureSuffixes is not null)
    {
      suffixes = ArchitectureSuffixes;
      return true;
    }

    if (!PlatformPackagePropsSsot.TryDeriveArchitectureSuffixes(RepoRoot, out suffixes, out string? error))
    {
      Terminal.WriteErrorLine((error ?? "Failed to derive architecture suffixes from props SSOT.").Red());
      Environment.ExitCode = 1;
      suffixes = [];
      return false;
    }

    ArchitectureSuffixes = suffixes;
    return true;
  }

  public IReadOnlyList<string> GetForbiddenRewrittenFragments()
  {
    if (!TryEnsureArchitectureSuffixes(out IReadOnlyList<string> suffixes))
      return [];
    return PlatformPackagePropsSsot.ToForbiddenRewrittenFragments(suffixes);
  }

  public IReadOnlyList<string> GetNupkgExcludeFragments()
  {
    if (!TryEnsureArchitectureSuffixes(out IReadOnlyList<string> suffixes))
      return [];
    return PlatformPackagePropsSsot.ToNupkgExcludeFragments(suffixes);
  }

  /// <summary>
  /// True when the nupkg file name is a platform sibling of the template package
  /// (e.g. TimeWarp.Architecture.Analyzers.*.nupkg).
  /// </summary>
  public bool IsExcludedPlatformNupkgFileName(string fileName)
  {
    IReadOnlyList<string> fragments = GetNupkgExcludeFragments();
    return fragments.Any(fragment =>
      fileName.Contains(fragment, StringComparison.Ordinal));
  }
}
