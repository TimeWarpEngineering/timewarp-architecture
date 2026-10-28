#region Purpose
// Render PageSplit with HtmlRenderer and pin column order, passthrough, and the stack rule.
#endregion

#region Design
// PageSplit is a leaf with no services, so a bare ServiceCollection host is enough.
// The root is a native div. Primary is the first child and secondary is the second.
// There is no third column. The stylesheet, not the markup, sets 1.6fr / 1fr and the 1100px stack.
// The script case asserts the raw markup, before HtmlDecode, so the primary text stays encoded.
#endregion

namespace PageSplitRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class PageSplit_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<PageSplit_Should_>();

  public static async Task Render_Primary_Before_Secondary_And_No_Third_Column()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Primary"] = (RenderFragment)(builder => builder.AddContent(0, "Primary column")),
      ["Secondary"] = (RenderFragment)(builder => builder.AddContent(1, "Secondary column"))
    });

    int primary = html.IndexOf("twe-page-split__primary", StringComparison.Ordinal);
    int secondary = html.IndexOf("twe-page-split__secondary", StringComparison.Ordinal);
    primary.ShouldBeGreaterThan(-1);
    secondary.ShouldBeGreaterThan(primary);
    html.IndexOf("Primary column", StringComparison.Ordinal).ShouldBeLessThan(
      html.IndexOf("Secondary column", StringComparison.Ordinal));
    Count(html, "twe-page-split__").ShouldBe(2);
    html.ShouldNotContain("<style");
  }

  public static async Task Pass_Extra_Attributes_To_The_Root()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Primary"] = (RenderFragment)(builder => builder.AddContent(0, "Primary column")),
      ["data-testid"] = "split-mark",
      ["class"] = "caller-mark"
    });

    string root = RootTag(html);
    root.ShouldContain("twe-page-split");
    root.ShouldContain("caller-mark");
    root.ShouldContain("data-testid=\"split-mark\"");
    html.ShouldContain("twe-page-split__secondary");
  }

  public static async Task Encode_Script_In_The_Primary_Column()
  {
    string raw = await RenderRawAsync(new Dictionary<string, object?>
    {
      ["Primary"] = (RenderFragment)(builder => builder.AddContent(0, "<script>"))
    });

    raw.ShouldNotContain("<script");
    raw.ShouldContain("&lt;script&gt;");
    WebUtility.HtmlDecode(raw).ShouldContain("<script>");
  }

  public static Task Stack_At_1100px_With_A_Wide_And_Narrow_Column()
  {
    string css = File.ReadAllText(SourceFile(
      "source",
      "container-apps",
      "web",
      "projects",
      "web-spa",
      "components",
      "elements",
      "PageSplit.razor.css"));

    css.ShouldContain("grid-template-columns: minmax(0, 1.6fr) minmax(0, 1fr);");
    css.ShouldContain("@media (max-width: 1100px)");
    css.ShouldContain("grid-template-columns: minmax(0, 1fr);");
    css.ShouldNotContain("style=");
    return Task.CompletedTask;
  }

  private static async Task<string> RenderAsync(Dictionary<string, object?> parameters) =>
    WebUtility.HtmlDecode(await RenderRawAsync(parameters));

  private static async Task<string> RenderRawAsync(Dictionary<string, object?> parameters)
  {
    await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    return await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<PageSplit>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }

  private static string RootTag(string html)
  {
    int classAt = html.IndexOf("twe-page-split", StringComparison.Ordinal);
    classAt.ShouldBeGreaterThan(-1, "page split rendered no root");
    int start = html.LastIndexOf('<', classAt, StringComparison.Ordinal);
    int end = html.IndexOf('>', classAt, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }

  private static string SourceFile(params string[] relative)
  {
    DirectoryInfo? dir = new(AppContext.BaseDirectory);
    while (dir is not null)
    {
      string candidate = Path.Combine([dir.FullName, .. relative]);
      if (File.Exists(candidate))
      {
        return candidate;
      }

      dir = dir.Parent;
    }

    throw new InvalidOperationException("Could not locate " + string.Join('/', relative));
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
