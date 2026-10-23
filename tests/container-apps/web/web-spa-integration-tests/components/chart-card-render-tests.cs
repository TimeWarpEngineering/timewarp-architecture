#region Purpose
// Render ChartCard with HtmlRenderer and pin the frame slots, heading level, Ask Tag mark, and encoding.
#endregion

#region Design
// ChartCard is a leaf with no services, so a bare ServiceCollection host is enough.
// The root is a native section. A caller class merges onto twe-chart-card. Unmatched
// attributes splat onto the root and override the Ask Tag mark. A null header, filter,
// plot, or legend is omitted. HeadingLevel outside 2–6 stays h2.
// The script case asserts the raw markup, before HtmlDecode, so the title stays encoded.
#endregion

namespace ChartCardRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using TimeWarp.Architecture.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;

[TestTag("Unit")]
public class ChartCard_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ChartCard_Should_>();

  public static async Task Render_The_Title_And_Omit_Empty_Slots()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Sample series"
    });

    html.ShouldContain("twe-chart-card__title");
    html.ShouldContain("<h2");
    html.ShouldContain("Sample series");
    html.ShouldContain("data-ask-tag=\"Card\"");
    html.ShouldContain("data-ask-tag-label=\"Sample series\"");
    html.ShouldNotContain("twe-chart-card__controls");
    html.ShouldNotContain("twe-chart-card__filter");
    html.ShouldNotContain("twe-chart-card__plot");
    html.ShouldNotContain("twe-chart-card__legend");
    html.ShouldNotContain("<style");
  }

  public static async Task Render_Header_Filter_Plot_And_Legend_When_Set()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Sample series",
      ["Header"] = (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class=\"hdr-mark\">Period</span>")),
      ["Filter"] = (RenderFragment)(builder => builder.AddMarkupContent(1, "<span class=\"filter-mark\">All locations</span>")),
      ["ChildContent"] = (RenderFragment)(builder => builder.AddMarkupContent(2, "<span class=\"plot-mark\">Plot</span>")),
      ["Legend"] = (RenderFragment)(builder => builder.AddMarkupContent(3, "<span class=\"legend-mark\">Series A</span>"))
    });

    html.ShouldContain("twe-chart-card__controls");
    html.ShouldContain("hdr-mark");
    html.ShouldContain("twe-chart-card__filter");
    html.ShouldContain("filter-mark");
    html.ShouldContain("twe-chart-card__plot");
    html.ShouldContain("plot-mark");
    html.ShouldContain("twe-chart-card__legend");
    html.ShouldContain("legend-mark");
  }

  public static async Task Use_HeadingLevel_And_Fall_Back_Outside_2_Through_6()
  {
    string third = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Sample series",
      ["HeadingLevel"] = 3
    });

    third.ShouldContain("<h3");
    third.ShouldNotContain("<h2");

    string fallback = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Sample series",
      ["HeadingLevel"] = 1
    });

    fallback.ShouldContain("<h2");
    fallback.ShouldNotContain("<h1");
  }

  public static async Task Pass_Extra_Attributes_To_The_Root_And_Let_Them_Override_Ask_Tag()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Sample series",
      ["data-testid"] = "chart-mark",
      ["class"] = "caller-mark",
      ["data-ask-tag"] = "Note"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-chart-card");
    root.ShouldContain("data-testid=\"chart-mark\"");
    root.ShouldContain("caller-mark");
    root.ShouldContain("data-ask-tag=\"Note\"");
    root.ShouldNotContain("data-ask-tag=\"Card\"");
  }

  public static async Task Encode_A_Script_Title()
  {
    string raw = await RenderRawAsync(new Dictionary<string, object?>
    {
      ["Title"] = "<script>"
    });

    raw.ShouldNotContain("<script");
    raw.ShouldContain("&lt;script&gt;");
    WebUtility.HtmlDecode(raw).ShouldContain("<script>");
  }

  private static async Task<string> RenderAsync(Dictionary<string, object?> parameters) =>
    WebUtility.HtmlDecode(await RenderRawAsync(parameters));

  private static async Task<string> RenderRawAsync(Dictionary<string, object?> parameters)
  {
    await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    return await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<ChartCard>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-chart-card", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "chart card rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }
}
