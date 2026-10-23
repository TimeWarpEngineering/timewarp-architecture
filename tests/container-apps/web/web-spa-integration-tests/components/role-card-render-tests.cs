#region Purpose
// Render RoleCard with HtmlRenderer and pin selection, the optional description, and encoding.
#endregion

#region Design
// RoleCard is a leaf with no services, so a bare ServiceCollection host is enough.
// Selected sets aria-checked and the check. A blank description is omitted.
// tabindex rides unmatched attributes, which is how a parent sets the roving
// tab stop. The script case asserts the raw markup, before HtmlDecode.
#endregion

namespace RoleCardRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class RoleCard_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RoleCard_Should_>();

  public static async Task Render_A_Selected_Option()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Owner",
      ["Description"] = "Full access to billing, people, and records.",
      ["Selected"] = true
    });

    string root = RootTag(html);
    root.ShouldContain("twe-role-card");
    root.ShouldContain("is-selected");
    root.ShouldContain("type=\"button\"");
    root.ShouldContain("role=\"radio\"");
    root.ShouldContain("aria-checked=\"true\"");
    html.ShouldContain("twe-role-card__title");
    html.ShouldContain("Owner");
    html.ShouldContain("twe-role-card__desc");
    html.ShouldContain("Full access to billing, people, and records.");
    html.ShouldContain("twe-role-card__check");
    html.ShouldContain("aria-hidden=\"true\"");
    html.ShouldNotContain("<style");
  }

  public static async Task Omit_The_Check_And_A_Blank_Description()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Staff",
      ["Description"] = "   ",
      ["Selected"] = false
    });

    string root = RootTag(html);
    root.ShouldContain("aria-checked=\"false\"");
    root.ShouldNotContain("is-selected");
    html.ShouldContain("Staff");
    html.ShouldNotContain("twe-role-card__check");
    html.ShouldNotContain("twe-role-card__desc");
  }

  public static async Task Merge_Class_And_Pass_Data_Qa()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Admin",
      ["Class"] = "caller-mark",
      ["data-qa"] = "card-mark",
      ["tabindex"] = "0"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-role-card");
    root.ShouldContain("caller-mark");
    root.ShouldContain("data-qa=\"card-mark\"");
    root.ShouldContain("tabindex=\"0\"");
    root.ShouldContain("aria-checked=\"false\"");
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
      (await renderer.RenderComponentAsync<RoleCard>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-role-card", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "role card rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }
}
