#region Purpose
// Render StatusBadge with HtmlRenderer and pin the error status, a caller label, and encoding.
#endregion

#region Design
// StatusBadge is a leaf with no services, so a bare ServiceCollection host is enough.
// Error maps to twe-badge--error. The default label is the enum name. Label replaces it.
// The script case asserts the raw markup, before HtmlDecode, so the label stays encoded.
#endregion

namespace StatusBadgeRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using TimeWarp.Architecture.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;

[TestTag("Unit")]
public class StatusBadge_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<StatusBadge_Should_>();

  public static async Task Render_Error_And_Keep_Active()
  {
    string error = await RenderAsync(new Dictionary<string, object?>
    {
      ["Status"] = BadgeStatus.Error
    });

    error.ShouldContain("twe-badge--error");
    error.ShouldContain("Error");
    error.ShouldNotContain("twe-badge--active");
    error.ShouldNotContain("<style");

    string active = await RenderAsync(new Dictionary<string, object?>
    {
      ["Status"] = BadgeStatus.Active
    });

    active.ShouldContain("twe-badge--active");
    active.ShouldContain("Active");
    active.ShouldNotContain("twe-badge--error");
  }

  public static async Task Use_A_Caller_Label()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Status"] = BadgeStatus.Error,
      ["Label"] = "Lost",
      ["Class"] = "caller-mark"
    });

    html.ShouldContain("twe-badge--error");
    html.ShouldContain("Lost");
    html.ShouldContain("caller-mark");
    html.ShouldNotContain("Error", Case.Sensitive);
  }

  public static async Task Encode_A_Script_Label()
  {
    string raw = await RenderRawAsync(new Dictionary<string, object?>
    {
      ["Status"] = BadgeStatus.Error,
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
      (await renderer.RenderComponentAsync<StatusBadge>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }
}
