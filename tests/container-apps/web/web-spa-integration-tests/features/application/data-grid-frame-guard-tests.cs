#region Purpose
// Fail if a FluentDataGrid under web-spa is not framed by DataGridFrame, does not size its
// columns with GridTemplateColumns in Grid display mode, or sets a column Width.
#endregion

#region Design
// Task 354: a grid whose other columns have fixed widths leaves its unsized column the leftover
// width, which is zero at a narrow viewport or with the Ask panel open. Every grid uses minmax
// tracks and scrolls inside DataGridFrame instead (tw-blazor, "Server-sorted FluentDataGrid").
// Source scan, the same shape as the hand-written Loading literal guard.
#endregion

namespace DataGridFrameGuard_;

[TestTag("Unit")]
public class WebSpaRazor_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<WebSpaRazor_Should_>();

  public static Task Frame_Every_FluentDataGrid_With_Minmax_Columns()
  {
    string webSpaDirectory = Path.Combine(FindRepoRoot(), "source", "container-apps", "web", "projects", "web-spa");
    Directory.Exists(webSpaDirectory).ShouldBeTrue(webSpaDirectory);

    List<string> offenders = [];
    int grids = 0;
    foreach (string file in Directory.EnumerateFiles(webSpaDirectory, "*.razor", SearchOption.AllDirectories))
    {
      string relative = Path.GetRelativePath(webSpaDirectory, file);
      string source = File.ReadAllText(file);
      int start = 0;
      while ((start = source.IndexOf("<FluentDataGrid ", start, StringComparison.Ordinal)) >= 0)
      {
        grids++;
        int end = source.IndexOf("</FluentDataGrid>", start, StringComparison.Ordinal);
        string grid = end < 0 ? source[start..] : source[start..end];
        string before = source[..start];
        bool framed = before.LastIndexOf("<DataGridFrame", StringComparison.Ordinal)
          > before.LastIndexOf("</DataGridFrame>", StringComparison.Ordinal);

        if (!framed)
        {
          offenders.Add($"{relative}: not inside <DataGridFrame>");
        }

        if (!grid.Contains("DisplayMode=\"DataGridDisplayMode.Grid\"", StringComparison.Ordinal)
          || !grid.Contains("GridTemplateColumns=", StringComparison.Ordinal))
        {
          offenders.Add($"{relative}: needs DisplayMode=\"DataGridDisplayMode.Grid\" and GridTemplateColumns");
        }

        if (grid.Contains(" Width=\"", StringComparison.Ordinal))
        {
          offenders.Add($"{relative}: a column sets Width; size columns with GridTemplateColumns minmax tracks");
        }

        start += "<FluentDataGrid ".Length;
      }
    }

    grids.ShouldBeGreaterThan(0, "No FluentDataGrid found under web-spa; the guard scans the wrong folder.");
    offenders.ShouldBeEmpty(
      "Every FluentDataGrid sits in DataGridFrame and sizes columns with GridTemplateColumns minmax tracks. Offenders: "
      + string.Join("; ", offenders));
    return Task.CompletedTask;
  }

  private static string FindRepoRoot()
  {
    DirectoryInfo? dir = new(AppContext.BaseDirectory);
    while (dir is not null)
    {
      if (File.Exists(Path.Combine(dir.FullName, "source", "Directory.Build.props")))
      {
        return dir.FullName;
      }

      dir = dir.Parent;
    }

    throw new InvalidOperationException("Could not locate repo root from " + AppContext.BaseDirectory);
  }
}
