#region Purpose
// Browser proof that the feedback form accepts a picked file and a pasted image, that the
// Details visible box matches Title, and that every shared TextArea (filer reply, admin reply,
// admin note) fills its field and resizes.
#endregion

#region Design
// Same host as the other WASM proofs: appsettings UseMock on the server, wwwroot UseMock left
// false so the page calls the real in-memory API. /Feedback is [Authorize] for
// feedback.file.self. That gate reads the SPA authentication state, which comes from the
// identity-session cookie. X-TimeWarp-Mock-Principal-Id authenticates server API calls only,
// so a direct visit while signed out renders RedirectToLogin (no app bar). The test creates
// an account with a virtual passkey, the same ceremony as AskSignInReprobe_Given_Wasm, then opens
// /Feedback. A new account's role includes feedback.file.self. Shots go to test output
// (PlaywrightScreenshot). Each test boots its own host (the admin proofs need a fresh one) and
// opens its own context on the suite's shared browser (PlaywrightBrowserSession). Layout
// measures fluent-textarea's shadow part=root and part=control against Title's
// shadow part=root and input. The host element can be full width while the visible box stays
// at the component's 18rem inline size, so a host BoundingBox check does not prove the fix.
// The reply and note boxes have no Title beside them, so they measure part=root against the
// content box of the element that holds the shared TextArea (task 326). The first passkey
// account on a fresh host claims Administrator, so the same session opens /Admin/Feedback/{id}.
#endregion

namespace FeedbackAttachmentPlaywright_;

using static FeedbackAttachmentPlaywright_.FeedbackAttachmentTestFixture;

public class FeedbackAttachment_Given_Wasm
{
  private const string PngBase64 =
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FeedbackAttachment_Given_Wasm>();

