#region Purpose
// Render SegmentedControl with HtmlRenderer and pin the radiogroup, check, and attribute passthrough.
#endregion

#region Design
// SegmentedControl is a leaf with no services, so a bare ServiceCollection host is enough.
// The root is a native div. Class, AriaLabel, and unmatched attributes land on that root.
// One radio per item. Only the Value item is checked. A null Value checks nothing and the
// first item has tabindex 0. OptionText replaces the label. The component authors no style element.
#endregion

namespace SegmentedControlRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class SegmentedControl_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SegmentedControl_Should_>();

  public static async Task Render_One_Radio_Per_Item_And_Check_Only_The_Value()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Items"] = new[] { "Day", "Week", "Month" },
      ["Value"] = "Week"
    });

    string[] buttons = Buttons(html);
    buttons.Length.ShouldBe(3);
    buttons[0].ShouldContain("Day");
    buttons[1].ShouldContain("Week");
    buttons[2].ShouldContain("Month");
    Checked(buttons).ShouldBe(["Week"]);
    TabIndex(buttons[0]).ShouldBe("-1");
    TabIndex(buttons[1]).ShouldBe("0");
    TabIndex(buttons[2]).ShouldBe("-1");
    RootTag(html).ShouldContain("role=\"radiogroup\"");
    html.ShouldNotContain("<style");
  }

  public static async Task Leave_Every_Radio_Unchecked_When_Value_Is_Null()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Items"] = new[] { "Day", "Week" },
      ["Value"] = null
    });

    string[] buttons = Buttons(html);
    buttons.Length.ShouldBe(2);
    Checked(buttons).ShouldBeEmpty();
    TabIndex(buttons[0]).ShouldBe("0");
    TabIndex(buttons[1]).ShouldBe("-1");
  }

  public static async Task Apply_OptionText()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Items"] = new[] { "a", "b" },
      ["Value"] = "b",
      ["OptionText"] = (Func<string, string>)(static value => value == "a" ? "Alpha" : "Beta")
    });

    string[] buttons = Buttons(html);
    buttons.Length.ShouldBe(2);
    buttons[0].ShouldContain("Alpha");
    buttons[1].ShouldContain("Beta");
    Checked(buttons).ShouldBe(["Beta"]);
  }

  public static async Task Merge_Class_AriaLabel_And_Extra_Attributes_Onto_The_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Items"] = new[] { "Week" },
      ["Value"] = "Week",
      ["Class"] = "is-sample",
      ["AriaLabel"] = "Sample period",
      ["data-qa"] = "SampleSegment"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-segmented");
    root.ShouldContain("is-sample");
    root.ShouldContain("aria-label=\"Sample period\"");
    root.ShouldContain("data-qa=\"SampleSegment\"");
    root.ShouldContain("role=\"radiogroup\"");
    html.ShouldNotContain("<style");
  }

  private static async Task<string> RenderAsync(Dictionary<string, object?> parameters)
  {
    await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<SegmentedControl>(ParameterView.FromDictionary(parameters))).ToHtmlString());
    return WebUtility.HtmlDecode(html);
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-segmented", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "segmented control rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }

  private static string[] Buttons(string html)
  {
    List<string> buttons = [];
    const string close = "</button>";
    int from = 0;
    while (true)
    {
      int start = html.IndexOf("<button", from, StringComparison.OrdinalIgnoreCase);
      if (start < 0)
      {
        break;
      }

      int end = html.IndexOf(close, start, StringComparison.OrdinalIgnoreCase);
      end.ShouldBeGreaterThan(start);
      end += close.Length;
      buttons.Add(html[start..end]);
      from = end;
    }

    return [.. buttons];
  }

  private static string[] Checked(string[] buttons)
  {
    const string marker = "aria-checked=\"true\"";
    List<string> selected = [];
    foreach (string button in buttons)
    {
      if (button.Contains(marker, StringComparison.Ordinal))
      {
        int labelAt = button.IndexOf('>', StringComparison.Ordinal) + 1;
        int labelEnd = button.LastIndexOf('<', StringComparison.Ordinal);
        selected.Add(button[labelAt..labelEnd]);
      }
    }

    return [.. selected];
  }

  private static string TabIndex(string button)
  {
    const string marker = "tabindex=\"";
    int start = button.IndexOf(marker, StringComparison.Ordinal);
    start.ShouldBeGreaterThan(-1, "radio rendered no tabindex. Html: " + button);
    start += marker.Length;
    int end = button.IndexOf('"', start, StringComparison.Ordinal);
    return button[start..end];
  }
}
