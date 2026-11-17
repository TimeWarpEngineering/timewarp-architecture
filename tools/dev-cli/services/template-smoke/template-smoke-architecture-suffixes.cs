#region Purpose
// Template smoke: platform package suffixes derived once from the props SSOT and cached.
#endregion
#region Design
// Suffixes come from msbuild/timewarp-platform-packages.props through PlatformPackagePropsSsot. A
// derivation failure prints an error, sets ExitCode and fails closed; every caller treats an empty
// fragment list as failure.
#endregion

namespace DevCli.Services;

/// <summary>
/// Cached architecture package suffixes and the rewrite and nupkg fragments built from them.
/// </summary>
internal sealed class TemplateSmokeArchitectureSuffixes
{
  private readonly ITerminal Terminal;
  private readonly string RepoRoot;
  private IReadOnlyList<string>? Cached;

  public TemplateSmokeArchitectureSuffixes(ITerminal terminal, string repoRoot)
  {
    Terminal = terminal;
    RepoRoot = repoRoot;
  }

  /// <summary>
  /// Ensures suffixes are derived and cached. Fail-closed (ExitCode + error line) on props drift.
  /// </summary>
  public bool TryEnsure(out IReadOnlyList<string> suffixes)
  {
    if (Cached is not null)
    {
      suffixes = Cached;
      return true;
    }

    if (!PlatformPackagePropsSsot.TryDeriveArchitectureSuffixes(RepoRoot, out suffixes, out string? error))
    {
      Terminal.WriteErrorLine((error ?? "Failed to derive architecture suffixes from props SSOT.").Red());
      Environment.ExitCode = 1;
      suffixes = [];
      return false;
    }

    Cached = suffixes;
    return true;
  }

  public IReadOnlyList<string> GetForbiddenRewrittenFragments()
  {
    if (!TryEnsure(out IReadOnlyList<string> suffixes))
      return [];
    return PlatformPackagePropsSsot.ToForbiddenRewrittenFragments(suffixes);
  }

  public IReadOnlyList<string> GetNupkgExcludeFragments()
  {
    if (!TryEnsure(out IReadOnlyList<string> suffixes))
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
