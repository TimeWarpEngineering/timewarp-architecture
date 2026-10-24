#region Purpose
// Render SlaChip with HtmlRenderer and pin the tier, the accessible name, and encoding.
#endregion

#region Design
// SlaChip is a leaf with no services, so a bare ServiceCollection host is enough.
// Good is the default. The dot is aria-hidden. The tier word stays in the markup.
// A blank label is omitted. The script case asserts the raw markup.
#endregion

namespace SlaChipRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using TimeWarp.Architecture.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;

[TestTag("Unit")]
public class SlaChip_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SlaChip_Should_>();

  public static async Task Default_To_Good_And_Name_The_Tier()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "98%"
    });

    html.ShouldContain("twe-sla-chip--good");
    html.ShouldContain("Good");
    html.ShouldContain("twe-sla-chip__label");
    html.ShouldContain("98%");
    html.ShouldContain("aria-hidden=\"true\"");
    html.ShouldNotContain("twe-sla-chip--warn");
    html.ShouldNotContain("<style");
  }

  public static async Task Render_Warning_And_Behind()
  {
    string warn = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "90%",
      ["Tone"] = SlaTone.Warn
    });

    warn.ShouldContain("twe-sla-chip--warn");
    warn.ShouldContain("Warning");
    warn.ShouldContain("90%");
    warn.ShouldNotContain("twe-sla-chip--bad");

    string bad = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "78%",
      ["Tone"] = SlaTone.Bad
    });

    bad.ShouldContain("twe-sla-chip--bad");
    bad.ShouldContain("Behind");
    bad.ShouldContain("78%");
    bad.ShouldNotContain("twe-sla-chip--good");
  }

  public static async Task Omit_A_Blank_Label()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "   ",
      ["Tone"] = SlaTone.Warn
    });

    html.ShouldContain("Warning");
    html.ShouldNotContain("twe-sla-chip__label");
  }

  public static async Task Pass_Extra_Attributes_To_The_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "98%",
      ["data-testid"] = "sla-mark",
      ["class"] = "caller-mark"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-sla-chip");
    root.ShouldContain("data-testid=\"sla-mark\"");
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
      (await renderer.RenderComponentAsync<SlaChip>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-sla-chip", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "sla chip rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }
}
