#region Purpose
// Render Tabs with HtmlRenderer and pin the tablist, selection, and attribute passthrough.
#endregion

#region Design
// Tabs is a leaf with no services, so a bare ServiceCollection host is enough. The root is a
// native div. Class, AriaLabel, and unmatched attributes land on that root. One button per
// item. Only the Value item is selected. A null Value selects nothing and the first item has
// tabindex 0. OptionText replaces the label. The component authors no style element. The root
// onkeydown cancels unmodified Arrow, Home, and End keys and leaves modified chords alone.
#endregion

namespace TabsRender_;

using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Web.Spa.Integration.Tests.Components;

[TestTag("Unit")]
public class Tabs_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Tabs_Should_>();

  public static async Task Render_One_Tab_Per_Item_And_Select_Only_The_Value()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Items"] = new[] { "Overview", "Activity", "Notes" },
      ["Value"] = "Activity"
    });

    string[] buttons = Buttons(html);
    buttons.Length.ShouldBe(3);
    buttons[0].ShouldContain("Overview");
    buttons[1].ShouldContain("Activity");
    buttons[2].ShouldContain("Notes");
    Selected(buttons).ShouldBe(["Activity"]);
    TabIndex(buttons[0]).ShouldBe("-1");
    TabIndex(buttons[1]).ShouldBe("0");
    TabIndex(buttons[2]).ShouldBe("-1");
    RootTag(html).ShouldContain("role=\"tablist\"");
    html.ShouldNotContain("<style");
  }

  public static async Task Leave_Every_Tab_Unselected_When_Value_Is_Null()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Items"] = new[] { "Overview", "Activity" },
      ["Value"] = null
    });

    string[] buttons = Buttons(html);
    buttons.Length.ShouldBe(2);
    Selected(buttons).ShouldBeEmpty();
    TabIndex(buttons[0]).ShouldBe("0");
    TabIndex(buttons[1]).ShouldBe("-1");
  }

  public static async Task Apply_OptionText()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Items"] = new[] { "a", "b" },
      ["Value"] = "a",
      ["OptionText"] = (Func<string, string>)(static value => value == "a" ? "Alpha" : "Beta")
    });

    string[] buttons = Buttons(html);
    buttons.Length.ShouldBe(2);
    buttons[0].ShouldContain("Alpha");
    buttons[1].ShouldContain("Beta");
    Selected(buttons).ShouldBe(["Alpha"]);
  }

  public static async Task Merge_Class_AriaLabel_And_Extra_Attributes_Onto_The_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Items"] = new[] { "Overview" },
      ["Value"] = "Overview",
      ["Class"] = "is-sample",
      ["AriaLabel"] = "Sample views",
      ["data-qa"] = "SampleTabs"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-tabs");
    root.ShouldContain("is-sample");
    root.ShouldContain("aria-label=\"Sample views\"");
    root.ShouldContain("data-qa=\"SampleTabs\"");
    root.ShouldContain("role=\"tablist\"");
    html.ShouldNotContain("<style");
  }

  public static async Task Cancel_Unmodified_Arrow_Home_And_End_Only()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Items"] = new[] { "Overview", "Activity" },
      ["Value"] = "Overview"
    });

    RovingMarkup.ShouldCancelPlainKeys(RootTag(html));
  }

  private static Task<string> RenderAsync(Dictionary<string, object?> parameters) =>
    RovingMarkup.RenderAsync<Tabs>(parameters);

  private static string RootTag(string html) =>
    RovingMarkup.RootTag(html, "twe-tabs", "tabs rendered no root");

  private static string[] Buttons(string html) => RovingMarkup.Buttons(html);

  private static string[] Selected(string[] buttons) => RovingMarkup.Labels(buttons, "aria-selected");

  private static string TabIndex(string button) =>
    RovingMarkup.TabIndex(button, "tab rendered no tabindex. Html: ");
}
