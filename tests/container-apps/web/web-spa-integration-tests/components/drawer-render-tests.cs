#region Purpose
// Render Drawer with HtmlRenderer and pin the closed shell, the open dialog, slots, and the dismiss control.
#endregion

#region Design
// Drawer hosts a FluentUI button, so the renderer needs FluentUI services and a no-op JS
// runtime. The component injects IJSRuntime and skips the module when the renderer is not
// interactive, which HtmlRenderer is not. Closed output has no title text. Open output is a
// dialog with twe-drawer, the side class, aria-modal, and aria-labelledby equal to the title
// element's id. Two instances mint different title ids. The footer element exists only when
// Footer is set. Class and unmatched attributes land on the dialog. The dismiss control's
// accessible name is Close and its icon is aria-hidden.
#endregion

namespace DrawerRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class Drawer_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Drawer_Should_>();

  public static async Task Render_Nothing_Named_By_The_Title_When_Closed()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Hidden title",
      ["IsOpen"] = false
    });

    html.ShouldNotContain("Hidden title");
    html.ShouldNotContain("twe-drawer__title");
    html.ShouldNotContain("aria-modal");
    html.ShouldNotContain("<style");
  }

  public static async Task Render_An_Open_Dialog_Labelled_By_Its_Title()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Member",
      ["IsOpen"] = true,
      ["Side"] = DrawerSide.End
    });

    string root = RootTag(html);
    root.ShouldContain("twe-drawer");
    root.ShouldContain("twe-drawer--end");
    root.ShouldNotContain("twe-drawer--start");
    root.ShouldNotContain("twe-drawer--wide");
    root.ShouldContain("aria-modal=\"true\"");
    string labelledBy = Attribute(root, "aria-labelledby");
    labelledBy.ShouldStartWith("twe-drawer-title-");
    html.ShouldContain($"id=\"{labelledBy}\"");
    html.ShouldContain("twe-drawer__title");
    html.ShouldContain("Member");
    html.ShouldContain("<h2");
    html.ShouldNotContain("<style");
  }

  public static async Task Add_The_Start_And_Wide_Modifiers()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Member",
      ["IsOpen"] = true,
      ["Side"] = DrawerSide.Start,
      ["Size"] = DrawerSize.Wide
    });

    string root = RootTag(html);
    root.ShouldContain("twe-drawer--start");
    root.ShouldContain("twe-drawer--wide");
    root.ShouldNotContain("twe-drawer--end");
  }

  public static async Task Mint_A_Different_Title_Id_Per_Instance()
  {
    string html = await RenderPairAsync();
    List<string> ids = LabelIds(html);
    ids.Count.ShouldBe(2);
    ids[0].ShouldNotBe(ids[1]);
    ids[0].ShouldStartWith("twe-drawer-title-");
    ids[1].ShouldStartWith("twe-drawer-title-");
    html.ShouldContain($"id=\"{ids[0]}\"");
    html.ShouldContain($"id=\"{ids[1]}\"");
    html.ShouldContain("First drawer");
    html.ShouldContain("Second drawer");
  }

  public static async Task Omit_The_Footer_Unless_It_Is_Set()
  {
    string hidden = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Member",
      ["IsOpen"] = true
    });

    hidden.ShouldNotContain("twe-drawer__footer");
    hidden.ShouldNotContain("<footer");

    string shown = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Member",
      ["IsOpen"] = true,
      ["Footer"] = (RenderFragment)(builder =>
      {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "data-qa", "DrawerSave");
        builder.AddContent(2, "Save");
        builder.CloseElement();
      })
    });

    shown.ShouldContain("twe-drawer__footer");
    shown.ShouldContain("<footer");
    shown.ShouldContain("data-qa=\"DrawerSave\"");
    shown.ShouldContain("Save");
  }

  public static async Task Merge_Class_And_Extra_Attributes_Onto_The_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Member",
      ["IsOpen"] = true,
      ["Class"] = "is-sample",
      ["data-qa"] = "drawer-mark"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-drawer");
    root.ShouldContain("twe-drawer--end");
    root.ShouldContain("is-sample");
    root.ShouldContain("data-qa=\"drawer-mark\"");
    root.ShouldContain("aria-modal=\"true\"");
  }

  public static async Task Name_The_Dismiss_Control_Close_And_Hide_Its_Icon()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "Member",
      ["IsOpen"] = true
    });

    html.ShouldContain("aria-label=\"Close\"");
    html.ShouldContain("data-qa=\"DrawerClose\"");
    string icon = IconTag(html);
    icon.ShouldContain("aria-hidden=\"true\"");
    RootTag(html).ShouldNotContain("aria-hidden");
  }

  private static async Task<string> RenderAsync(Dictionary<string, object?> parameters) =>
    WebUtility.HtmlDecode(await RenderRawAsync(parameters));

  private static async Task<string> RenderRawAsync(Dictionary<string, object?> parameters)
  {
    await using ServiceProvider services = Services();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    return await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<Drawer>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }

  private static async Task<string> RenderPairAsync()
  {
    await using ServiceProvider services = Services();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<Pair>()).ToHtmlString());
    return WebUtility.HtmlDecode(html);
  }

  private static ServiceProvider Services()
  {
    ServiceCollection services = new();
    services.AddLogging();
    services.AddFluentUIComponents();
    services.AddSingleton<IJSRuntime, NoopJsRuntime>();
    return services.BuildServiceProvider();
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-drawer", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "drawer rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }

  private static string Attribute(string tag, string name)
  {
    string marker = name + "=\"";
    int start = tag.IndexOf(marker, StringComparison.Ordinal);
    start.ShouldBeGreaterThan(-1, $"root has no {name}");
    start += marker.Length;
    int end = tag.IndexOf('"', start, StringComparison.Ordinal);
    return tag.Substring(start, end - start);
  }

  private static List<string> LabelIds(string html)
  {
    List<string> ids = [];
    const string marker = "aria-labelledby=\"";
    int index = 0;
    while ((index = html.IndexOf(marker, index, StringComparison.Ordinal)) >= 0)
    {
      int start = index + marker.Length;
      int end = html.IndexOf('"', start, StringComparison.Ordinal);
      ids.Add(html.Substring(start, end - start));
      index = end + 1;
    }

    return ids;
  }

  private static string IconTag(string html)
  {
    int svgAt = html.IndexOf("<svg", StringComparison.Ordinal);
    svgAt.ShouldBeGreaterThan(-1, "dismiss control rendered no icon");
    int end = html.IndexOf('>', svgAt, StringComparison.Ordinal);
    return html.Substring(svgAt, end - svgAt + 1);
  }

  private sealed class Pair : ComponentBase
  {
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
      builder.OpenComponent<Drawer>(0);
      builder.AddComponentParameter(1, "Title", "First drawer");
      builder.AddComponentParameter(2, "IsOpen", true);
      builder.CloseComponent();
      builder.OpenComponent<Drawer>(3);
      builder.AddComponentParameter(4, "Title", "Second drawer");
      builder.AddComponentParameter(5, "IsOpen", true);
      builder.CloseComponent();
    }
  }

  private sealed class NoopJsRuntime : IJSRuntime
  {
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
      ValueTask.FromResult(default(TValue)!);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
      ValueTask.FromResult(default(TValue)!);
  }
}
