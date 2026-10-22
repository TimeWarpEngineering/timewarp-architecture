#region Purpose
// Render EmptyState with HtmlRenderer and pin the root, variant, optional slots, heading, and icon.
#endregion

#region Design
// EmptyState's full form hosts a FluentUI icon, so the renderer needs FluentUI services and a
// no-op JS runtime. The component itself injects nothing. The root is a native div.
// Full adds no modifier. Inline adds twe-empty-state--inline, uses a paragraph title, and
// renders no icon. A blank description is omitted. ChildContent is the actions row.
// Class and unmatched attributes land on the root. HeadingLevel selects h2–h6; a value
// outside that range stays h2. The icon tile is aria-hidden. The root is not.
#endregion

namespace EmptyStateRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using TimeWarp.Architecture.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

[TestTag("Unit")]
public class EmptyState_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<EmptyState_Should_>();

  public static async Task Render_The_Title_On_A_Twe_Empty_State_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "No reports yet"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-empty-state");
    root.ShouldContain("role=\"status\"");
    root.ShouldNotContain("aria-hidden");
    html.ShouldContain("No reports yet");
    html.ShouldContain("twe-empty-state__title");
    html.ShouldNotContain("<style");
  }

  public static async Task Leave_The_Default_Variant_Without_The_Inline_Modifier()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "No reports yet",
      ["Variant"] = EmptyStateVariant.Full
    });

    string root = RootTag(html);
    root.ShouldContain("twe-empty-state");
    root.ShouldNotContain("twe-empty-state--");
    html.ShouldContain("<h2");
  }

  public static async Task Add_The_Inline_Modifier()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "No rows match",
      ["Variant"] = EmptyStateVariant.Inline,
      ["HeadingLevel"] = 4
    });

    string root = RootTag(html);
    root.ShouldContain("twe-empty-state--inline");
    html.ShouldContain("twe-empty-state__title");
    html.ShouldContain("No rows match");
    html.ShouldNotContain("<h4");
    html.ShouldNotContain("<h2");
    html.ShouldContain("<p");
  }

  public static async Task Omit_Description_And_Actions_Unless_They_Are_Set()
  {
    string hidden = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "No reports yet",
      ["Description"] = "   "
    });

    hidden.ShouldNotContain("twe-empty-state__description");
    hidden.ShouldNotContain("twe-empty-state__actions");

    string shown = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "No reports yet",
      ["Description"] = "Reports appear here after the first run.",
      ["ChildContent"] = (RenderFragment)(builder =>
      {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "data-qa", "EmptyAction");
        builder.AddContent(2, "Refresh");
        builder.CloseElement();
      })
    });

    shown.ShouldContain("twe-empty-state__description");
    shown.ShouldContain("Reports appear here after the first run.");
    shown.ShouldContain("twe-empty-state__actions");
    shown.ShouldContain("data-qa=\"EmptyAction\"");
    shown.ShouldContain("Refresh");
  }

  public static async Task Merge_Class_And_Extra_Attributes_Onto_The_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "No reports yet",
      ["Class"] = "is-sample",
      ["data-qa"] = "empty-mark"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-empty-state");
    root.ShouldContain("is-sample");
    root.ShouldContain("data-qa=\"empty-mark\"");
    root.ShouldContain("role=\"status\"");
  }

  public static async Task Follow_HeadingLevel_On_The_Full_Form()
  {
    string heading3 = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "No reports yet",
      ["HeadingLevel"] = 3
    });

    heading3.ShouldContain("<h3");
    heading3.ShouldNotContain("<h2");

    string heading6 = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "No reports yet",
      ["HeadingLevel"] = 6
    });

    heading6.ShouldContain("<h6");
    heading6.ShouldNotContain("<h2");

    string fallback = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "No reports yet",
      ["HeadingLevel"] = 1
    });

    fallback.ShouldContain("<h2");
    fallback.ShouldNotContain("<h1");
  }

  public static async Task Hide_The_Icon_On_The_Full_Form_And_Omit_It_When_Inline()
  {
    string full = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "No reports yet"
    });

    string icon = IconTag(full);
    icon.ShouldContain("twe-empty-state__icon");
    icon.ShouldContain("aria-hidden=\"true\"");
    full.ShouldContain("<svg");
    full.ShouldContain("aria-hidden=\"true\"");
    RootTag(full).ShouldNotContain("aria-hidden");

    string custom = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "No reports yet",
      ["Icon"] = new Icons.Regular.Size24.Info()
    });

    IconTag(custom).ShouldContain("aria-hidden=\"true\"");
    custom.ShouldContain("<svg");
    custom.ShouldNotBe(full);

    string inline = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "No rows match",
      ["Variant"] = EmptyStateVariant.Inline,
      ["Icon"] = new Icons.Regular.Size24.Info()
    });

    inline.ShouldNotContain("twe-empty-state__icon");
    inline.ShouldNotContain("<svg");
    inline.ShouldNotContain("aria-hidden");
  }

  private static async Task<string> RenderAsync(Dictionary<string, object?> parameters) =>
    WebUtility.HtmlDecode(await RenderRawAsync(parameters));

  private static async Task<string> RenderRawAsync(Dictionary<string, object?> parameters)
  {
    await using ServiceProvider services = Services();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    return await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<EmptyState>(ParameterView.FromDictionary(parameters))).ToHtmlString());
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
    int classAt = html.IndexOf("twe-empty-state", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "empty state rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }

  private static string IconTag(string html)
  {
    int classAt = html.IndexOf("twe-empty-state__icon", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "empty state rendered no icon");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }

  private sealed class NoopJsRuntime : IJSRuntime
  {
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
      ValueTask.FromResult(default(TValue)!);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
      ValueTask.FromResult(default(TValue)!);
  }
}
