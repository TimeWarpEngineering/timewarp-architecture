#region Purpose
// Render GroupedTable and TableGroup with HtmlRenderer and pin one table, row groups, and attribute passthrough.
#endregion

#region Design
// Both components are Tier-1 leaves with no DI, so a bare ServiceCollection host is enough.
// GroupedTable's root is a native div. A caller class binds to Class; other unmatched attributes
// splat onto it. Child content is a thead plus TableGroup bodies, and ColumnCount cascades so
// each group header spans that many columns. A blank Label omits aria-label. A TableGroup
// rendered alone has no cascade and spans one column. The script case asserts the raw markup,
// before HtmlDecode, so the title stays encoded.
#endregion

namespace GroupedTableRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class GroupedTable_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GroupedTable_Should_>();

  public static async Task Render_One_Table_And_One_Tbody_Per_Group()
  {
    string html = await RenderAsync(Sample(label: "Work items"));

    Count(html, "<table").ShouldBe(1);
    Count(html, "<tbody").ShouldBe(2);
    html.ShouldContain("twe-grouped-table");
    html.ShouldContain("twe-table-group");
    html.ShouldNotContain("<style");
  }

  public static async Task Put_Aria_Label_On_The_Table_Only_When_Label_Has_Text()
  {
    string named = await RenderAsync(Sample(label: "Work items"));
    TableTag(named).ShouldContain("aria-label=\"Work items\"");

    string blank = await RenderAsync(Sample(label: "   "));
    TableTag(blank).ShouldNotContain("aria-label");

    string missing = await RenderAsync(Sample(label: null));
    TableTag(missing).ShouldNotContain("aria-label");
  }

  public static async Task Render_Each_Group_Header_As_A_Rowgroup_Cell_Spanning_The_Columns()
  {
    string html = await RenderAsync(Sample(label: "Work items"));

    Count(html, "scope=\"rowgroup\"").ShouldBe(2);
    Count(html, "colspan=\"3\"").ShouldBe(2);
    html.ShouldNotContain("colspan=\"1\"");
    html.ShouldContain("twe-table-group__title");
    html.ShouldContain("Open");
    html.ShouldContain("In progress");
  }

  public static async Task Show_The_Count_Chip_And_Meta_Only_When_They_Are_Set()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["ColumnCount"] = 3,
      ["Label"] = "Work items",
      ["ChildContent"] = (RenderFragment)(builder =>
      {
        builder.AddContent(0, Head());
        builder.AddContent(1, Group("Open", 2, "2 items", "Alpha"));
        builder.AddContent(2, Group("Holding", 0, null, "Beta"));
        builder.AddContent(3, Group("Closed", null, "   ", "Gamma"));
      })
    });

    Count(html, "class=\"twe-count-chip").ShouldBe(2);
    html.ShouldContain("twe-count-chip--accent");
    html.ShouldContain(">2<");
    html.ShouldContain(">0<");
    html.ShouldContain("twe-table-group__meta");
    html.ShouldContain("2 items");
    Count(html, "twe-table-group__meta").ShouldBe(1);
    html.ShouldContain("Closed");
    html.ShouldContain("Gamma");
  }

  public static async Task Render_Group_Rows_After_The_Header_In_The_Same_Tbody()
  {
    string html = await RenderAsync(Sample(label: "Work items"));

    string open = Slice(html, "<tbody", "</tbody>");
    open.ShouldContain("twe-table-group__header");
    open.ShouldContain("Open");
    open.ShouldContain("Alpha");
    open.IndexOf("twe-table-group__header", StringComparison.Ordinal)
      .ShouldBeLessThan(open.IndexOf("Alpha", StringComparison.Ordinal));

    int second = html.IndexOf("<tbody", html.IndexOf("</tbody>", StringComparison.Ordinal), StringComparison.Ordinal);
    string progress = Slice(html[second..], "<tbody", "</tbody>");
    progress.ShouldContain("In progress");
    progress.ShouldContain("Beta");
    progress.ShouldNotContain("Alpha");
  }

  public static async Task Merge_Class_And_Pass_Data_Qa_On_The_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["ColumnCount"] = 3,
      ["Label"] = "Work items",
      ["data-qa"] = "grouped-mark",
      ["class"] = "caller-mark",
      ["ChildContent"] = (RenderFragment)(builder =>
      {
        builder.AddContent(0, Head());
        builder.AddContent(1, Group("Open", 2, "2 items", "Alpha"));
      })
    });

    string root = RootTag(html);
    root.ShouldContain("twe-grouped-table");
    root.ShouldContain("caller-mark");
    root.ShouldContain("data-qa=\"grouped-mark\"");
  }

  public static async Task Encode_A_Script_Title()
  {
    string raw = await RenderRawAsync(new Dictionary<string, object?>
    {
      ["ColumnCount"] = 1,
      ["ChildContent"] = (RenderFragment)(builder =>
        builder.AddContent(0, Group("<script>", null, null, "Alpha")))
    });

    raw.ShouldNotContain("<script");
    raw.ShouldContain("&lt;script&gt;");
    WebUtility.HtmlDecode(raw).ShouldContain("<script>");
  }

  public static async Task Use_A_Single_Column_When_A_Group_Is_Rendered_Alone()
  {
    string html = await RenderGroupAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Open",
      ["data-qa"] = "group-mark",
      ["class"] = "caller-mark",
      ["ChildContent"] = (RenderFragment)(builder =>
      {
        builder.OpenElement(0, "tr");
        builder.OpenElement(1, "td");
        builder.AddContent(2, "Alpha");
        builder.CloseElement();
        builder.CloseElement();
      })
    });

    string root = RootTag(html, "twe-table-group");
    root.ShouldContain("twe-table-group");
    root.ShouldContain("caller-mark");
    root.ShouldContain("data-qa=\"group-mark\"");
    html.ShouldContain("colspan=\"1\"");
    html.ShouldContain("scope=\"rowgroup\"");
    html.ShouldContain("Alpha");
    html.ShouldNotContain("twe-count-chip");
    html.ShouldNotContain("twe-table-group__meta");
  }

  private static Dictionary<string, object?> Sample(string? label) => new()
  {
    ["ColumnCount"] = 3,
    ["Label"] = label,
    ["ChildContent"] = (RenderFragment)(builder =>
    {
      builder.AddContent(0, Head());
      builder.AddContent(1, Group("Open", 2, "2 items", "Alpha"));
      builder.AddContent(2, Group("In progress", 1, null, "Beta"));
    })
  };

  private static RenderFragment Head() => builder =>
  {
    builder.OpenElement(0, "thead");
    builder.OpenElement(1, "tr");
    builder.OpenElement(2, "th");
    builder.AddContent(3, "Item");
    builder.CloseElement();
    builder.OpenElement(4, "th");
    builder.AddContent(5, "Note");
    builder.CloseElement();
    builder.OpenElement(6, "th");
    builder.AddContent(7, "Updated");
    builder.CloseElement();
    builder.CloseElement();
    builder.CloseElement();
  };

  private static RenderFragment Group(string title, int? count, string? meta, string rowText) => builder =>
  {
    builder.OpenComponent<TableGroup>(0);
    builder.AddComponentParameter(1, "Title", title);
    if (count is not null)
    {
      builder.AddComponentParameter(2, "Count", count);
    }

    if (meta is not null)
    {
      builder.AddComponentParameter(3, "Meta", meta);
    }

    builder.AddComponentParameter(4, "ChildContent", Row(rowText));
    builder.CloseComponent();
  };

  private static RenderFragment Row(string rowText) => builder =>
  {
    builder.OpenElement(0, "tr");
    builder.OpenElement(1, "td");
    builder.AddContent(2, rowText);
    builder.CloseElement();
    builder.CloseElement();
  };

  private static async Task<string> RenderAsync(Dictionary<string, object?> parameters) =>
    WebUtility.HtmlDecode(await RenderRawAsync(parameters));

  private static async Task<string> RenderRawAsync(Dictionary<string, object?> parameters)
  {
    await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    return await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<GroupedTable>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }

  private static async Task<string> RenderGroupAsync(Dictionary<string, object?> parameters)
  {
    await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<TableGroup>(ParameterView.FromDictionary(parameters))).ToHtmlString());
    return WebUtility.HtmlDecode(html);
  }

  private static string TableTag(string html)
  {
    int start = html.IndexOf("<table", StringComparison.Ordinal);
    start.ShouldBeGreaterThan(-1, "grouped table rendered no table");
    int end = html.IndexOf('>', start, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }

  private static string RootTag(string html, string marker = "twe-grouped-table")
  {
    int classAt = html.IndexOf(marker, StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }

  private static string Slice(string html, string startMarker, string endMarker)
  {
    int start = html.IndexOf(startMarker, StringComparison.Ordinal);
    start.ShouldBeGreaterThan(-1, startMarker);
    int end = html.IndexOf(endMarker, start, StringComparison.Ordinal);
    end.ShouldBeGreaterThan(-1, endMarker);
    return html.Substring(start, end + endMarker.Length - start);
  }

  private static int Count(string html, string needle)
  {
    int count = 0;
    int index = 0;
    while (index < html.Length)
    {
      int found = html.IndexOf(needle, index, StringComparison.Ordinal);
      if (found < 0)
      {
        break;
      }

      count++;
      index = found + needle.Length;
    }

    return count;
  }
}
