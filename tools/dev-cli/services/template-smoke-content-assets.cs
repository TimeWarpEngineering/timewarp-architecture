#region Purpose
// Map a root-relative `_content` specifier to a file in the NuGet static-web-asset cache.
#endregion

#region Design
// The first URL segment is the static-web-asset base path, which is not always the package id.
// TimeWarp.State.Blazor publishes at BasePath `_content/TimeWarp.State` (see its
// PackageAssets.json), so a miss under the TimeWarp.State package also checks
// TimeWarp.State.Blazor at the same relative path. The URL package is tried first so a
// package that still ships its own assets wins. Versions come from Directory.Packages.props.
// Kept free of Amuru/Terminal so tests/tools/dev-cli-tests can Compile-include this file.
#endregion

namespace DevCli.Services;

internal static class TemplateSmokeContentAssets
{
  private const string TimeWarpStateContentPackageId = "TimeWarp.State";
  private const string TimeWarpStateBlazorPackageId = "TimeWarp.State.Blazor";

  /// <summary>
  /// Resolves a root-relative <c>/_content/...</c> specifier to an existing static web asset.
  /// Non-content specifiers return false with a null error so the caller can skip them.
  /// </summary>
  internal static bool TryResolve(
    string specifier,
    IReadOnlyDictionary<string, string> packageVersions,
    string packagesRoot,
    out string? resolvedPath,
    out string? error)
  {
    resolvedPath = null;
    error = null;

    if (string.IsNullOrWhiteSpace(specifier) || !specifier.StartsWith("/_content/", StringComparison.Ordinal))
    {
      return false;
    }

    string relative = specifier["/_content/".Length..];
    int slash = relative.IndexOf('/', StringComparison.Ordinal);
    if (slash <= 0 || slash == relative.Length - 1)
    {
      error = $"Malformed _content specifier: {specifier}";
      return false;
    }

    string contentPackageId = relative[..slash];
    string assetPath = relative[(slash + 1)..].Replace('/', Path.DirectorySeparatorChar);
    List<string> candidateIds = CandidatePackageIds(contentPackageId, packageVersions);
    if (candidateIds.Count == 0)
    {
      error = $"{specifier} (PackageVersion for {contentPackageId} not found)";
      return false;
    }

    List<string> attempted = [];
    foreach (string candidateId in candidateIds)
    {
      string candidatePath = StaticWebAssetPath(
        packagesRoot,
        candidateId,
        packageVersions[candidateId],
        assetPath);
      attempted.Add(candidatePath);
      if (File.Exists(candidatePath))
      {
        resolvedPath = candidatePath;
        return true;
      }
    }

    error = $"{specifier} → {string.Join("; ", attempted)}";
    return false;
  }

  private static List<string> CandidatePackageIds(
    string contentPackageId,
    IReadOnlyDictionary<string, string> packageVersions)
  {
    List<string> candidates = [];
    if (packageVersions.ContainsKey(contentPackageId))
    {
      candidates.Add(contentPackageId);
    }

    if (string.Equals(contentPackageId, TimeWarpStateContentPackageId, StringComparison.OrdinalIgnoreCase)
      && packageVersions.ContainsKey(TimeWarpStateBlazorPackageId))
    {
      candidates.Add(TimeWarpStateBlazorPackageId);
    }

    return candidates;
  }

  private static string StaticWebAssetPath(
    string packagesRoot,
    string packageId,
    string version,
    string assetPath)
  {
    return Path.Combine(
      packagesRoot,
      packageId.ToLowerInvariant(),
      version,
      "staticwebassets",
      assetPath);
  }
}
