#region Purpose
// Render Avatar with HtmlRenderer and pin initials, size, attributes, and the hidden or labelled circle.
#endregion

#region Design
// Avatar is a Tier-1 leaf with no DI, so a bare ServiceCollection host is enough.
// The root is a native span. Medium adds no modifier. Large adds twe-avatar--large.
// Class and unmatched attributes land on that root. An empty name is an empty hidden circle
// even when AriaHidden is false. Decorative mode is aria-hidden and has no role. Labelled
// mode is role="img" and uses AriaLabel, or Name when that label is blank.
// The component authors no style element.
#endregion

namespace AvatarRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class Avatar_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Avatar_Should_>();

  public static async Task Render_The_Initials_On_A_Twe_Avatar_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Name"] = "Ada Lovelace"
    });

    RootTag(html).ShouldContain("twe-avatar");
    Inner(html).ShouldBe("AL");
    html.ShouldNotContain("<style");
  }

  public static async Task Leave_The_Default_Size_Without_A_Modifier()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Name"] = "Ada Lovelace"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-avatar");
    root.ShouldNotContain("twe-avatar--");
  }

  public static async Task Add_The_Large_Modifier()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Name"] = "Ada Lovelace",
      ["Size"] = InitialsAvatarSize.Large
    });

    string root = RootTag(html);
    root.ShouldContain("twe-avatar--large");
    root.ShouldNotContain("twe-avatar--small");
  }

  public static async Task Merge_Class_And_Extra_Attributes_Onto_The_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Name"] = "Ada Lovelace",
      ["Class"] = "is-sample",
      ["data-qa"] = "avatar-mark"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-avatar");
    root.ShouldContain("is-sample");
    root.ShouldContain("data-qa=\"avatar-mark\"");
    html.ShouldNotContain("<style");
  }

  public static async Task Render_An_Empty_Name_As_A_Hidden_Circle()
  {
    string blank = await RenderAsync(new Dictionary<string, object?>
    {
      ["Name"] = "   ",
      ["AriaHidden"] = false,
      ["AriaLabel"] = "Missing"
    });

    string blankRoot = RootTag(blank);
    blankRoot.ShouldContain("aria-hidden=\"true\"");
    blankRoot.ShouldContain("twe-avatar");
    blank.ShouldNotContain("role=");
    blank.ShouldNotContain("aria-label");
    Inner(blank).Trim().ShouldBe("");

    string empty = await RenderAsync(new Dictionary<string, object?>
    {
      ["Name"] = ""
    });

    RootTag(empty).ShouldContain("aria-hidden=\"true\"");
    empty.ShouldNotContain("role=");
    Inner(empty).Trim().ShouldBe("");
  }

  public static async Task Hide_A_Decorative_Circle_And_Give_It_No_Role()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Name"] = "Ada Lovelace"
    });

    string root = RootTag(html);
    root.ShouldContain("aria-hidden=\"true\"");
    html.ShouldNotContain("role=");
    Inner(html).ShouldBe("AL");
  }

  public static async Task Label_The_Circle_When_It_Is_Not_Hidden()
  {
    string named = await RenderAsync(new Dictionary<string, object?>
    {
      ["Name"] = "Ada Lovelace",
      ["AriaHidden"] = false,
      ["AriaLabel"] = "Account holder"
    });

    string namedRoot = RootTag(named);
    namedRoot.ShouldContain("role=\"img\"");
    namedRoot.ShouldContain("aria-label=\"Account holder\"");
    named.ShouldNotContain("aria-hidden");

    string fallback = await RenderAsync(new Dictionary<string, object?>
    {
      ["Name"] = "Ada Lovelace",
      ["AriaHidden"] = false,
      ["AriaLabel"] = "  "
    });

    string fallbackRoot = RootTag(fallback);
    fallbackRoot.ShouldContain("role=\"img\"");
    fallbackRoot.ShouldContain("aria-label=\"Ada Lovelace\"");
    fallback.ShouldNotContain("aria-hidden");
  }

  private static async Task<string> RenderAsync(Dictionary<string, object?> parameters) =>
    WebUtility.HtmlDecode(await RenderRawAsync(parameters));

  private static async Task<string> RenderRawAsync(Dictionary<string, object?> parameters)
  {
    await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    return await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<Avatar>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-avatar", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "avatar rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }

  private static string Inner(string html)
  {
    string root = RootTag(html);
    int start = html.IndexOf(root, StringComparison.Ordinal) + root.Length;
    int end = html.IndexOf("</span>", start, StringComparison.Ordinal);
    end.ShouldBeGreaterThan(-1, "avatar rendered no closing span");
    return html[start..end];
  }
}
