#region Purpose
// Render ClassPill with HtmlRenderer and pin the mapped and unmapped looks, plus encoding.
#endregion

#region Design
// ClassPill is a leaf with no services, so a bare ServiceCollection host is enough.
// Unmapped adds twe-class-pill--unmapped. The default adds no modifier.
// The script case asserts the raw markup, before HtmlDecode, so the label stays encoded.
#endregion

namespace ClassPillRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using TimeWarp.Architecture.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;

[TestTag("Unit")]
public class ClassPill_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ClassPill_Should_>();

  public static async Task Render_A_Mapped_Label_Without_A_Modifier()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "Operations"
    });

    html.ShouldContain("twe-class-pill");
    html.ShouldContain("Operations");
    html.ShouldNotContain("twe-class-pill--unmapped");
    html.ShouldNotContain("<style");
  }

  public static async Task Mark_An_Unmapped_Label()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "Not mapped",
      ["Unmapped"] = true
    });

    html.ShouldContain("twe-class-pill--unmapped");
    html.ShouldContain("Not mapped");
  }

  public static async Task Pass_Extra_Attributes_To_The_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "Operations",
      ["data-testid"] = "class-mark",
      ["class"] = "caller-mark"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-class-pill");
    root.ShouldContain("data-testid=\"class-mark\"");
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
      (await renderer.RenderComponentAsync<ClassPill>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-class-pill", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "class pill rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }
}
