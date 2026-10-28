#region Purpose
// Pin the numeric table utility: right-aligned tabular numerals, used on the grouped-table sample.
#endregion

#region Design
// The class is not a component. It lives beside .twe-table because isolation cannot stamp
// a th or td the page authors inside GroupedTable. The two-class selector beats the
// left-aligned header rule. The sample puts the class on one column's th and td.
#endregion

namespace TableNumeric_;

[TestTag("Unit")]
public class TableNumeric_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TableNumeric_Should_>();

  public static Task Live_With_The_Table_Rules()
  {
    string css = File.ReadAllText(SourceFile(
      "source",
      "container-apps",
      "web",
      "projects",
      "web-spa",
      "wwwroot",
      "css",
      "app.css"));

    css.ShouldContain(".twe-table th.twe-numeric,");
    css.ShouldContain(".twe-table td.twe-numeric {");
    css.ShouldContain("text-align: right;");
    css.ShouldContain("font-variant-numeric: tabular-nums;");
    return Task.CompletedTask;
  }

  public static Task Show_On_One_Grouped_Table_Column()
  {
    string sample = File.ReadAllText(SourceFile(
      "source",
      "container-apps",
      "web",
      "projects",
      "web-spa",
      "features",
      "design-system",
      "samples",
      "GroupedTableSample.razor"));

    sample.ShouldContain("<th class=\"twe-numeric\">Count</th>");
    sample.ShouldContain("<td class=\"twe-numeric\">2</td>");
    return Task.CompletedTask;
  }

  private static string SourceFile(params string[] relative)
  {
    DirectoryInfo? dir = new(AppContext.BaseDirectory);
    while (dir is not null)
    {
      string candidate = Path.Combine([dir.FullName, .. relative]);
      if (File.Exists(candidate))
      {
        return candidate;
      }

      dir = dir.Parent;
    }

    throw new InvalidOperationException("Could not locate " + string.Join('/', relative));
  }
}
