#region Purpose
// Selects the architecture template nupkg (not Analyzers/Generators/Attributes) from a pack output.
#endregion

#region Design
// TimeWarp.Architecture.{version}.nupkg — version starts with a digit. Sibling platform packages
// TimeWarp.Architecture.Analyzers.*.nupkg share the prefix and must not win a glob.
// FindArchitectureTemplateNupkg picks LastWriteTimeUtc among remaining matches so a just-packed
// file wins if leftovers remain. Callers wipe the pack directory first (template-install).
#endregion

namespace DevCli.Services;

/// <summary>Nupkg file-name filter for the architecture template package id.</summary>
internal static class TemplateNupkg
{
  internal const string PackageId = "TimeWarp.Architecture";

  internal static bool IsArchitectureTemplateNupkgFileName(string fileName)
  {
    const string prefix = "TimeWarp.Architecture.";
    const string suffix = ".nupkg";
    if (fileName.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
      || !fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    string rest = fileName[prefix.Length..^suffix.Length];
    return rest.Length > 0 && char.IsAsciiDigit(rest[0]);
  }

  internal static string? FindArchitectureTemplateNupkg(string directory)
  {
    if (!Directory.Exists(directory))
    {
      return null;
    }

    return Directory
      .GetFiles(directory, $"{PackageId}.*.nupkg")
      .Where(path => IsArchitectureTemplateNupkgFileName(Path.GetFileName(path)))
      .OrderByDescending(path => File.GetLastWriteTimeUtc(path))
      .ThenByDescending(path => path, StringComparer.Ordinal)
      .FirstOrDefault();
  }
}
