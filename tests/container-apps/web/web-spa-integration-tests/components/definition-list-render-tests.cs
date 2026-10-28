#region Purpose
// Render DefinitionList with HtmlRenderer and pin terms, fragment values, blank-term omission, and passthrough.
#endregion

#region Design
// DefinitionList and DefinitionItem are leaves with no services, so a bare ServiceCollection host is enough.
// The list root is a native dl. Each shown item is a div the item component authors, holding a dt and a dd.
// A blank term omits that item, including its value. The value is a fragment, so a link stays a link.
// Class and unmatched attributes land on the component that declares them.
// The script case asserts the raw markup, before HtmlDecode, so the term stays encoded.
#endregion

namespace DefinitionListRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class DefinitionList_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<DefinitionList_Should_>();

  public static async Task Render_A_Term_And_Its_Value()
  {
    string html = await RenderAsync(
    [
      Item("Kind", "Human"),
      Item("Trust", "Standard")
    ]);

    html.ShouldContain("twe-definition-list");
    html.ShouldContain("<dt");
    html.ShouldContain("<dd");
    html.ShouldContain("Kind");
    html.ShouldContain("Human");
    html.ShouldContain("Trust");
    html.ShouldContain("Standard");
    html.ShouldNotContain("<style");
    Count(html, "twe-definition-list__item").ShouldBe(2);
  }

  public static async Task Render_A_Fragment_Value()
  {
    string html = await RenderAsync(
    [
      Item("Record", value =>
      {
        value.OpenElement(0, "a");
        value.AddAttribute(1, "href", "/items/1");
        value.AddContent(2, "Open");
        value.CloseElement();
      })
    ]);

    html.ShouldContain("twe-definition-list__value");
    html.ShouldContain("href=\"/items/1\"");
    html.ShouldContain("Open");
    html.ShouldContain("Record");
  }

  public static async Task Omit_An_Item_Whose_Term_Is_Blank()
  {
    string html = await RenderAsync(
    [
      Item("Kind", "Human"),
      Item("   ", "Hidden"),
      Item(null, "Also hidden"),
      Item("Trust", "Standard")
    ]);

    html.ShouldContain("Kind");
    html.ShouldContain("Human");
    html.ShouldContain("Trust");
    html.ShouldNotContain("Hidden");
    html.ShouldNotContain("Also hidden");
    Count(html, "twe-definition-list__item").ShouldBe(2);
  }

  public static async Task Pass_Extra_Attributes_To_The_Roots()
  {
    string html = await RenderAsync(
      [Item("Kind", "Human", "item-mark", "item-qa")],
      "list-mark",
      "list-qa");

    string list = RootTag(html, "class=\"twe-definition-list");
    list.ShouldContain("twe-definition-list");
    list.ShouldContain("list-mark");
    list.ShouldContain("data-qa=\"list-qa\"");

    string item = RootTag(html, "twe-definition-list__item");
    item.ShouldContain("item-mark");
    item.ShouldContain("data-qa=\"item-qa\"");
  }

  public static async Task Encode_A_Script_Term()
  {
    string raw = await RenderRawAsync([Item("<script>", "1")]);

    raw.ShouldNotContain("<script");
    raw.ShouldContain("&lt;script&gt;");
    WebUtility.HtmlDecode(raw).ShouldContain("<script>");
  }

  private static RenderFragment Item(string? term, string text, string? cssClass = null, string? qa = null) =>
    Item(term, value => value.AddContent(0, text), cssClass, qa);

  private static RenderFragment Item(string? term, RenderFragment value, string? cssClass = null, string? qa = null)
  {
    return builder =>
    {
      builder.OpenComponent<DefinitionItem>(0);
      builder.AddAttribute(1, "Term", term);
      builder.AddAttribute(2, "ChildContent", value);
      if (cssClass is not null)
      {
        builder.AddAttribute(3, "Class", cssClass);
      }

      if (qa is not null)
      {
        builder.AddAttribute(4, "data-qa", qa);
      }

      builder.CloseComponent();
    };
  }

  private static async Task<string> RenderAsync
  (
    RenderFragment[] items,
    string? cssClass = null,
    string? qa = null
  ) => WebUtility.HtmlDecode(await RenderRawAsync(items, cssClass, qa));

  private static async Task<string> RenderRawAsync
  (
    RenderFragment[] items,
    string? cssClass = null,
    string? qa = null
  )
  {
    Dictionary<string, object?> parameters = new()
    {
      ["ChildContent"] = (RenderFragment)(builder =>
      {
        foreach (RenderFragment item in items)
        {
          builder.AddContent(0, item);
        }
      })
    };
    if (cssClass is not null)
    {
      parameters["Class"] = cssClass;
    }

    if (qa is not null)
    {
      parameters["data-qa"] = qa;
    }

    await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    return await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<DefinitionList>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }

  private static string RootTag(string html, string marker)
  {
    int classAt = html.IndexOf(marker, StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, marker);
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
