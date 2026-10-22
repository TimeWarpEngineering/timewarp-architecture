#region Purpose
// Render FilterSelect with HtmlRenderer and pin the label, options, and attribute passthrough.
#endregion

#region Design
// FilterSelect is a leaf. FluentUI services are enough to render the select. A blank label is
// omitted. OptionText and Value are the select's own parameters. data-qa is an unmatched
// attribute and lands on the select, not the native root. Class stays on the root.
#endregion

namespace FilterSelectRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class FilterSelect_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FilterSelect_Should_>();

  public static async Task Render_The_Label_And_Every_Option()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "Status",
      ["Items"] = new[] { "Open", "Closed" }
    });

    html.ShouldContain("twe-filter-select");
    html.ShouldContain("twe-filter-select__label");
    html.ShouldContain("Status");
    html.ShouldContain("Open");
    html.ShouldContain("Closed");
    html.ShouldNotContain("<style");
  }

  public static async Task Omit_The_Label_When_It_Is_Empty()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "",
      ["Items"] = new[] { "Open" }
    });

    html.ShouldNotContain("twe-filter-select__label");
    html.ShouldContain("Open");
  }

  public static async Task Apply_OptionText_And_The_Selected_Value()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "Status",
      ["Items"] = new[] { "", "Open" },
      ["Value"] = "",
      ["OptionText"] = (Func<string, string>)(value => value.Length == 0 ? "Open (default)" : value)
    });

    html.ShouldContain("Open (default)");
    html.ShouldContain("Open");
    html.ShouldContain("selected");
    SelectTag(html).ShouldContain("aria-label=\"Status\"");
  }

  public static async Task Put_DataQa_On_The_Select_And_Class_On_The_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "Kind",
      ["Items"] = new[] { "Note" },
      ["Class"] = "is-kind",
      ["data-qa"] = "AdminFeedbackKind"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-filter-select");
    root.ShouldContain("is-kind");
    root.ShouldNotContain("data-qa=\"AdminFeedbackKind\"");
    html.ShouldContain("data-qa=\"AdminFeedbackKind\"");
    (SelectTag(html) + FieldTag(html)).ShouldContain("data-qa=\"AdminFeedbackKind\"");
  }

  private static async Task<string> RenderAsync(Dictionary<string, object?> parameters)
  {
    ServiceCollection services = new();
    services.AddLogging();
    services.AddFluentUIComponents();
    services.AddSingleton<IJSRuntime, NoopJsRuntime>();
    await using ServiceProvider provider = services.BuildServiceProvider();
    await using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<FilterSelect>(ParameterView.FromDictionary(parameters))).ToHtmlString());
    return WebUtility.HtmlDecode(html);
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-filter-select", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "filter select rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }

  private static string SelectTag(string html) => OpeningTag(html, "<fluent-dropdown");

  private static string FieldTag(string html) => OpeningTag(html, "<fluent-field");

  private static string OpeningTag(string html, string tag)
  {
    int at = html.IndexOf(tag, StringComparison.OrdinalIgnoreCase);
    at.ShouldBeGreaterThan(-1, "filter select rendered no " + tag + ". Html: " + html);
    int end = html.IndexOf('>', at, StringComparison.Ordinal);
    return html.Substring(at, end - at + 1);
  }

  public sealed class NoopJsRuntime : IJSRuntime
  {
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
      ValueTask.FromResult(default(TValue)!);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
      ValueTask.FromResult(default(TValue)!);
  }
}
