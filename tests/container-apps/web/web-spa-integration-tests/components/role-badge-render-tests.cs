#region Purpose
// Render RoleBadge with HtmlRenderer and pin the status, the unchanged label, and encoding.
#endregion

#region Design
// RoleBadge renders StatusBadge and adds no services, so a bare ServiceCollection host
// is enough. The label is the caller's role text. A blank role does not fall back to
// the status name. Class and data-qa land on the badge. The script case asserts the
// raw markup, before HtmlDecode.
#endregion

namespace RoleBadgeRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class RoleBadge_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RoleBadge_Should_>();

  public static async Task Render_The_Role_On_The_Caller_Status()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Role"] = "Owner",
      ["Status"] = BadgeStatus.Warning
    });

    string root = RootTag(html);
    root.ShouldContain("twe-badge");
    root.ShouldContain("twe-badge--warning");
    html.ShouldContain("Owner");
    html.ShouldNotContain("Warning", Case.Sensitive);
    html.ShouldNotContain("<style");
  }

  public static async Task Keep_The_Caller_Text()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Role"] = "API Admin",
      ["Status"] = BadgeStatus.Info
    });

    html.ShouldContain("API Admin");
    html.ShouldContain("twe-badge--info");
    html.ShouldNotContain("Api admin", Case.Sensitive);
  }

  public static async Task Use_An_Empty_Label_When_The_Role_Is_Blank()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Role"] = "   "
    });

    RootTag(html).ShouldContain("twe-badge--inactive");
    html.ShouldNotContain("Inactive", Case.Sensitive);
  }

  public static async Task Merge_Class_And_Pass_Data_Qa()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Role"] = "Staff",
      ["Status"] = BadgeStatus.Inactive,
      ["Class"] = "caller-mark",
      ["data-qa"] = "role-mark"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-badge--inactive");
    root.ShouldContain("caller-mark");
    root.ShouldContain("data-qa=\"role-mark\"");
    html.ShouldContain("Staff");
  }

  public static async Task Encode_A_Script_Role()
  {
    string raw = await RenderRawAsync(new Dictionary<string, object?>
    {
      ["Role"] = "<script>",
      ["Status"] = BadgeStatus.Info
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
      (await renderer.RenderComponentAsync<RoleBadge>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-badge", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "role badge rendered no pill");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }
}
