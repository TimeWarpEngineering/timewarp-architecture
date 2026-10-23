#region Purpose
// Render PermissionGrid with HtmlRenderer and pin groups, grant state, and encoding.
#endregion

#region Design
// PermissionGrid is a leaf with no services, so a bare ServiceCollection host is enough.
// A null or empty group list renders the root and no rows. A blank group label is
// omitted. Granted and Not granted are in the markup. The script case asserts the
// raw markup, before HtmlDecode.
#endregion

namespace PermissionGridRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class PermissionGrid_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<PermissionGrid_Should_>();

  public static async Task Render_Granted_And_Denied_Rows()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Groups"] = Sample()
    });

    html.ShouldContain("twe-perm-grid");
    html.ShouldContain("twe-perm-grid__eyebrow");
    html.ShouldContain("Work");
    html.ShouldContain("Administration");
    html.ShouldContain("View assigned work");
    html.ShouldContain("Delete records");
    html.ShouldContain("Granted");
    html.ShouldContain("Not granted");
    html.ShouldContain("is-denied");
    html.ShouldContain("twe-perm-grid__mark");
    html.ShouldContain("aria-hidden=\"true\"");
    html.ShouldNotContain("<style");
  }

  public static async Task Render_No_Rows_When_Groups_Are_Missing()
  {
    string empty = await RenderAsync(new Dictionary<string, object?>
    {
      ["Groups"] = Array.Empty<PermissionGroup>()
    });

    empty.ShouldContain("twe-perm-grid");
    empty.ShouldNotContain("twe-perm-grid__eyebrow");
    empty.ShouldNotContain("twe-perm-grid__row");

    string missing = await RenderAsync(new Dictionary<string, object?>
    {
      ["Groups"] = null
    });

    missing.ShouldContain("twe-perm-grid");
    missing.ShouldNotContain("twe-perm-grid__row");
  }

  public static async Task Omit_A_Blank_Group_Label()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Groups"] = new PermissionGroup[]
      {
        new("   ", [new PermissionItem("Alpha", true)])
      }
    });

    html.ShouldContain("Alpha");
    html.ShouldContain("Granted");
    html.ShouldNotContain("twe-perm-grid__eyebrow");
  }

  public static async Task Merge_Class_And_Pass_Data_Qa()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Groups"] = Sample(),
      ["Class"] = "caller-mark",
      ["data-qa"] = "grid-mark"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-perm-grid");
    root.ShouldContain("caller-mark");
    root.ShouldContain("data-qa=\"grid-mark\"");
  }

  public static async Task Encode_A_Script_Label()
  {
    string raw = await RenderRawAsync(new Dictionary<string, object?>
    {
      ["Groups"] = new PermissionGroup[]
      {
        new("Work", [new PermissionItem("<script>", false)])
      }
    });

    raw.ShouldNotContain("<script");
    raw.ShouldContain("&lt;script&gt;");
    raw.ShouldContain("Not granted");
    WebUtility.HtmlDecode(raw).ShouldContain("<script>");
  }

  private static PermissionGroup[] Sample() =>
  [
    new("Work",
    [
      new PermissionItem("View assigned work", true),
      new PermissionItem("Delete records", false)
    ]),
    new("Administration",
    [
      new PermissionItem("Change billing", false)
    ])
  ];

  private static async Task<string> RenderAsync(Dictionary<string, object?> parameters) =>
    WebUtility.HtmlDecode(await RenderRawAsync(parameters));

  private static async Task<string> RenderRawAsync(Dictionary<string, object?> parameters)
  {
    await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    return await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<PermissionGrid>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-perm-grid", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "permission grid rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }
}
