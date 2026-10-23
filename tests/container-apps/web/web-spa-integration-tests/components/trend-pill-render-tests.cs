#region Purpose
// Render TrendPill with HtmlRenderer and pin direction, the accessible name, and encoding.
#endregion

#region Design
// TrendPill is a leaf with no services, so a bare ServiceCollection host is enough.
// Flat is the default and omits a blank label. The glyph is aria-hidden. The direction
// word stays in the markup for assistive tech. The script case asserts the raw markup.
#endregion

namespace TrendPillRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using TimeWarp.Architecture.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;

[TestTag("Unit")]
public class TrendPill_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TrendPill_Should_>();

  public static async Task Default_To_Flat_And_Omit_A_Blank_Label()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "   "
    });

    html.ShouldContain("twe-trend--flat");
    html.ShouldContain("—");
    html.ShouldContain("Flat");
    html.ShouldContain("aria-hidden=\"true\"");
    html.ShouldNotContain("twe-trend__label");
    html.ShouldNotContain("twe-trend--up");
    html.ShouldNotContain("<style");
  }

  public static async Task Render_Up_And_Down_With_The_Label()
  {
    string up = await RenderAsync(new Dictionary<string, object?>
    {
      ["Direction"] = TrendDirection.Up,
      ["Label"] = "4.2%"
    });

    up.ShouldContain("twe-trend--up");
    up.ShouldContain("↑");
    up.ShouldContain("Up");
    up.ShouldContain("twe-trend__label");
    up.ShouldContain("4.2%");
    up.ShouldNotContain("twe-trend--down");

    string down = await RenderAsync(new Dictionary<string, object?>
    {
      ["Direction"] = TrendDirection.Down,
      ["Label"] = "1.8%"
    });

    down.ShouldContain("twe-trend--down");
    down.ShouldContain("↓");
    down.ShouldContain("Down");
    down.ShouldContain("1.8%");
    down.ShouldNotContain("twe-trend--up");
  }

  public static async Task Pass_Extra_Attributes_To_The_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Direction"] = TrendDirection.Up,
      ["Label"] = "4.2%",
      ["data-testid"] = "trend-mark",
      ["class"] = "caller-mark"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-trend");
    root.ShouldContain("data-testid=\"trend-mark\"");
    root.ShouldContain("caller-mark");
  }

  public static async Task Encode_A_Script_Label()
  {
    string raw = await RenderRawAsync(new Dictionary<string, object?>
    {
      ["Direction"] = TrendDirection.Up,
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
      (await renderer.RenderComponentAsync<TrendPill>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-trend", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "trend pill rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }
}
