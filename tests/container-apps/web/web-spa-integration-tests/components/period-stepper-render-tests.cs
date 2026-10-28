#region Purpose
// Render PeriodStepper with HtmlRenderer and pin the label, the two ends, disabled state, and passthrough.
#endregion

#region Design
// PeriodStepper is a leaf with no services, so a bare ServiceCollection host is enough.
// The root is a native div. The page owns the label. Each end is a type="button".
// Disabled renders the disabled attribute on that end only. The component authors no style element.
// The script case asserts the raw markup, before HtmlDecode, so the label stays encoded.
#endregion

namespace PeriodStepperRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class PeriodStepper_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<PeriodStepper_Should_>();

  public static async Task Render_Previous_Label_And_Next()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "This period"
    });

    int previous = html.IndexOf(">Previous<", StringComparison.Ordinal);
    int label = html.IndexOf("twe-period-stepper__label", StringComparison.Ordinal);
    int next = html.IndexOf(">Next<", StringComparison.Ordinal);
    previous.ShouldBeGreaterThan(-1);
    label.ShouldBeGreaterThan(previous);
    next.ShouldBeGreaterThan(label);
    html.ShouldContain("This period");
    html.ShouldContain("type=\"button\"");
    html.ShouldNotContain("disabled");
    html.ShouldNotContain("<style");
    Count(html, "<button").ShouldBe(2);
  }

  public static async Task Disable_Either_End_On_Its_Own()
  {
    string previous = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "This period",
      ["PreviousDisabled"] = true
    });

    string previousButton = ButtonContaining(previous, ">Previous<");
    string nextButton = ButtonContaining(previous, ">Next<");
    previousButton.ShouldContain("disabled");
    nextButton.ShouldNotContain("disabled");

    string next = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "This period",
      ["NextDisabled"] = true
    });

    ButtonContaining(next, ">Previous<").ShouldNotContain("disabled");
    ButtonContaining(next, ">Next<").ShouldContain("disabled");
  }

  public static async Task Pass_Extra_Attributes_To_The_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Label"] = "This period",
      ["data-testid"] = "step-mark",
      ["class"] = "caller-mark"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-period-stepper");
    root.ShouldContain("caller-mark");
    root.ShouldContain("data-testid=\"step-mark\"");
  }

  public static async Task Encode_A_Script_Label()
  {
    string raw = await RenderRawAsync(new Dictionary<string, object?>
    {
      ["Label"] = "<script>"
    });

    raw.ShouldNotContain("<script");
    raw.ShouldContain("&lt;script&gt;");
    WebUtility.HtmlDecode(raw).ShouldContain("<script>");
  }

  private static string ButtonContaining(string html, string marker)
  {
    int content = html.IndexOf(marker, StringComparison.Ordinal);
    content.ShouldBeGreaterThan(-1, marker);
    int start = html.LastIndexOf("<button", content, StringComparison.Ordinal);
    start.ShouldBeGreaterThan(-1, marker);
    int end = html.IndexOf('>', start, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }

  private static async Task<string> RenderAsync(Dictionary<string, object?> parameters) =>
    WebUtility.HtmlDecode(await RenderRawAsync(parameters));

  private static async Task<string> RenderRawAsync(Dictionary<string, object?> parameters)
  {
    await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    return await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<PeriodStepper>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-period-stepper", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "period stepper rendered no root");
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
