#region Purpose
// Browser proof that a filed attachment whose bytes are gone shows its name and "file no longer stored" on the filer's and the admin's item pages.
#endregion

#region Design
// Task 522: two production items kept attachment rows after their bytes were lost (bytes were
// held in memory before task 352). This test makes the same state on purpose. It files an item
// over its own Postgres database (PostgresWebHost) with a pasted image the body shows inline and a
// text file the body does not mention, so the file is listed under the body. It then deletes both
// files' bytes through the host's IFeedbackAttachmentBlobStore, leaving the rows, as the restart
// did. The first passkey account claims Administrator, so the same browser opens both the filer
// permalink and /Admin/Feedback/{id}. On each page the inline image and the listed file render
// MissingFile, no img or link requests a download, and the page logs no render exception. The
// screenshots are the task's evidence of the state.
#endregion

namespace FeedbackAttachmentPlaywright_;

using TimeWarp.Architecture.Features.Feedback.Application;
using TimeWarp.Architecture.Features.Feedback.Domain;
using static FeedbackAttachmentPlaywright_.FeedbackAttachmentTestFixture;

public class FeedbackMissingFile_Given_Wasm
{
  private const string PngBase64 =
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FeedbackMissingFile_Given_Wasm>();

  private static string ScreenshotPath(string fileName) =>
    PlaywrightScreenshot.PathFor<FeedbackMissingFile_Given_Wasm>(fileName);

  public static async Task ItemPages_Should_ShowMissingFiles_InsteadOfBrokenImages()
  {
    await using HostGraph graph = await PostgresWebHost.CreateWebAsync();
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
    await page.Locator("[data-qa=FeedbackTitle]").Locator("input").FillAsync("Dark theme broken");
    await page.Locator("[data-qa=FeedbackBody]").Locator("textarea").FillAsync("The header is unreadable in dark mode.");
    await page.Locator("[data-qa=FeedbackBody]").Locator("textarea").PressAsync("Tab");
    await PasteImageAsync(page);
    await page.Locator("[data-qa=FeedbackFile]").SetInputFilesAsync(new FilePayload
    {
      Name = "console-log.txt",
      MimeType = "text/plain",
      Buffer = "Unhandled exception in ThemeToggle"u8.ToArray(),
    });
    await Assertions.Expect(page.Locator("[data-qa=FeedbackAttachment]"))
      .ToHaveCountAsync(2, new LocatorAssertionsToHaveCountOptions { Timeout = 30_000 });

    // Keep the pasted image inline and drop the text file's line, so the body shows one file and
    // the page lists the other under it.
    ILocator details = page.Locator("[data-qa=FeedbackBody]").Locator("textarea");
    string body = await details.InputValueAsync();
    string kept = string.Join('\n', body.Split('\n').Where(static line => !line.Contains("console-log.txt", StringComparison.Ordinal)));
    kept.ShouldContain("/api/Feedback/attachments/");
    kept.ShouldContain("The header is unreadable in dark mode.");
    await details.FillAsync(kept);
    await details.PressAsync("Tab");
    await SubmitFeedbackAsync<FeedbackMissingFile_Given_Wasm>(page);
    await page.Locator("[data-qa=FeedbackPermalink]").ClickAsync();
    await page.WaitForURLAsync("**/Feedback/*", new PageWaitForURLOptions { Timeout = 30_000 });
    Guid itemId = Guid.Parse(new Uri(page.Url).Segments[^1]);

    (await DeleteFileBytesAsync(graph, itemId)).ShouldBe(2);

    await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
    await AssertMissingFilesAsync(page, "FeedbackItemBody", "FeedbackItemAttachment");
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("filer-item-missing-files.png"), FullPage = true });

    await page.GotoAsync($"{InProcTestPorts.WebHostUrl.TrimEnd('/')}/Admin/Feedback/{itemId:D}", new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    await AssertMissingFilesAsync(page, "AdminFeedbackBody", "AdminFeedbackAttachment");
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("admin-item-missing-files.png"), FullPage = true });

    string log = string.Join('\n', console);
    log.ShouldNotContain("Unhandled exception rendering component");
  }

  private static async Task PasteImageAsync(IPage page)
  {
    const string pasteScript = """
      (pngBase64) => {
        const host = document.querySelector('[data-qa=FeedbackBody]');
        const textarea = host && host.shadowRoot && host.shadowRoot.querySelector('textarea');
        if (!textarea) {
          return 'missing-textarea';
        }
        const binary = Uint8Array.from(atob(pngBase64), (character) => character.charCodeAt(0));
        const file = new File([binary], 'dark-header.png', { type: 'image/png' });
        const data = new DataTransfer();
        data.items.add(file);
        const event = new Event('paste', { bubbles: true, cancelable: true });
        Object.defineProperty(event, 'clipboardData', { value: data });
        textarea.dispatchEvent(event);
        return 'pasted';
      }
      """;
    (await page.EvaluateAsync<string>(pasteScript, PngBase64)).ShouldBe("pasted");
    await page.Locator("[data-qa=FeedbackAttachment]").First.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
  }

  /// <summary>Deletes every linked file's bytes and keeps the rows, as a restart did before task 352.</summary>
  private static async Task<int> DeleteFileBytesAsync(HostGraph graph, Guid itemId)
  {
    await using AsyncServiceScope scope = graph.Web!.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IFeedbackAttachmentStore rows = scope.ServiceProvider.GetRequiredService<IFeedbackAttachmentStore>();
    IFeedbackAttachmentBlobStore bytes = scope.ServiceProvider.GetRequiredService<IFeedbackAttachmentBlobStore>();
    IReadOnlyList<FeedbackAttachment> linked = await rows.ListByItemAsync(FeedbackItemId.From(itemId));
    foreach (FeedbackAttachment attachment in linked)
    {
      await bytes.DeleteAsync(attachment.StorageKey);
      (await bytes.ExistsAsync(attachment.StorageKey)).ShouldBeFalse();
    }

    return linked.Count;
  }

  private static async Task AssertMissingFilesAsync(IPage page, string bodyQa, string attachmentQa)
  {
    ILocator missing = page.Locator("[data-qa=MissingFile]");
    await Assertions.Expect(missing).ToHaveCountAsync(2, new LocatorAssertionsToHaveCountOptions { Timeout = 120_000 });
    await Assertions.Expect(page.Locator($"[data-qa={bodyQa}]")).ToContainTextAsync("The header is unreadable in dark mode.");
    ILocator inline = page.Locator($"[data-qa={bodyQa}] [data-qa=MissingFile]");
    await Assertions.Expect(inline).ToHaveCountAsync(1);
    await Assertions.Expect(inline).ToContainTextAsync("dark-header.png");
    await Assertions.Expect(inline).ToContainTextAsync("file no longer stored");
    ILocator listed = page.Locator($"[data-qa={attachmentQa}] [data-qa=MissingFile]");
    await Assertions.Expect(listed).ToHaveCountAsync(1);
    await Assertions.Expect(listed).ToContainTextAsync("console-log.txt");
    (await page.Locator("img[src*='/api/Feedback/attachments/']").CountAsync()).ShouldBe(0);
    (await page.Locator("a[href*='/api/Feedback/attachments/']").CountAsync()).ShouldBe(0);
  }
}
