#region Purpose
// Render Tabs with HtmlRenderer and pin the tablist, selection, and attribute passthrough.
#endregion

#region Design
// Tabs is a leaf with no services, so a bare ServiceCollection host is enough. The root is a
// native div. Class, AriaLabel, and unmatched attributes land on that root. One button per
// item. Only the Value item is selected. A null Value selects nothing and the first item has
// tabindex 0. OptionText replaces the label. The component authors no style element.
#endregion

namespace TabsRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Architecture.Components;

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
    Selected(buttons, "aria-selected").ShouldBe(["Activity"]);
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
    Selected(buttons, "aria-selected").ShouldBeEmpty();
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
    Selected(buttons, "aria-selected").ShouldBe(["Alpha"]);
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

  private static async Task<string> RenderAsync(Dictionary<string, object?> parameters)
  {
    await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<Tabs>(ParameterView.FromDictionary(parameters))).ToHtmlString());
    return WebUtility.HtmlDecode(html);
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-tabs", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "tabs rendered no root");
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

  private static string[] Selected(string[] buttons, string attribute)
  {
    string marker = attribute + "=\"true\"";
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
    start.ShouldBeGreaterThan(-1, "tab rendered no tabindex. Html: " + button);
    start += marker.Length;
    int end = button.IndexOf('"', start, StringComparison.Ordinal);
    return button[start..end];
  }
}
