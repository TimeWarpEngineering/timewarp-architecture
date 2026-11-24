#region Purpose
// Fails when web project markup names the template's company, which a rebrand cannot reach.
#endregion

#region Design
// Task 513 review M1, skill tw-brand: a generated app rebrands through BrandOptions and image
// files only. Copy in a .razor file is neither, and sourceName does not rewrite a company name,
// so "TimeWarp Enterprises" in markup survives every rebrand. Template-smoke scans wwwroot and
// App.razor but not the other .razor files; this scans every .razor file of every web project.
// Product copy that needs a name reads BrandOptions.ProductName.
#endregion

namespace BrandOptions_;

using System.Globalization;

[TestTag("Unit")]
public class BrandCopy_Should_
{
  private const string TemplateCompany = "TimeWarp Enterprises";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<BrandCopy_Should_>();

  public static Task Keep_Razor_Markup_Free_Of_The_Template_Company()
  {
    string root = RepoRoot();
    string projects = Path.Combine(root, "source/container-apps/web/projects");
    List<string> hits = [];
    foreach (string file in Directory.EnumerateFiles(projects, "*.razor", SearchOption.AllDirectories))
    {
      string[] lines = File.ReadAllLines(file);
      for (int index = 0; index < lines.Length; index++)
      {
        if (lines[index].Contains(TemplateCompany, StringComparison.OrdinalIgnoreCase))
        {
          hits.Add(Path.GetRelativePath(root, file) + ":" + (index + 1).ToString(CultureInfo.InvariantCulture));
        }
      }
    }

    hits.ShouldBeEmpty
    (
      "Razor markup must not name " + TemplateCompany + "; a rebrand by configuration cannot change it. "
      + "Read BrandOptions.ProductName or drop the name. See skill tw-brand. Hits: " + string.Join(", ", hits)
    );
    return Task.CompletedTask;
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
  }
}
