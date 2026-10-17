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
// The Ask parameters (task 380): HtmlRenderer drops handlers, so the app-link click is proven
// with LinkClickRenderer, a bare Renderer that keeps the frames of the first render and
// dispatches a real click to the onclick handler. Partial markdown is the streaming case: each
// prefix of a reply must render without throwing.
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

  public static async Task Keep_Soft_Breaks_As_Br_By_Default()
  {
    string html = await RenderAsync("one\ntwo");

    html.ShouldContain("one<br />two");
  }

  public static async Task Render_Soft_Breaks_As_Spaces_When_Asked()
  {
    string html = await RenderComponentAsync<SafeMarkdown>(new Dictionary<string, object?>
    {
      ["Markdown"] = "one\ntwo  \nthree",
      ["SoftBreaksAsSpaces"] = true,
    });

    html.ShouldContain("one two<br />three");
  }

  public static async Task Open_External_Links_In_A_New_Tab_Only_When_Asked()
  {
    const string markdown = "[e](https://example.com) [a](/Login) [f](#top)";
    string plain = await RenderAsync(markdown);
    string ask = await RenderComponentAsync<SafeMarkdown>(new Dictionary<string, object?>
    {
      ["Markdown"] = markdown,
      ["ExternalLinksInNewTab"] = true,
    });

    plain.ShouldNotContain("target=");
    CountOf(ask, "target=\"_blank\"").ShouldBe(1);
    ask.ShouldContain("<a href=\"https://example.com\" rel=\"nofollow noopener noreferrer\" target=\"_blank\">");
    ask.ShouldContain("<a href=\"/Login\" rel=\"nofollow noopener noreferrer\">");
  }

  public static async Task Send_App_Link_Clicks_To_The_Callback()
  {
    List<string> clicked = [];
    await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    await using LinkClickRenderer renderer = new(services);
    object receiver = new();
    await renderer.RenderAsync(new Dictionary<string, object?>
    {
      ["Markdown"] = "[in](/Login) [out](https://example.com) [frag](#top)",
      ["OnAppLinkClick"] = EventCallback.Factory.Create<string>(receiver, clicked.Add),
    });

    renderer.ClickHandlers.Count.ShouldBe(1);
    await renderer.ClickAsync(renderer.ClickHandlers[0]);

    clicked.ShouldBe(["/Login"]);
  }

  public static async Task Render_No_Click_Handler_Without_A_Callback()
  {
    await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    await using LinkClickRenderer renderer = new(services);
    await renderer.RenderAsync(new Dictionary<string, object?> { ["Markdown"] = "[in](/Login)" });

    renderer.ClickHandlers.ShouldBeEmpty();
  }

  public static async Task Render_Every_Prefix_Of_A_Streaming_Reply()
  {
    const string reply = "Some **bold** and [Settings](/Settings).\n\n- one\n- two\n\n```cs\nvar x = 1;\n```\n<b>raw</b>";
    for (int length = 0; length <= reply.Length; length++)
    {
      string html = await RenderComponentAsync<SafeMarkdown>(new Dictionary<string, object?>
      {
        ["Markdown"] = reply[..length],
        ["SoftBreaksAsSpaces"] = true,
      });

      html.ShouldStartWith("<div class=\"safe-markdown\"", Case.Sensitive, $"prefix {length}");
      html.ShouldNotContain("<b>", Case.Sensitive, $"prefix {length}");
    }

    string open = await RenderAsync("Some **bol");
    open.ShouldContain("Some **bol");
    string empty = await RenderAsync("```");
    empty.ShouldContain("<pre><code></code></pre>");
    string fence = await RenderAsync("```\nhalf a fence");
    fence.ShouldContain("<pre><code>half a fence</code></pre>");
  }

  public static async Task Policy_Reports_Only_Given_Sources()
  {
    HashSet<string> sources = new(StringComparer.Ordinal) { Own };

    SafeMarkdownPolicy.ReferencedSources($"![a]({Own}) ![b]({Other})", sources).ShouldBe([Own]);
    SafeMarkdownPolicy.ReferencedSources($"[link only]({Own})", sources).ShouldBe([Own]);
    SafeMarkdownPolicy.ReferencedSources($"`![code]({Own})`", sources).ShouldBeEmpty();
    SafeMarkdownPolicy.ReferencedSources($"![see [log]({Own})](/x)", sources).ShouldBeEmpty();
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

#pragma warning disable BL0006 // RenderTree types: the test reads the rendered frames to find the click handler.
  private sealed class LinkClickRenderer(IServiceProvider services)
    : Microsoft.AspNetCore.Components.RenderTree.Renderer(services, NullLoggerFactory.Instance)
  {
    public List<ulong> ClickHandlers { get; } = [];

    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

    public Task RenderAsync(Dictionary<string, object?> parameters) => Dispatcher.InvokeAsync(async () =>
    {
      int id = AssignRootComponentId(new SafeMarkdown());
      await RenderRootComponentAsync(id, ParameterView.FromDictionary(parameters));
    });

    public Task ClickAsync(ulong handler) =>
      Dispatcher.InvokeAsync(() => DispatchEventAsync(handler, null, new MouseEventArgs()));

    protected override void HandleException(Exception exception) =>
      System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();

    protected override Task UpdateDisplayAsync(in Microsoft.AspNetCore.Components.RenderTree.RenderBatch renderBatch)
    {
      Microsoft.AspNetCore.Components.RenderTree.ArrayRange<Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrame> frames =
        renderBatch.ReferenceFrames;
      for (int index = 0; index < frames.Count; index++)
      {
        Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrame frame = frames.Array[index];
        if (frame.FrameType == Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrameType.Attribute &&
          frame.AttributeName == "onclick" && frame.AttributeEventHandlerId != 0)
        {
          ClickHandlers.Add(frame.AttributeEventHandlerId);
        }
      }

      return Task.CompletedTask;
    }
  }
#pragma warning restore BL0006

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
