#region Purpose
// Render SectionHeader with HtmlRenderer and pin title, subtitle, aside, and attribute passthrough.
#endregion

#region Design
// SectionHeader is a Tier-1 leaf with no DI, so a bare ServiceCollection host is enough.
// The root is a native div; unmatched attributes splat onto it the same way Card passes data-qa.
// A blank subtitle is omitted. The component authors no style element.
#endregion

namespace SectionHeaderRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class SectionHeader_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SectionHeader_Should_>();

  public static async Task Render_Title_Only()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Reports"
    });

    Heading(html).ShouldBe("Reports");
    html.ShouldContain("twe-section-header__title");
    html.ShouldNotContain("twe-section-header__subtitle");
    html.ShouldNotContain("twe-section-header__aside");
    html.ShouldNotContain("<style");
    CountOpen(html, "h2").ShouldBe(1);
  }

  public static async Task Render_Subtitle_And_Hide_A_Blank_One()
  {
    string shown = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Reports",
      ["Subtitle"] = "Summary for the current period."
    });

    shown.ShouldContain("twe-section-header__subtitle");
    shown.ShouldContain("Summary for the current period.");

    string hidden = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Reports",
      ["Subtitle"] = "   "
    });

    hidden.ShouldNotContain("twe-section-header__subtitle");
  }

  public static async Task Render_Aside()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Reports",
      ["Aside"] = (RenderFragment)(builder =>
      {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "data-qa", "SectionAside");
        builder.AddContent(2, "Hint");
        builder.CloseElement();
      })
    });

    html.ShouldContain("twe-section-header__aside");
    html.ShouldContain("data-qa=\"SectionAside\"");
    html.ShouldContain("Hint");
  }

  public static async Task Pass_Extra_Attributes_To_The_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Reports",
      ["id"] = "reports-header",
      ["data-qa"] = "SectionMark"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-section-header");
    root.ShouldContain("id=\"reports-header\"");
    root.ShouldContain("data-qa=\"SectionMark\"");
  }

  private static async Task<string> RenderAsync(Dictionary<string, object?> parameters)
  {
    await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<SectionHeader>(ParameterView.FromDictionary(parameters))).ToHtmlString());
    return WebUtility.HtmlDecode(html);
  }

  private static string Heading(string html)
  {
    const string open = "<h2";
    int start = html.IndexOf(open, StringComparison.Ordinal);
    start.ShouldBeGreaterThan(-1, "section header rendered no h2");
    int content = html.IndexOf('>', start, StringComparison.Ordinal);
    int end = html.IndexOf("</h2", content, StringComparison.Ordinal);
    return html.Substring(content + 1, end - content - 1).Trim();
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-section-header", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "section header rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }

  private static int CountOpen(string html, string tagName)
  {
    int count = 0;
    string open = "<" + tagName;
    int index = 0;
    while (index < html.Length)
    {
      int found = html.IndexOf(open, index, StringComparison.Ordinal);
      if (found < 0)
      {
        break;
      }

      int after = found + open.Length;
      if (after >= html.Length || !char.IsAsciiLetterOrDigit(html[after]))
      {
        count++;
      }

      index = after;
    }

    return count;
  }
}
