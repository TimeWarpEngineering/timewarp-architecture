#region Purpose
// Render KpiStrip with HtmlRenderer and pin child content and attribute passthrough.
#endregion

#region Design
// KpiStrip is a Tier-1 composite with no DI, so a bare ServiceCollection host is enough.
// The root is a native div; unmatched attributes splat onto it. Child content is the caller's.
// The component authors no style element. The grid itself is CSS, not markup.
#endregion

namespace KpiStripRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class KpiStrip_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<KpiStrip_Should_>();

  public static async Task Render_Child_Content_Inside_The_Strip()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["ChildContent"] = (RenderFragment)(builder =>
      {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "data-qa", "StripChild");
        builder.AddContent(2, "12");
        builder.CloseElement();
      })
    });

    string root = RootTag(html);
    root.ShouldContain("twe-kpi-strip");
    int stripAt = html.IndexOf("twe-kpi-strip", StringComparison.Ordinal);
    int childAt = html.IndexOf("data-qa=\"StripChild\"", StringComparison.Ordinal);
    int closeAt = html.IndexOf("</div>", childAt, StringComparison.Ordinal);
    stripAt.ShouldBeGreaterThan(-1);
    childAt.ShouldBeGreaterThan(stripAt);
    closeAt.ShouldBeGreaterThan(childAt);
    html.ShouldContain("12");
    html.ShouldNotContain("<style");
  }

  public static async Task Pass_Extra_Attributes_To_The_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["data-testid"] = "strip-mark",
      ["class"] = "caller-mark"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-kpi-strip");
    root.ShouldContain("data-testid=\"strip-mark\"");
    root.ShouldContain("caller-mark");
  }

  private static async Task<string> RenderAsync(Dictionary<string, object?> parameters)
  {
    await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<KpiStrip>(ParameterView.FromDictionary(parameters))).ToHtmlString());
    return WebUtility.HtmlDecode(html);
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-kpi-strip", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "kpi strip rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }
}
