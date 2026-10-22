#region Purpose
// Render SegmentedControl with HtmlRenderer and pin the radiogroup, check, and attribute passthrough.
#endregion

#region Design
// SegmentedControl is a leaf with no services, so a bare ServiceCollection host is enough.
// The root is a native div. Class, AriaLabel, and unmatched attributes land on that root.
// One radio per item. Only the Value item is checked. A null Value checks nothing and the
// first item has tabindex 0. OptionText replaces the label. The component authors no style element.
// The root onkeydown cancels unmodified Arrow, Home, and End keys and leaves modified chords alone.
#endregion

namespace SegmentedControlRender_;

using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Web.Spa.Integration.Tests.Components;

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

  public static async Task Cancel_Unmodified_Arrow_Home_And_End_Only()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Items"] = new[] { "Day", "Week" },
      ["Value"] = "Day"
    });

    RovingMarkup.ShouldCancelPlainKeys(RootTag(html));
  }

  private static Task<string> RenderAsync(Dictionary<string, object?> parameters) =>
    RovingMarkup.RenderAsync<SegmentedControl>(parameters);

  private static string RootTag(string html) =>
    RovingMarkup.RootTag(html, "twe-segmented", "segmented control rendered no root");

  private static string[] Buttons(string html) => RovingMarkup.Buttons(html);

  private static string[] Checked(string[] buttons) => RovingMarkup.Labels(buttons, "aria-checked");

  private static string TabIndex(string button) =>
    RovingMarkup.TabIndex(button, "radio rendered no tabindex. Html: ");
}
