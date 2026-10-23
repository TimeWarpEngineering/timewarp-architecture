#region Purpose
// Render DevNote with HtmlRenderer and pin the slots, attribute passthrough, and encoding.
#endregion

#region Design
// DevNote is a leaf with no services, so a bare ServiceCollection host is enough.
// A blank eyebrow or title is omitted. ChildContent is the body. The icon tile is
// aria-hidden. The script case asserts the raw markup, before HtmlDecode.
#endregion

namespace DevNoteRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class DevNote_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<DevNote_Should_>();

  public static async Task Render_The_Eyebrow_Title_And_Body()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Eyebrow"] = "Note",
      ["Title"] = "How a note reads",
      ["ChildContent"] = (RenderFragment)(builder =>
      {
        builder.AddContent(0, "See ");
        builder.OpenElement(1, "code");
        builder.AddContent(2, "inline");
        builder.CloseElement();
      })
    });

    string root = RootTag(html);
    root.ShouldContain("twe-devnote");
    root.ShouldContain("role=\"note\"");
    html.ShouldContain("twe-devnote__icon");
    html.ShouldContain("aria-hidden=\"true\"");
    html.ShouldContain("twe-devnote__eyebrow");
    html.ShouldContain("Note");
    html.ShouldContain("twe-devnote__title");
    html.ShouldContain("How a note reads");
    html.ShouldContain("twe-devnote__desc");
    html.ShouldContain("<code");
    html.ShouldContain("inline");
    html.ShouldNotContain("<style");
  }

  public static async Task Omit_Blank_Slots()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Eyebrow"] = "   ",
      ["Title"] = "   "
    });

    html.ShouldContain("twe-devnote__icon");
    html.ShouldNotContain("twe-devnote__eyebrow");
    html.ShouldNotContain("twe-devnote__title");
    html.ShouldNotContain("twe-devnote__desc");
  }

  public static async Task Merge_Class_And_Pass_Data_Qa()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "How a note reads",
      ["Class"] = "caller-mark",
      ["data-qa"] = "note-mark"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-devnote");
    root.ShouldContain("caller-mark");
    root.ShouldContain("data-qa=\"note-mark\"");
    root.ShouldContain("role=\"note\"");
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
      (await renderer.RenderComponentAsync<DevNote>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-devnote", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "dev note rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }
}
