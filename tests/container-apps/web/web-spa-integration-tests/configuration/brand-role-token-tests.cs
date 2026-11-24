#region Purpose
// Fails when component CSS reads the hue palette instead of a role token.
#endregion

#region Design
// Task 513, skill tw-brand: /brand.css overrides role tokens only, so a component that reads
// --twe-purple, --twe-blue, or --twe-green keeps the template color after a rebrand. This scans
// features/ and components/ of every web project for var( followed by a hue token. tokens.css is
// under wwwroot/, not scanned: its palette seeds the role defaults.
#endregion

namespace BrandOptions_;

using System.Globalization;

[TestTag("Unit")]
public class BrandRoleTokens_Should_
{
  private static readonly HashSet<string> HuePaletteScanExtensions = new(StringComparer.OrdinalIgnoreCase) { ".css", ".razor", ".cs", ".ts" };

  private static readonly string[] HuePalette = ["--twe-purple", "--twe-blue", "--twe-green"];

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<BrandRoleTokens_Should_>();

  public static Task Keep_Component_Css_On_Role_Tokens()
  {
    string projects = Path.Combine(RepoRoot(), "source/container-apps/web/projects");
    List<string> hits = [];
    foreach (string project in Directory.EnumerateDirectories(projects))
    {
      foreach (string folder in new[] { "features", "components" })
      {
        string root = Path.Combine(project, folder);
        if (!Directory.Exists(root))
        {
          continue;
        }

        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
          if (!HuePaletteScanExtensions.Contains(Path.GetExtension(file)))
          {
            continue;
          }

          string[] lines = File.ReadAllLines(file);
          for (int index = 0; index < lines.Length; index++)
          {
            if (ReadsHuePalette(lines[index]))
            {
              hits.Add(Path.GetRelativePath(RepoRoot(), file) + ":" + (index + 1).ToString(CultureInfo.InvariantCulture));
            }
          }
        }
      }
    }

    hits.ShouldBeEmpty
    (
      "Component CSS reads role tokens (--twe-primary*, --twe-secondary*, --twe-highlight*, --twe-info), "
      + "not the hue palette. See skill tw-brand. Hits: " + string.Join(", ", hits)
    );
    return Task.CompletedTask;
  }

  /// <summary>True when a line has var( followed by a hue palette token.</summary>
  private static bool ReadsHuePalette(string line)
  {
    int start = 0;
    while ((start = line.IndexOf("var(", start, StringComparison.Ordinal)) >= 0)
    {
      start += 4;
      string argument = line[start..].TrimStart();
      if (HuePalette.Any(token => argument.StartsWith(token, StringComparison.Ordinal)))
      {
        return true;
      }
    }

    return false;
  }

  private static string RepoRoot()
  {
    string? directory = AppContext.BaseDirectory;
    while (directory is not null && !File.Exists(Path.Combine(directory, "timewarp-architecture.slnx")))
    {
      directory = Path.GetDirectoryName(directory);
    }

    directory.ShouldNotBeNull();
    return directory;
  }}
