#region Purpose
// Shared HtmlRenderer checks for Tabs and SegmentedControl.
#endregion

#region Design
// Both controls render a native root, one button per item, and one cancel script. The parsers
// live here so the two suites assert behavior instead of copying the scan. HtmlRenderer output
// is decoded before the assertions read attribute text. The cancel check reads the rendered
// onkeydown and requires each RovingSelection key plus the unmodified-key guard.
#endregion

namespace TimeWarp.Architecture.Web.Spa.Integration.Tests.Components;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Architecture.Components;

internal static class RovingMarkup
{
  internal static async Task<string> RenderAsync<TComponent>(Dictionary<string, object?> parameters)
    where TComponent : IComponent
  {
    await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(parameters))).ToHtmlString());
    return WebUtility.HtmlDecode(html);
  }

  internal static string RootTag(string html, string marker, string missing)
  {
    int classAt = html.IndexOf(marker, StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, missing);
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }

  internal static string[] Buttons(string html)
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

  internal static string[] Labels(string[] buttons, string attribute)
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

  internal static string TabIndex(string button, string missing)
  {
    const string marker = "tabindex=\"";
    int start = button.IndexOf(marker, StringComparison.Ordinal);
    start.ShouldBeGreaterThan(-1, missing + button);
    start += marker.Length;
    int end = button.IndexOf('"', start, StringComparison.Ordinal);
    return button[start..end];
  }

  internal static void ShouldCancelPlainKeys(string root)
  {
    const string marker = "onkeydown=\"";
    int start = root.IndexOf(marker, StringComparison.Ordinal);
    start.ShouldBeGreaterThan(-1, "root rendered no onkeydown. Html: " + root);
    start += marker.Length;
    int end = root.IndexOf('"', start, StringComparison.Ordinal);
    end.ShouldBeGreaterThan(start);
    string script = root[start..end];
    script.ShouldBe(RovingSelection.CancelScript);
    script.ShouldContain("!event.altKey");
    script.ShouldContain("!event.ctrlKey");
    script.ShouldContain("!event.metaKey");
    script.ShouldContain("!event.shiftKey");
    script.ShouldContain("preventDefault()");
    foreach (string key in RovingSelection.HandledKeys)
    {
      script.ShouldContain("event.key==='" + key + "'");
    }
  }
}
