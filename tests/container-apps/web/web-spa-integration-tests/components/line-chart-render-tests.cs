#region Purpose
// Render LineChart with HtmlRenderer and pin series, the last-point dot, axis labels, and passthrough.
#endregion

#region Design
// LineChart is a leaf with no services, so a bare ServiceCollection host is enough.
// The root is a native svg. The page passes points, a tone, and already-formatted axis labels.
// The plot draws gridlines, a line when a series has two finite points, and a dot on the last point.
// A money-looking label is copied through. The plot does not invent a currency sign.
// The script case asserts the raw markup, before HtmlDecode, so the label stays encoded.
#endregion

namespace LineChartRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class LineChart_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<LineChart_Should_>();

  public static async Task Draw_Gridlines_Lines_And_A_Dot_On_The_Last_Point()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["AxisLabels"] = new[] { "Jan", "Feb" },
      ["Series"] = new LineChartSeries[]
      {
        new("Series A", ChartSeriesTone.Ink, [0, 10]),
        new("Series B", ChartSeriesTone.Primary, [4, 6]),
        new("Series C", ChartSeriesTone.Positive, [1, 2])
      }
    });

    html.ShouldContain("twe-line-chart__grid");
    Count(html, "<line ").ShouldBe(4);
    html.ShouldContain("twe-line-chart__line--ink");
    html.ShouldContain("twe-line-chart__line--primary");
    html.ShouldContain("twe-line-chart__line--positive");
    html.ShouldContain("twe-line-chart__dot--ink");
    html.ShouldContain("twe-line-chart__dot--primary");
    html.ShouldContain("twe-line-chart__dot--positive");
    html.ShouldContain(">Jan<");
    html.ShouldContain(">Feb<");
    html.ShouldContain("aria-label=\"Series A, Series B, Series C. Jan, Feb\"");
    html.ShouldNotContain("<style");
    Count(html, "<circle ").ShouldBe(3);
    DotMatchesLineEnd(html, "twe-line-chart__line--ink", "twe-line-chart__dot--ink");
  }

  public static async Task Keep_An_Already_Formatted_Axis_Label()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["AxisLabels"] = new[] { "$1,240" },
      ["Series"] = new LineChartSeries[]
      {
        new("Series A", ChartSeriesTone.Ink, [1240])
      }
    });

    html.ShouldContain(">$1,240<");
    html.ShouldContain("aria-label=\"Series A. $1,240\"");
    Count(html, "$1,240").ShouldBe(2);
    html.ShouldNotContain("<polyline");
    Count(html, "<circle ").ShouldBe(1);
  }

  public static async Task Omit_A_Series_With_No_Finite_Points()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["AxisLabels"] = new[] { "Jan" },
      ["Series"] = new LineChartSeries[]
      {
        new("Empty", ChartSeriesTone.Ink, []),
        new("Missing", ChartSeriesTone.Primary, [double.NaN]),
        new("Series A", ChartSeriesTone.Positive, [1, 2])
      }
    });

    html.ShouldNotContain("twe-line-chart__line--ink");
    html.ShouldNotContain("twe-line-chart__dot--ink");
    html.ShouldNotContain("twe-line-chart__line--primary");
    html.ShouldContain("twe-line-chart__line--positive");
    html.ShouldContain("aria-label=\"Empty, Missing, Series A. Jan\"");
    Count(html, "<circle ").ShouldBe(1);
  }

  public static async Task Name_An_Empty_Plot_Line_Chart()
  {
    string html = await RenderAsync(new Dictionary<string, object?>());

    RootTag(html).ShouldContain("aria-label=\"Line chart\"");
    Count(html, "<line ").ShouldBe(4);
    html.ShouldNotContain("<polyline");
    html.ShouldNotContain("<circle");
    html.ShouldNotContain("<text");
  }

  public static async Task Pass_Extra_Attributes_To_The_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["data-testid"] = "chart-mark",
      ["class"] = "caller-mark",
      ["aria-label"] = "Caller name"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-line-chart");
    root.ShouldContain("caller-mark");
    root.ShouldContain("data-testid=\"chart-mark\"");
    root.ShouldContain("aria-label=\"Caller name\"");
    root.ShouldNotContain("Line chart");
  }

  public static async Task Encode_A_Script_Label()
  {
    string raw = await RenderRawAsync(new Dictionary<string, object?>
    {
      ["AxisLabels"] = new[] { "<script>" },
      ["Series"] = new LineChartSeries[]
      {
        new("<script>", ChartSeriesTone.Ink, [1])
      }
    });

    raw.ShouldNotContain("<script");
    raw.ShouldNotContain("<style");
    raw.ShouldContain("&lt;script&gt;");
    WebUtility.HtmlDecode(raw).ShouldContain("<script>");
  }

  private static void DotMatchesLineEnd(string html, string lineClass, string dotClass)
  {
    string line = TagContaining(html, lineClass);
    string points = Attribute(line, "points");
    string last = points.Split(' ', StringSplitOptions.RemoveEmptyEntries)[^1];
    string[] pair = last.Split(',');
    string dot = TagContaining(html, dotClass);
    dot.ShouldContain("cx=\"" + pair[0] + "\"");
    dot.ShouldContain("cy=\"" + pair[1] + "\"");
  }

  private static string Attribute(string tag, string name)
  {
    string marker = name + "=\"";
    int start = tag.IndexOf(marker, StringComparison.Ordinal);
    start.ShouldBeGreaterThan(-1, name);
    start += marker.Length;
    int end = tag.IndexOf('"', start, StringComparison.Ordinal);
    end.ShouldBeGreaterThan(start, name);
    return tag.Substring(start, end - start);
  }

  private static string TagContaining(string html, string marker)
  {
    int classAt = html.IndexOf(marker, StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, marker);
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }

  private static async Task<string> RenderAsync(Dictionary<string, object?> parameters) =>
    WebUtility.HtmlDecode(await RenderRawAsync(parameters));

  private static async Task<string> RenderRawAsync(Dictionary<string, object?> parameters)
  {
    await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    return await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<LineChart>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-line-chart", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "line chart rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
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
