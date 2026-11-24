#region Purpose
// Pin the design-system rule: every element is catalogued, and feature components render no raw
// grouped checkbox, tree, or radio group.
#endregion

#region Design
// Source scan, no renderer. The web-spa project root is found by walking up from the test
// output. An element is catalogued when design-system-parts.cs names it as a tag (<Name
// followed by a space, slash, or angle bracket, or a line end) in a usage snippet or sample
// type. A feature component is any .razor with a components directory between features/ and
// the file. FluentCheckbox, FluentTreeView, and FluentRadioGroup there must go through a
// catalogued element instead (tw-blazor, "Visual controls come from the design system").
#endregion

namespace DesignSystemCatalogCoverage_;

using System.Text.RegularExpressions;

[TestTag("Unit")]
public class DesignSystemCatalog_Should_
{
  private static readonly string[] RawControls = ["FluentCheckbox", "FluentTreeView", "FluentRadioGroup"];

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<DesignSystemCatalog_Should_>();

  public static Task Name_Every_Element_In_The_Catalog()
  {
    string root = WebSpaRoot();
    string parts = File.ReadAllText(Path.Combine(root, "features", "design-system", "design-system-parts.cs"));
    string[] elements = Directory.GetFiles(Path.Combine(root, "components", "elements"), "*.razor");
    elements.Length.ShouldBeGreaterThan(0);

    List<string> missing = [];
    foreach (string file in elements)
    {
      string name = Path.GetFileNameWithoutExtension(file);
      if (!NamesTag(parts, name))
      {
        missing.Add(name);
      }
    }

    missing.ShouldBeEmpty("components/elements without a design-system-parts.cs entry: " + string.Join(", ", missing));
    NamesTag(parts, "PermissionTree").ShouldBeTrue();
    NamesTag(parts, "Permission").ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Keep_Raw_Selection_Controls_Out_Of_Feature_Components()
  {
    string features = Path.Combine(WebSpaRoot(), "features");
    List<string> offenders = [];
    foreach (string file in Directory.EnumerateFiles(features, "*.razor", SearchOption.AllDirectories))
    {
      string relative = Path.GetRelativePath(features, file);
      string[] segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
      if (Array.IndexOf(segments, "components", 0, segments.Length - 1) < 0)
      {
        continue;
      }

      string source = File.ReadAllText(file);
      foreach (string control in RawControls)
      {
        if (NamesTag(source, control))
        {
          offenders.Add(relative + " renders " + control);
        }
      }
    }

    offenders.ShouldBeEmpty("Feature components must use a catalogued element: " + string.Join("; ", offenders));
    return Task.CompletedTask;
  }

  private static bool NamesTag(string source, string name) =>
    Regex.IsMatch(source, "<" + Regex.Escape(name) + @"(?=[\s/>])", RegexOptions.CultureInvariant);

  private static string WebSpaRoot()
  {
    DirectoryInfo? dir = new(AppContext.BaseDirectory);
    while (dir is not null)
    {
      string candidate = Path.Combine(dir.FullName, "source", "container-apps", "web", "projects", "web-spa");
      if (Directory.Exists(Path.Combine(candidate, "components", "elements")))
      {
        return candidate;
      }

      dir = dir.Parent;
    }

    throw new InvalidOperationException("Could not locate projects/web-spa from " + AppContext.BaseDirectory);
  }
}
