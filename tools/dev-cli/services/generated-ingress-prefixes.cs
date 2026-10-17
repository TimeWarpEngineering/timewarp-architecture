#region Purpose
// Reads the ingress prefix source the hosts emit and writes the checked-in golden.
#endregion

#region Design
// artifacts/ is gitignored, so the compiler's copy of WebServerApiRoutePrefixes.g.cs cannot be
// the drift check: a hand edit there disappears on the next build and `git diff` never sees it.
// The regenerate surface is the checked-in golden goldens/ingress/WebServerApiRoutePrefixes.g.cs.
// `dev regenerate-generated` builds the two hosts that enable IngressRoutePrefixGenerator (yarp and
// the Aspire AppHost) and overwrites that golden. CI then runs `git diff --exit-code` on
// goldens/ingress. Both hosts must emit the same normalized text; a disagreement leaves the golden
// untouched. Normalization is LF and no BOM, so the golden does not change with the OS.
// Task 282 removed the Offerable offer-record generator. Availability flags are hand-written on
// the contracts, so there is no offer file in this golden set.
#endregion

namespace DevCli.Services;

using System.Text;

internal static class GeneratedIngressPrefixes
{
  internal const string SnapshotRelativePath = "goldens/ingress/WebServerApiRoutePrefixes.g.cs";

  internal const string EmittedFileName = "WebServerApiRoutePrefixes.g.cs";

  internal static readonly string[] HostProjects =
  [
    "source/container-apps/yarp/yarp.csproj",
    "source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj",
  ];

  internal static string Normalize(string text)
  {
    string withoutBom = text.TrimStart('\uFEFF');
    return withoutBom
      .Replace("\r\n", "\n", StringComparison.Ordinal)
      .Replace('\r', '\n');
  }

  internal static bool TryReadEmit(string generatedProjectDirectory, out string text, out string error)
  {
    text = string.Empty;
    error = string.Empty;

    if (!Directory.Exists(generatedProjectDirectory))
    {
      error = $"No generated output at {generatedProjectDirectory}.";
      return false;
    }

    string[] paths = Directory
      .GetFiles(generatedProjectDirectory, EmittedFileName, SearchOption.AllDirectories)
      .OrderBy(path => path, StringComparer.Ordinal)
      .ToArray();

    if (paths.Length == 0)
    {
      error = $"Build did not emit {EmittedFileName} under {generatedProjectDirectory}.";
      return false;
    }

    string[] distinct = paths
      .Select(path => Normalize(File.ReadAllText(path)))
      .Distinct(StringComparer.Ordinal)
      .ToArray();

    if (distinct.Length != 1)
    {
      error = $"{EmittedFileName} under {generatedProjectDirectory} differs across emit paths: {string.Join(", ", paths)}.";
      return false;
    }

    text = distinct[0];
    return true;
  }

  internal static string ProjectEmitDirectory(string repoRoot, string projectRelativePath)
  {
    string projectName = Path.GetFileNameWithoutExtension(projectRelativePath);
    return Path.Combine(repoRoot, "artifacts", "generated", projectName);
  }

  internal static void WriteSnapshot(string snapshotPath, string normalizedText)
  {
    string? directory = Path.GetDirectoryName(snapshotPath);
    if (!string.IsNullOrEmpty(directory))
    {
      Directory.CreateDirectory(directory);
    }

    File.WriteAllText(snapshotPath, normalizedText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
  }
}