  public static async Task PasteAndUpload_Should_ShowAttachmentsOnTheItem()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync();
    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
    await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
      // The host locale can be en-us@posix, which .NET WASM rejects and never renders the shell.
      Locale = "en-US",
    });
    IPage page = await context.NewPageAsync();
    ConcurrentQueue<string> console = new();
    int sawWasm = 0;
    page.Console += (_, message) => console.Enqueue($"{message.Type}: {message.Text}");
    page.PageError += (_, error) => console.Enqueue(error);
    page.Response += (_, response) =>
    {
      if (response.Status == 200 && response.Url.Contains(".wasm", StringComparison.OrdinalIgnoreCase))
      {
        Interlocked.Exchange(ref sawWasm, 1);
      }
    };

    await SignInWithNewPasskeyAsync(page, context);
    await OpenFeedbackAsync(page, console);

    Volatile.Read(ref sawWasm).ShouldBe(1, "InteractiveWebAssembly did not download a .wasm");

    await AssertDetailsMatchesTitleAsync(page, "attachment");

    await page.Locator("[data-qa=FeedbackTitle]").Locator("input").FillAsync("Picker and paste");
    await page.Locator("[data-qa=FeedbackBody]").Locator("textarea").FillAsync("Details before the file.");
    await page.Locator("[data-qa=FeedbackFile]").SetInputFilesAsync(new FilePayload
    {
      Name = "notes.txt",
      MimeType = "text/plain",
      Buffer = "hello from the picker"u8.ToArray(),
    });
    await page.Locator("[data-qa=FeedbackAttachment]").First.WaitForAsync(new LocatorWaitForOptions
    {
      Timeout = 30_000,
    });
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("feedback-upload.png") });

    const string pasteScript = """
      (pngBase64) => {
        const host = document.querySelector('[data-qa=FeedbackBody]');
        const textarea = host && host.shadowRoot && host.shadowRoot.querySelector('textarea');
        if (!textarea) {
          return 'missing-textarea';
        }
        const binary = Uint8Array.from(atob(pngBase64), (character) => character.charCodeAt(0));
        const file = new File([binary], 'pasted-image.png', { type: 'image/png' });
        const data = new DataTransfer();
        data.items.add(file);
        const event = new Event('paste', { bubbles: true, cancelable: true });
        Object.defineProperty(event, 'clipboardData', { value: data });
        textarea.dispatchEvent(event);
        return 'pasted';
      }
      """;
    string pasted = await page.EvaluateAsync<string>(pasteScript, PngBase64);
    pasted.ShouldBe("pasted");
    await page.Locator("[data-qa=FeedbackAttachment]").Nth(1).WaitForAsync(new LocatorWaitForOptions
    {
      Timeout = 30_000,
    });
    string body = await page.Locator("[data-qa=FeedbackBody]").Locator("textarea").InputValueAsync();
    body.ShouldContain("/api/Feedback/attachments/");
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("feedback-paste.png") });

    await page.Locator("[data-qa=FeedbackSubmit]").ClickAsync();
    await page.Locator("[data-qa=FeedbackReceiptId]").WaitForAsync(new LocatorWaitForOptions
    {
      Timeout = 30_000,
    });
    await page.Locator("[data-qa=FeedbackPermalink]").ClickAsync();
    // Task 353: the body is safe markdown. The composer wrote ![name](path) for both uploads:
    // the pasted image renders once, inline; the text file renders as an inline link. Both are
    // referenced inline, so neither is listed again under the body.
    await page.Locator("[data-qa=FeedbackItemBody] img").First.WaitForAsync(new LocatorWaitForOptions
    {
      Timeout = 30_000,
    });
    (await page.Locator("[data-qa=FeedbackItemBody] img").CountAsync()).ShouldBe(1);
    (await page.Locator("img[src*='/api/Feedback/attachments/']").CountAsync()).ShouldBe(1);
    (await page.Locator("[data-qa=FeedbackItemBody] a", new PageLocatorOptions { HasText = "notes.txt" }).CountAsync()).ShouldBe(1);
    (await page.Locator("[data-qa=FeedbackItemBody]").InnerTextAsync()).ShouldNotContain("![");
    (await page.Locator("[data-qa=FeedbackItemAttachment]").CountAsync()).ShouldBe(0);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("feedback-item-attachments.png") });
  }

  public static async Task VisibleDetailsBox_Should_MatchTitle_And_Resize()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync();
    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
    await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
      Locale = "en-US",
    });
    IPage page = await context.NewPageAsync();
    ConcurrentQueue<string> console = new();
    page.Console += (_, message) => console.Enqueue($"{message.Type}: {message.Text}");
    page.PageError += (_, error) => console.Enqueue(error);

    await SignInWithNewPasskeyAsync(page, context);
    await OpenFeedbackAsync(page, console);

    await AssertDetailsMatchesTitleAsync(page, "default-1280");
    await page.ScreenshotAsync(new PageScreenshotOptions
    {
      Path = ScreenshotPath("feedback-details.png"),
      FullPage = true,
    });

    await AssertGripResizesAsync(page, "FeedbackBody");

    await page.SetViewportSizeAsync(600, 900);
    await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
    await AssertDetailsMatchesTitleAsync(page, "narrow-600");
  }

  public static async Task ReplyAndNoteBoxes_Should_FillTheirField_And_Resize()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync();
    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
    await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
      Locale = "en-US",
    });
    IPage page = await context.NewPageAsync();
    ConcurrentQueue<string> console = new();
    page.Console += (_, message) => console.Enqueue($"{message.Type}: {message.Text}");
    page.PageError += (_, error) => console.Enqueue(error);

    await SignInWithNewPasskeyAsync(page, context);
    await OpenFeedbackAsync(page, console);

    await page.Locator("[data-qa=FeedbackTitle]").Locator("input").FillAsync("Reply box sizing");
    await page.Locator("[data-qa=FeedbackBody]").Locator("textarea").FillAsync("Measure the reply and note boxes.");
    await page.Locator("[data-qa=FeedbackBody]").Locator("textarea").PressAsync("Tab");
    ILocator receipt = await SubmitFeedbackAsync(page);
    string receiptText = await receipt.InnerTextAsync();
    string feedbackItemId = receiptText.Replace("Filed as", string.Empty, StringComparison.Ordinal).Trim();
    Guid.TryParse(feedbackItemId, out _).ShouldBeTrue(receiptText);

    await page.Locator("[data-qa=FeedbackPermalink]").ClickAsync();
    await page.Locator("[data-qa=FeedbackReply]").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    await AssertBoxesFillTheirFieldAsync(page, "filer", ["FeedbackReply"]);

    await page.SetViewportSizeAsync(1280, 900);
    string adminUrl = InProcTestPorts.WebHostUrl.TrimEnd('/') + "/Admin/Feedback/" + feedbackItemId;
    await page.GotoAsync(adminUrl, new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    try
    {
      await page.Locator("[data-qa=AdminFeedbackNote]").WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
    }
    catch (TimeoutException exception)
    {
      string log = string.Join('\n', console);
      throw new TimeoutException($"Admin feedback item did not render at {page.Url}. Console:\n{log}", exception);
    }

    await AssertBoxesFillTheirFieldAsync(page, "admin", ["AdminFeedbackReply", "AdminFeedbackNote"]);
  }
}
