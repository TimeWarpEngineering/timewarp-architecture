#region Purpose
// Extension, root and file tables for the sourceName-literal and package-id rewrite scans.
#endregion
#region Design
// Roots under the monorepo that ship into generated apps (or root wiring they inherit) are scanned;
// platform source trees (foundation/analyzers/libraries) are template-excluded and not scanned.
// Package-id rewrite scanning is scoped to MSBuild/JSON (not .cs); the literal scan includes .cs.
#endregion

namespace DevCli.Services;

/// <summary>
/// Scan sets used by the platform namespace-literal and package-id rewrite asserts.
/// </summary>
internal static class SourceNameLiteralScanSets
{
  public static readonly string[] SourceNameLiteralScanExtensions =
  [
    ".cs", ".csproj", ".props", ".targets", ".slnx", ".json", ".razor", ".proto",
  ];

  public static readonly string[] PackageIdRewriteScanExtensions =
  [
    ".props", ".csproj", ".targets", ".slnx", ".json",
  ];

  /// <summary>
  /// Roots under the monorepo that ship into generated apps (or root wiring they inherit).
  /// Platform source trees (foundation/analyzers/libraries) are template-excluded and not scanned.
  /// </summary>
  public static readonly string[] SourceNameLiteralScanRelativeRoots =
  [
    "source/container-apps",
    "tests/common",
    "tests/container-apps",
    "msbuild",
  ];

  public static readonly string[] SourceNameLiteralScanRelativeFiles =
  [
    "Directory.Build.props",
    "Directory.Packages.props",
    "source/Directory.Build.props",
    "tests/Directory.Build.props",
    "timewarp-architecture.slnx",
    "global.json",
    "BannedSymbols.txt",
    "aspire.config.json",
  ];

  public static bool IsSourceNameLiteralScanExtension(string file)
  {
    string extension = Path.GetExtension(file);
    return SourceNameLiteralScanExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
  }
}
