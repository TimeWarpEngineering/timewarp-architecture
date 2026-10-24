#region Purpose
// Render UserCard with HtmlRenderer and pin the name, detail, link, and attributes.
#endregion

#region Design
// UserCard is a Tier-1 leaf. Avatar needs no services, so a bare ServiceCollection host is enough.
// Href makes the root an anchor. A blank Href makes the root a div. A blank Detail omits the
// detail line. Class and unmatched attributes land on that root. The component authors no style element.
#endregion

namespace UserCardRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class UserCard_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<UserCard_Should_>();

  public static async Task Render_The_Name_And_Initials()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Name"] = "Ada Lovelace"
    });

    RootTag(html).ShouldContain("twe-user-card");
    html.ShouldContain("twe-user-card__name");
    html.ShouldContain("Ada Lovelace");
    html.ShouldContain("AL");
    html.ShouldNotContain("twe-user-card__detail");
    html.ShouldNotContain("<style");
  }

  public static async Task Omit_A_Blank_Detail()
  {
    string blank = await RenderAsync(new Dictionary<string, object?>
    {
      ["Name"] = "Ada Lovelace",
      ["Detail"] = "   "
    });

    blank.ShouldNotContain("twe-user-card__detail");

    string shown = await RenderAsync(new Dictionary<string, object?>
    {
      ["Name"] = "Ada Lovelace",
      ["Detail"] = "ada@example.test"
    });

    shown.ShouldContain("twe-user-card__detail");
    shown.ShouldContain("ada@example.test");
  }

  public static async Task Render_An_Anchor_When_Href_Is_Set()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Name"] = "Ada Lovelace",
      ["Href"] = "/Profile"
    });

    string root = RootTag(html);
    root.ShouldStartWith("<a ");
    root.ShouldContain("class=\"twe-user-card\"");
    root.ShouldContain("href=\"/Profile\"");
  }

  public static async Task Render_A_Div_When_Href_Is_Blank()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Name"] = "Ada Lovelace",
      ["Href"] = "  "
    });

    string root = RootTag(html);
    root.ShouldStartWith("<div ");
    root.ShouldContain("class=\"twe-user-card\"");
    root.ShouldNotContain("href");
  }

  public static async Task Merge_Class_And_Extra_Attributes_Onto_The_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Name"] = "Ada Lovelace",
      ["Class"] = "is-sample",
      ["data-qa"] = "user-mark"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-user-card");
    root.ShouldContain("is-sample");
    root.ShouldContain("data-qa=\"user-mark\"");
    html.ShouldNotContain("<style");
  }

  private static async Task<string> RenderAsync(Dictionary<string, object?> parameters)
  {
    await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<UserCard>(ParameterView.FromDictionary(parameters))).ToHtmlString());
    return WebUtility.HtmlDecode(html);
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-user-card", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "user card rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }
}
