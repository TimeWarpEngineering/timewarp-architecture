#region Purpose
// Render ChartLegendItem with HtmlRenderer and pin the dot tone, label, and encoding.
#endregion

#region Design
// ChartLegendItem is a leaf with no services, so a bare ServiceCollection host is enough.
// The dot is decorative. Tone Ink, Primary, and Positive each add their modifier.
// The script case asserts the raw markup, before HtmlDecode, so the label stays encoded.
#endregion

namespace ChartLegendItemRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using TimeWarp.Architecture.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;

[TestTag("Unit")]
public class ChartLegendItem_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ChartLegendItem_Should_>();

  public static async Task Render_The_Label_And_Default_To_Ink()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "Series A"
    });

    html.ShouldContain("twe-chart-legend__label");
    html.ShouldContain("Series A");
    html.ShouldContain("twe-chart-legend__dot--ink");
    html.ShouldContain("aria-hidden=\"true\"");
    html.ShouldNotContain("twe-chart-legend__dot--primary");
    html.ShouldNotContain("<style");
  }

  public static async Task Apply_Primary_And_Positive_Tones()
  {
    string primary = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "Series B",
      ["Tone"] = ChartSeriesTone.Primary
    });

    primary.ShouldContain("twe-chart-legend__dot--primary");
    primary.ShouldNotContain("twe-chart-legend__dot--ink");

    string positive = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "Series C",
      ["Tone"] = ChartSeriesTone.Positive
    });

    positive.ShouldContain("twe-chart-legend__dot--positive");
    positive.ShouldNotContain("twe-chart-legend__dot--primary");
  }

  public static async Task Pass_Extra_Attributes_To_The_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "Series A",
      ["data-testid"] = "legend-mark",
      ["class"] = "caller-mark"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-chart-legend");
    root.ShouldContain("data-testid=\"legend-mark\"");
    root.ShouldContain("caller-mark");
  }

  public static async Task Encode_A_Script_Label()
  {
    string raw = await RenderRawAsync(new Dictionary<string, object?>
    {
      ["Label"] = "<script>"
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
      (await renderer.RenderComponentAsync<ChartLegendItem>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-chart-legend\"", StringComparison.Ordinal);
    if (classAt < 0)
    {
      classAt = html.IndexOf("twe-chart-legend ", StringComparison.Ordinal);
    }

    classAt.ShouldBeGreaterThan(-1, "legend item rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }
}
