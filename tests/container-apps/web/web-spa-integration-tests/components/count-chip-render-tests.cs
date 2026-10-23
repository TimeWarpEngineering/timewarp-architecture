#region Purpose
// Render CountChip with HtmlRenderer and pin the value, accent modifier, and attribute passthrough.
#endregion

#region Design
// CountChip is a Tier-1 leaf with no DI, so a bare ServiceCollection host is enough.
// The root is a native span. A caller class binds to Class; other unmatched attributes splat onto
// it the same way SectionHeader passes data-qa. Accent false adds no modifier class.
#endregion

namespace CountChipRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class CountChip_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CountChip_Should_>();

  public static async Task Render_The_Value_Without_An_Accent_Class()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Value"] = 12
    });

    string root = RootTag(html);
    root.ShouldContain("twe-count-chip");
    root.ShouldNotContain("twe-count-chip--accent");
    html.ShouldContain(">12<");
    html.ShouldNotContain("<style");
  }

  public static async Task Render_Zero()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Value"] = 0
    });

    html.ShouldContain(">0<");
    RootTag(html).ShouldNotContain("twe-count-chip--accent");
  }

  public static async Task Add_The_Accent_Modifier()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Value"] = 7,
      ["Accent"] = true
    });

    string root = RootTag(html);
    root.ShouldContain("twe-count-chip");
    root.ShouldContain("twe-count-chip--accent");
    html.ShouldContain(">7<");
  }

  public static async Task Merge_Class_And_Pass_Data_Qa()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Value"] = 1,
      ["data-qa"] = "chip-mark",
      ["class"] = "caller-mark"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-count-chip");
    root.ShouldContain("caller-mark");
    root.ShouldNotContain("twe-count-chip--accent");
    root.ShouldContain("data-qa=\"chip-mark\"");
  }

  private static async Task<string> RenderAsync(Dictionary<string, object?> parameters) =>
    WebUtility.HtmlDecode(await RenderRawAsync(parameters));

  private static async Task<string> RenderRawAsync(Dictionary<string, object?> parameters)
  {
    await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    return await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<CountChip>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-count-chip", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "count chip rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }
}
