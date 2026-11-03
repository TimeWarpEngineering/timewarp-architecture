#region Purpose
// Path helper shared by every smoke tree walk.
#endregion
#region Design
// All tree walks over the monorepo or a generated app skip bin/obj/artifacts through this one
// helper (any path segment, case-insensitive), so scans never read build output.
#endregion

namespace DevCli.Services;

/// <summary>
/// Path helpers for smoke tree walks.
/// </summary>
internal static class TemplateSmokePaths
{
  /// <summary>
  /// True when <paramref name="file"/> is under bin/, obj/, or artifacts/ relative to
  /// <paramref name="root"/> (any path segment, case-insensitive).
  /// </summary>
  public static bool IsBinObjOrArtifacts(string root, string file)
  {
    string relative = Path.GetRelativePath(root, file);
    string[] parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    return parts.Any(part =>
      part.Equals("bin", StringComparison.OrdinalIgnoreCase)
      || part.Equals("obj", StringComparison.OrdinalIgnoreCase)
      || part.Equals("artifacts", StringComparison.OrdinalIgnoreCase));
  }
}
