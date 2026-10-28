#region Purpose
// Render IntegrationCard with HtmlRenderer and pin title, meta, badge, actions, icon, and variant.
#endregion

#region Design
// IntegrationCard's icon is a FluentUI icon, so the renderer needs FluentUI services and a
// no-op JS runtime. The component itself injects nothing. The root is a native div.
// Full adds no modifier. Compact adds twe-integration-card--compact.
// Tone Warning adds twe-integration-card--warning only together with compact.
// Full ignores Tone, so the banner surface stays the paper rule.
// A blank title or meta is omitted. The compact separator is aria-hidden and appears only
// when both are set. Status null omits the badge. ChildContent is the actions row.
// Class and unmatched attributes land on the root. The icon tile is aria-hidden. The root is not.
// The script case asserts the raw markup, before HtmlDecode, so the title stays encoded.
#endregion

namespace IntegrationCardRender_;

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
public class IntegrationCard_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<IntegrationCard_Should_>();

  public static async Task Render_Title_And_Meta_And_Omit_A_Blank_Meta()
  {
    string shown = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "External system",
      ["Meta"] = "Last data pull 2 hours ago"
    });

    shown.ShouldContain("twe-integration-card__title");
    shown.ShouldContain("External system");
    shown.ShouldContain("twe-integration-card__meta");
    shown.ShouldContain("Last data pull 2 hours ago");
    shown.ShouldNotContain("<style");

    string blank = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "External system",
      ["Meta"] = "   "
    });

    blank.ShouldContain("External system");
    blank.ShouldNotContain("twe-integration-card__meta");
  }

  public static async Task Omit_The_Badge_Unless_Status_Is_Set()
  {
    string hidden = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "External system",
      ["Status"] = null
    });

    hidden.ShouldNotContain("twe-badge");

    string shown = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "External system",
      ["Status"] = BadgeStatus.Warning,
      ["StatusLabel"] = "Needs attention"
    });

    shown.ShouldContain("twe-badge");
    shown.ShouldContain("Needs attention");
    shown.ShouldContain("twe-badge--warning");
  }

  public static async Task Show_Actions_Only_When_ChildContent_Is_Passed()
  {
    string hidden = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "External system"
    });

    hidden.ShouldNotContain("twe-integration-card__actions");

    string shown = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "External system",
      ["ChildContent"] = (RenderFragment)(builder =>
      {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "data-qa", "SyncAction");
        builder.AddContent(2, "Sync now");
        builder.CloseElement();
      })
    });

    shown.ShouldContain("twe-integration-card__actions");
    shown.ShouldContain("data-qa=\"SyncAction\"");
    shown.ShouldContain("Sync now");
  }

  public static async Task Show_An_Aria_Hidden_Icon_Tile_Only_When_Icon_Is_Set()
  {
    string hidden = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "External system"
    });

    hidden.ShouldNotContain("twe-integration-card__icon");
    hidden.ShouldNotContain("aria-hidden");
    RootTag(hidden).ShouldNotContain("aria-hidden");

    string shown = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "External system",
      ["Icon"] = new Icons.Regular.Size24.Info()
    });

    string icon = IconTag(shown);
    icon.ShouldContain("twe-integration-card__icon");
    icon.ShouldContain("aria-hidden=\"true\"");
    shown.ShouldContain("<svg");
    RootTag(shown).ShouldNotContain("aria-hidden");
  }

  public static async Task Leave_Full_Without_The_Compact_Modifier()
  {
    string implicitFull = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "External system",
      ["Meta"] = "Last data pull 2 hours ago"
    });

    string root = RootTag(implicitFull);
    root.ShouldContain("twe-integration-card");
    root.ShouldNotContain("twe-integration-card--");
    implicitFull.ShouldNotContain("twe-integration-card__separator");

    string explicitFull = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "External system",
      ["Meta"] = "Last data pull 2 hours ago",
      ["Variant"] = IntegrationCardVariant.Full
    });

    RootTag(explicitFull).ShouldNotContain("twe-integration-card--");
    explicitFull.ShouldNotContain("twe-integration-card__separator");
  }

  public static async Task Add_The_Compact_Modifier()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "External system",
      ["Meta"] = "Last data pull 2 hours ago",
      ["Variant"] = IntegrationCardVariant.Compact
    });

    string root = RootTag(html);
    root.ShouldContain("twe-integration-card");
    root.ShouldContain("twe-integration-card--compact");
    html.ShouldContain("External system");
    html.ShouldContain("Last data pull 2 hours ago");
  }

  public static async Task Show_The_Compact_Separator_Only_When_Title_And_Meta_Are_Set()
  {
    string both = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "External system",
      ["Meta"] = "Last data pull 2 hours ago",
      ["Variant"] = IntegrationCardVariant.Compact
    });

    string separator = SeparatorTag(both);
    separator.ShouldContain("twe-integration-card__separator");
    separator.ShouldContain("aria-hidden=\"true\"");

    string titleOnly = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "External system",
      ["Meta"] = "   ",
      ["Variant"] = IntegrationCardVariant.Compact
    });

    titleOnly.ShouldContain("External system");
    titleOnly.ShouldNotContain("twe-integration-card__separator");
    titleOnly.ShouldNotContain("twe-integration-card__meta");

    string metaOnly = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "   ",
      ["Meta"] = "Last data pull 2 hours ago",
      ["Variant"] = IntegrationCardVariant.Compact
    });

    metaOnly.ShouldContain("Last data pull 2 hours ago");
    metaOnly.ShouldContain("twe-integration-card__meta");
    metaOnly.ShouldNotContain("twe-integration-card__separator");
    metaOnly.ShouldNotContain("twe-integration-card__title");
  }

  public static async Task Paint_Warning_Only_On_The_Compact_Row()
  {
    string compact = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "External system",
      ["Meta"] = "The connection cannot be used.",
      ["Variant"] = IntegrationCardVariant.Compact,
      ["Tone"] = IntegrationCardTone.Warning
    });

    string root = RootTag(compact);
    root.ShouldContain("twe-integration-card--compact");
    root.ShouldContain("twe-integration-card--warning");
    compact.ShouldContain("The connection cannot be used.");

    string compactDefault = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "External system",
      ["Variant"] = IntegrationCardVariant.Compact,
      ["Tone"] = IntegrationCardTone.Default
    });

    RootTag(compactDefault).ShouldNotContain("twe-integration-card--warning");

    string full = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "External system",
      ["Meta"] = "The connection cannot be used.",
      ["Variant"] = IntegrationCardVariant.Full,
      ["Tone"] = IntegrationCardTone.Warning
    });

    string fullRoot = RootTag(full);
    fullRoot.ShouldNotContain("twe-integration-card--warning");
    fullRoot.ShouldNotContain("twe-integration-card--compact");
  }

  public static async Task Merge_Class_And_Pass_Data_Qa()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Title"] = "External system",
      ["Class"] = "caller-mark",
      ["data-qa"] = "integration-mark"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-integration-card");
    root.ShouldContain("caller-mark");
    root.ShouldContain("data-qa=\"integration-mark\"");
  }

  public static async Task Encode_A_Script_Title()
  {
    string raw = await RenderRawAsync(new Dictionary<string, object?>
    {
      ["Title"] = "<script>"
    });

    raw.ShouldNotContain("<script");
    raw.ShouldNotContain("<style");
    raw.ShouldContain("&lt;script&gt;");
    WebUtility.HtmlDecode(raw).ShouldContain("<script>");
  }

  private static async Task<string> RenderAsync(Dictionary<string, object?> parameters) =>
    WebUtility.HtmlDecode(await RenderRawAsync(parameters));

  private static async Task<string> RenderRawAsync(Dictionary<string, object?> parameters)
  {
    await using ServiceProvider services = Services();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    return await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<IntegrationCard>(ParameterView.FromDictionary(parameters))).ToHtmlString());
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
    int classAt = html.IndexOf("twe-integration-card", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "integration card rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }

  private static string IconTag(string html)
  {
    int classAt = html.IndexOf("twe-integration-card__icon", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "integration card rendered no icon");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }

  private static string SeparatorTag(string html)
  {
    int classAt = html.IndexOf("twe-integration-card__separator", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "integration card rendered no separator");
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
