#region Purpose
// Render SafeMarkdown and FeedbackBody and prove the safety rules: no raw HTML, no script or
// on* attributes, links only http(s) or app-relative, images only from the allow-list, and an
// attachment shown inline is not listed again.
#endregion

#region Design
// HtmlRenderer is enough: the components take no services. Static rendering drops Blazor event
// handlers, so "onerror" in the output can only come from the markdown. Blazor always quotes an
// attribute value, so the asserts look for ' onclick="' (a real attribute), while the escaped
// text keeps &quot;.
#endregion

namespace SafeMarkdownRender_;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features.Feedback;

[TestTag("Unit")]
public class SafeMarkdown_Should_
{
  private const string Own = "/api/Feedback/attachments/11111111-1111-1111-1111-111111111111";
  private const string Other = "/api/Feedback/attachments/22222222-2222-2222-2222-222222222222";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SafeMarkdown_Should_>();

  public static async Task Render_Markdown_Structure()
  {
    string html = await RenderAsync("# Title\n\nSome **bold** and *em* and `code`.\n\n- one\n- two");

    html.ShouldContain("<h3>Title</h3>");
    html.ShouldContain("<strong>bold</strong>");
    html.ShouldContain("<em>em</em>");
    html.ShouldContain("<code>code</code>");
    html.ShouldContain("<li><p>one</p></li>");
  }

  public static async Task Escape_Raw_Html_And_Script()
  {
    string html = await RenderAsync(
      "<script>alert(1)</script>\n\nInline <b onclick=\"alert(1)\">x</b> and <img src=x onerror=alert(1)>\n\n" +
      "<div>\n<iframe src=\"https://evil.example\"></iframe>\n</div>");

    html.ShouldNotContain("<script");
    html.ShouldNotContain("<b ");
    html.ShouldNotContain("<img");
    html.ShouldNotContain("<iframe");
    html.ShouldNotContain("<div>");
    html.ShouldNotContain(" onclick=\"");
    html.ShouldNotContain(" onerror=\"");
    html.ShouldContain("onclick=&quot;");
    html.ShouldContain("&lt;script&gt;");
  }

  public static async Task Refuse_Script_And_Data_Links()
  {
    string html = await RenderAsync(
      "[a](javascript:alert(1)) [b](JaVaScRiPt:alert(1)) [c](<java\tscript:alert(1)>) " +
      "[d](&#106;avascript:alert(1)) [e](data:text/html;base64,PHNjcmlwdD4=) [f](//evil.example) " +
      "[g](vbscript:msgbox) [h](/\\evil.example) <javascript:alert(1)> [i](file:///etc/passwd)");

    html.ShouldNotContain("<a ");
    html.ShouldNotContain("href=");
  }

  public static async Task Allow_Http_And_Relative_Links()
  {
    string html = await RenderAsync(
      "[s](https://example.com/x) [p](http://example.com) [r](/Feedback) [f](#top) <https://example.org>");

    html.ShouldContain("href=\"https://example.com/x\"");
    html.ShouldContain("href=\"http://example.com\"");
    html.ShouldContain("href=\"/Feedback\"");
    html.ShouldContain("href=\"#top\"");
    html.ShouldContain("href=\"https://example.org\"");
    html.ShouldContain("rel=\"nofollow noopener noreferrer\"");
  }

  public static async Task Render_Only_Allowed_Images()
  {
    string html = await RenderAsync(
      $"![mine]({Own}) ![theirs]({Other}) ![ext](https://evil.example/x.png) ![js](javascript:alert(1))",
      Own);

    html.ShouldContain($"src=\"{Own}\"");
    html.ShouldNotContain($"src=\"{Other}\"");
    html.ShouldNotContain("src=\"https://evil.example");
    CountOf(html, "<img").ShouldBe(1);
    html.ShouldContain($"href=\"{Other}\"");
    html.ShouldContain("href=\"https://evil.example/x.png\"");
    html.ShouldNotContain("javascript:");
    html.ShouldContain("js");
  }

  public static async Task Escape_Image_Alt_Text()
  {
    string html = await RenderAsync($"![\" onerror=\"alert(1)]({Own})", Own);

    CountOf(html, "<img").ShouldBe(1);
    html.ShouldNotContain(" onerror=\"alert");
  }

  public static async Task Not_List_Inline_Attachments_Twice()
  {
    GetFeedback.Attachment inline = new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "pasted.png", "image/png", 10, Own);
    GetFeedback.Attachment unreferenced = new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "second.png", "image/png", 10, Other);
    GetFeedback.Attachment notes = new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "notes.txt", "text/plain", 10,
      "/api/Feedback/attachments/33333333-3333-3333-3333-333333333333");

    string html = await RenderComponentAsync<FeedbackBody>(new Dictionary<string, object?>
    {
      ["Body"] = $"See this:\n\n![pasted.png]({Own})",
      ["Attachments"] = new List<GetFeedback.Attachment> { inline, unreferenced, notes },
      ["AttachmentDataQa"] = "Listed",
    });

    CountOf(html, $"src=\"{Own}\"").ShouldBe(1);
    CountOf(html, $"src=\"{Other}\"").ShouldBe(1);
    CountOf(html, "data-qa=\"Listed\"").ShouldBe(2);
    html.ShouldContain(">notes.txt</a>");
  }

  public static async Task Not_List_A_File_The_Body_Links_Inline()
  {
    const string notesPath = "/api/Feedback/attachments/33333333-3333-3333-3333-333333333333";
    GetFeedback.Attachment notes = new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "notes.txt", "text/plain", 10, notesPath);

    string html = await RenderComponentAsync<FeedbackBody>(new Dictionary<string, object?>
    {
      ["Body"] = $"Log attached\n![notes.txt]({notesPath})",
      ["Attachments"] = new List<GetFeedback.Attachment> { notes },
      ["AttachmentDataQa"] = "Listed",
    });

    html.ShouldNotContain("<img");
    CountOf(html, $"href=\"{notesPath}\"").ShouldBe(1);
    html.ShouldNotContain("data-qa=\"Listed\"");
  }

  public static async Task Policy_Reports_Only_Given_Sources()
  {
    HashSet<string> sources = new(StringComparer.Ordinal) { Own };

    SafeMarkdownPolicy.ReferencedSources($"![a]({Own}) ![b]({Other})", sources).ShouldBe([Own]);
    SafeMarkdownPolicy.ReferencedSources($"[link only]({Own})", sources).ShouldBe([Own]);
    SafeMarkdownPolicy.ReferencedSources($"`![code]({Own})`", sources).ShouldBeEmpty();
    await Task.CompletedTask;
  }

  private static Task<string> RenderAsync(string markdown, params string[] allowedImages) =>
    RenderComponentAsync<SafeMarkdown>(new Dictionary<string, object?>
    {
      ["Markdown"] = markdown,
      ["AllowedImageSources"] = (IReadOnlySet<string>)new HashSet<string>(allowedImages, StringComparer.Ordinal),
    });

  private static async Task<string> RenderComponentAsync<TComponent>(Dictionary<string, object?> parameters)
    where TComponent : IComponent
  {
    await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    return await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }

  private static int CountOf(string text, string value)
  {
    int count = 0;
    for (int index = text.IndexOf(value, StringComparison.Ordinal); index >= 0;
      index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
    {
      count++;
    }

    return count;
  }
}
