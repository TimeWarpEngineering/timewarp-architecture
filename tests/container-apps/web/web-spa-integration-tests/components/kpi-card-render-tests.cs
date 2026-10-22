#region Purpose
// Render KpiCard with HtmlRenderer and pin label, metrics, caption, tone, and attribute passthrough.
#endregion

#region Design
// KpiCard is a Tier-1 leaf with no DI, so a bare ServiceCollection host is enough.
// The root is a native div; unmatched attributes splat onto it the same way SectionHeader passes data-qa.
// A blank unit, second metric, or caption is omitted. Tone None adds no modifier class.
// The script case asserts the raw markup, before HtmlDecode, so the label stays encoded.
#endregion

namespace KpiCardRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class KpiCard_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<KpiCard_Should_>();

  public static async Task Render_Label_And_Value()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "Open items",
      ["Value"] = "12"
    });

    html.ShouldContain("twe-kpi__label");
    html.ShouldContain("Open items");
    html.ShouldContain("twe-kpi__value");
    html.ShouldContain("12");
    html.ShouldNotContain("twe-kpi__unit");
    html.ShouldNotContain("twe-kpi__caption");
    html.ShouldNotContain("<style");
    Count(html, "class=\"twe-kpi__metric\"").ShouldBe(1);
  }

  public static async Task Omit_Unit_When_Null_And_Show_It_When_Set()
  {
    string hidden = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "Open items",
      ["Value"] = "12",
      ["Unit"] = null
    });

    hidden.ShouldNotContain("twe-kpi__unit");

    string blank = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "Open items",
      ["Value"] = "12",
      ["Unit"] = "   "
    });

    blank.ShouldNotContain("twe-kpi__unit");

    string shown = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "Coverage",
      ["Value"] = "4",
      ["Unit"] = "sites"
    });

    shown.ShouldContain("twe-kpi__unit");
    shown.ShouldContain("sites");
  }

  public static async Task Omit_The_Second_Metric_Unless_SecondaryValue_Is_Set()
  {
    string single = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "Coverage",
      ["Value"] = "4",
      ["SecondaryUnit"] = "regions"
    });

    Count(single, "class=\"twe-kpi__metric\"").ShouldBe(1);
    single.ShouldNotContain("regions");

    string dual = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "Coverage",
      ["Value"] = "4",
      ["Unit"] = "sites",
      ["SecondaryValue"] = "2",
      ["SecondaryUnit"] = "regions"
    });

    Count(dual, "class=\"twe-kpi__metric\"").ShouldBe(2);
    dual.ShouldContain("sites");
    dual.ShouldContain("regions");
  }

  public static async Task Omit_Caption_Unless_It_Is_Set()
  {
    string hidden = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "On time",
      ["Value"] = "49.5%",
      ["Caption"] = "   "
    });

    hidden.ShouldNotContain("twe-kpi__caption");

    string shown = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "On time",
      ["Value"] = "49.5%",
      ["Caption"] = "Last 12 months"
    });

    shown.ShouldContain("twe-kpi__caption");
    shown.ShouldContain("Last 12 months");
  }

  public static async Task Apply_Tone_Modifiers_And_Leave_None_Unmarked()
  {
    string none = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "Open items",
      ["Value"] = "12",
      ["Tone"] = KpiTone.None
    });

    RootTag(none).ShouldContain("twe-kpi");
    none.ShouldNotContain("twe-kpi--");

    string positive = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "Open items",
      ["Value"] = "12",
      ["Tone"] = KpiTone.Positive
    });

    RootTag(positive).ShouldContain("twe-kpi--positive");
    positive.ShouldNotContain("twe-kpi--danger");

    string danger = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "Exceptions",
      ["Value"] = "3",
      ["Tone"] = KpiTone.Danger
    });

    RootTag(danger).ShouldContain("twe-kpi--danger");
    danger.ShouldNotContain("twe-kpi--positive");
  }

  public static async Task Pass_Extra_Attributes_To_The_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "Open items",
      ["Value"] = "12",
      ["data-testid"] = "kpi-mark",
      ["class"] = "caller-mark"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-kpi");
    root.ShouldContain("data-testid=\"kpi-mark\"");
    root.ShouldContain("caller-mark");
  }

  public static async Task Encode_A_Script_Label()
  {
    string raw = await RenderRawAsync(new Dictionary<string, object?>
    {
      ["Label"] = "<script>",
      ["Value"] = "1"
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
      (await renderer.RenderComponentAsync<KpiCard>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-kpi", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "kpi card rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }

  private static int Count(string html, string needle)
  {
    int count = 0;
    int index = 0;
    while (index < html.Length)
    {
      int found = html.IndexOf(needle, index, StringComparison.Ordinal);
      if (found < 0)
      {
        break;
      }

      count++;
      index = found + needle.Length;
    }

    return count;
  }
}
