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
// an account with a virtual passkey, the same ceremony as AskSignIn_Given_Wasm, then opens
// /Feedback. A new account's role includes feedback.file.self. Shots go to test output
// (PlaywrightScreenshot). Chromium install
// retries with the ubuntu24.04 build when the host distro is newer than Playwright's platform
// list. Layout measures fluent-textarea's shadow part=root and part=control against Title's
// shadow part=root and input. The host element can be full width while the visible box stays
// at the component's 18rem inline size, so a host BoundingBox check does not prove the fix.
// The reply and note boxes have no Title beside them, so they measure part=root against the
// content box of the element that holds the shared TextArea (task 326). The first passkey
// account on a fresh host claims Administrator, so the same session opens /Admin/Feedback/{id}.
// The admin list sort proof (task 327) files two items, opens /Admin/Feedback, clicks the
// Title header's sort button, and checks the rows re-order (server-side sort) and aria-sort
// toggles ascending then descending.
#endregion

namespace FeedbackAttachmentPlaywright_;

public class FeedbackAttachment_Given_Wasm
{
  private const string PngBase64 =
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FeedbackAttachment_Given_Wasm>();

  public static async Task PasteAndUpload_Should_ShowAttachmentsOnTheItem()
  {
    InstallChromium();

    await using HostGraph graph = await HostGraphFactory.CreateWebAsync();
    using IPlaywright playwright = await Playwright.CreateAsync();
    await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
      Headless = true,
    });
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
    await page.Locator("[data-qa=FeedbackItemAttachment]").First.WaitForAsync(new LocatorWaitForOptions
    {
      Timeout = 30_000,
    });
    (await page.Locator("[data-qa=FeedbackItemAttachment] img").CountAsync()).ShouldBeGreaterThan(0);
    (await page.Locator("[data-qa=FeedbackItemAttachment] a").CountAsync()).ShouldBeGreaterThan(0);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("feedback-item-attachments.png") });
  }

  public static async Task VisibleDetailsBox_Should_MatchTitle_And_Resize()
  {
    InstallChromium();

    await using HostGraph graph = await HostGraphFactory.CreateWebAsync();
    using IPlaywright playwright = await Playwright.CreateAsync();
    await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
      Headless = true,
    });
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
    InstallChromium();

    await using HostGraph graph = await HostGraphFactory.CreateWebAsync();
    using IPlaywright playwright = await Playwright.CreateAsync();
    await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
      Headless = true,
    });
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

  public static async Task AdminListHeaderClick_Should_ReorderRows_And_ToggleDirection()
  {
    InstallChromium();

    await using HostGraph graph = await HostGraphFactory.CreateWebAsync();
    using IPlaywright playwright = await Playwright.CreateAsync();
    await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
      Headless = true,
    });
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
    // Alpha is filed first, so the default newest-first order is Zulu, Alpha.
    foreach (string title in new[] { "Alpha sort", "Zulu sort" })
    {
      await OpenFeedbackAsync(page, console);
      await page.Locator("[data-qa=FeedbackTitle]").Locator("input").FillAsync(title);
      await page.Locator("[data-qa=FeedbackBody]").Locator("textarea").FillAsync("Sort me.");
      await page.Locator("[data-qa=FeedbackBody]").Locator("textarea").PressAsync("Tab");
      await SubmitFeedbackAsync(page);
    }

    await page.GotoAsync(InProcTestPorts.WebHostUrl.TrimEnd('/') + "/Admin/Feedback", new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    ILocator rows = page.Locator("[data-qa=AdminFeedbackRow]");
    try
    {
      await Assertions.Expect(rows).ToHaveTextAsync(["Zulu sort", "Alpha sort"], new LocatorAssertionsToHaveTextOptions { Timeout = 120_000 });
    }
    catch (PlaywrightException exception)
    {
      await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("admin-feedback-sort-failed.png"), FullPage = true });
      string text = await page.Locator("body").InnerTextAsync();
      string log = string.Join('\n', console);
      throw new TimeoutException($"Admin feedback list did not render newest first at {page.Url}. Page:\n{text}\nConsole:\n{log}", exception);
    }

    ILocator grid = page.Locator("[data-qa=AdminFeedbackTable]");
    ILocator titleHeader = grid.GetByRole(AriaRole.Columnheader).Filter(new LocatorFilterOptions { HasText = "Title" });
    ILocator filedHeader = grid.GetByRole(AriaRole.Columnheader).Filter(new LocatorFilterOptions { HasText = "Filed" });
    await Assertions.Expect(filedHeader).ToHaveAttributeAsync("aria-sort", "descending");

    await titleHeader.Locator("[col-sort-button]").ClickAsync();
    await Assertions.Expect(rows).ToHaveTextAsync(["Alpha sort", "Zulu sort"], new LocatorAssertionsToHaveTextOptions { Timeout = 30_000 });
    await Assertions.Expect(titleHeader).ToHaveAttributeAsync("aria-sort", "ascending");
    await Assertions.Expect(filedHeader).Not.ToHaveAttributeAsync("aria-sort", "descending");
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("admin-feedback-sort-title-ascending.png"), FullPage = true });

    await titleHeader.Locator("[col-sort-button]").ClickAsync();
    await Assertions.Expect(titleHeader).ToHaveAttributeAsync("aria-sort", "descending", new LocatorAssertionsToHaveAttributeOptions { Timeout = 30_000 });
    await Assertions.Expect(rows).ToHaveTextAsync(["Zulu sort", "Alpha sort"], new LocatorAssertionsToHaveTextOptions { Timeout = 30_000 });
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("admin-feedback-sort-title-descending.png"), FullPage = true });
  }

  // The submit button is Disabled while any action is active, and blurring Details dispatches the
  // composer-context action, so a click straight after typing can land on a disabled button and
  // do nothing. Wait until the button has stayed enabled for 500ms, then click exactly once: a
  // re-click could file a duplicate item and would hide a submit that really did nothing.
  private static async Task<ILocator> SubmitFeedbackAsync(IPage page)
  {
    await page.WaitForFunctionAsync(
      """
      () => {
        const button = document.querySelector('[data-qa=FeedbackSubmit]');
        const enabled = button && !button.hasAttribute('disabled');
        if (!enabled) { window.feedbackSubmitEnabledSince = undefined; return false; }
        window.feedbackSubmitEnabledSince ??= performance.now();
        return performance.now() - window.feedbackSubmitEnabledSince >= 500;
      }
      """,
      null,
      new PageWaitForFunctionOptions { Timeout = 30_000, PollingInterval = 50 });
    await page.Locator("[data-qa=FeedbackSubmit]").ClickAsync();

    ILocator receipt = page.Locator("[data-qa=FeedbackReceiptId]");
    try
    {
      await receipt.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    }
    catch (TimeoutException exception)
    {
      await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("feedback-reply-submit-failed.png"), FullPage = true });
      throw new TimeoutException("Feedback submit showed no receipt after one click on an enabled button.", exception);
    }

    return receipt;
  }

  private static async Task AssertBoxesFillTheirFieldAsync(IPage page, string surface, string[] qas)
  {
    foreach (string qa in qas)
    {
      await AssertFillsFieldAsync(page, qa, $"{surface}-1280");
    }

    await page.ScreenshotAsync(new PageScreenshotOptions
    {
      Path = ScreenshotPath($"feedback-{surface}-textareas-1280.png"),
      FullPage = true,
    });

    foreach (string qa in qas)
    {
      await AssertGripResizesAsync(page, qa);
    }

    await page.SetViewportSizeAsync(600, 900);
    await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
    foreach (string qa in qas)
    {
      await AssertFillsFieldAsync(page, qa, $"{surface}-600");
    }

    await page.ScreenshotAsync(new PageScreenshotOptions
    {
      Path = ScreenshotPath($"feedback-{surface}-textareas-600.png"),
      FullPage = true,
    });
  }

  // The field is the element holding the shared TextArea wrapper; its content box is the width
  // the visible part=root must fill. A narrow box (18rem default) fails the right-edge check.
  private static async Task AssertFillsFieldAsync(IPage page, string qa, string viewport)
  {
    const string fieldScript = """
      (qa) => {
        const host = document.querySelector(`[data-qa=${qa}]`);
        const wrapper = host && host.closest('.twe-text-area');
        const field = wrapper && wrapper.parentElement;
        if (!field) {
          return null;
        }
        const rect = field.getBoundingClientRect();
        const style = getComputedStyle(field);
        const left = rect.left + parseFloat(style.paddingLeft) + parseFloat(style.borderLeftWidth);
        const right = rect.right - parseFloat(style.paddingRight) - parseFloat(style.borderRightWidth);
        return [left, right];
      }
      """;
    await page.Locator($"[data-qa={qa}]").ScrollIntoViewIfNeededAsync();
    float[] field = (await page.EvaluateAsync<float[]?>(fieldScript, qa)).ShouldNotBeNull($"{qa} has no TextArea field");
    EdgeBox fieldBox = new(field[0], 0f, field[1] - field[0], 0f);
    EdgeBox root = await ShadowBoxAsync(page, qa, "root");
    EdgeBox control = await ShadowBoxAsync(page, qa, "control");
    string report = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"viewport={viewport} qa={qa} field={fieldBox.X:0.#}..{fieldBox.Right:0.#} {fieldBox.Width:0.#} root={root.X:0.#}..{root.Right:0.#} {root.Width:0.#}x{root.Height:0.#} control={control.Width:0.#}x{control.Height:0.#}");
    Console.WriteLine(report);
    SameEdges(fieldBox, root, report);
    ((double)root.Height).ShouldBeGreaterThanOrEqualTo(140d, report);
    ((double)control.Height).ShouldBeGreaterThan(100d, report);
  }

  private static async Task AssertGripResizesAsync(IPage page, string qa)
  {
    const float dragDistance = 80f;
    ILocator rootLocator = page.Locator($"[data-qa={qa}]").Locator("[part=root]");
    await rootLocator.ScrollIntoViewIfNeededAsync();
    EdgeBox before = await ShadowBoxAsync(page, qa, "root");
    float gripX = before.Right - 8f;
    float gripY = before.Y + before.Height - 4f;
    await page.Mouse.MoveAsync(gripX, gripY);
    await page.Mouse.DownAsync();
    await page.Mouse.MoveAsync(gripX, gripY + dragDistance, new MouseMoveOptions { Steps = 12 });
    await page.Mouse.UpAsync();
    EdgeBox after = await ShadowBoxAsync(page, qa, "root");
    double grown = after.Height - before.Height;
    string dragReport = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"qa={qa} drag={dragDistance:0.#} before={before.Height:0.#} after={after.Height:0.#} grown={grown:0.#}");
    Console.WriteLine(dragReport);
    grown.ShouldBeGreaterThan(dragDistance - 20d, dragReport);
    grown.ShouldBeLessThan(dragDistance + 20d, dragReport);
  }

  private static async Task OpenFeedbackAsync(IPage page, ConcurrentQueue<string> console)
  {
    string feedbackUrl = InProcTestPorts.WebHostUrl.TrimEnd('/') + "/Feedback";
    await page.GotoAsync(feedbackUrl, new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    try
    {
      await page.Locator(".twe-appbar").WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
      await page.Locator("[data-qa=FeedbackBody]").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    }
    catch (TimeoutException exception)
    {
      string startupLog = string.Join('\n', console);
      throw new TimeoutException(
        $"Feedback form did not render at {page.Url}. Console:\n{startupLog}",
        exception);
    }
  }

  private static async Task AssertDetailsMatchesTitleAsync(IPage page, string viewport)
  {
    EdgeBox host = await HostBoxAsync(page, "FeedbackBody");
    EdgeBox titleRoot = await ShadowBoxAsync(page, "FeedbackTitle", "root");
    EdgeBox detailsRoot = await ShadowBoxAsync(page, "FeedbackBody", "root");
    EdgeBox titleControl = await ShadowBoxAsync(page, "FeedbackTitle", "control");
    EdgeBox detailsControl = await ShadowBoxAsync(page, "FeedbackBody", "control");
    string report = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"viewport={viewport} host={host.Width:0.#}x{host.Height:0.#} detailsRoot={detailsRoot.X:0.#}..{detailsRoot.Right:0.#} {detailsRoot.Width:0.#}x{detailsRoot.Height:0.#} titleRoot={titleRoot.X:0.#}..{titleRoot.Right:0.#} {titleRoot.Width:0.#}x{titleRoot.Height:0.#} detailsControl={detailsControl.X:0.#}..{detailsControl.Right:0.#} {detailsControl.Width:0.#}x{detailsControl.Height:0.#} titleControl={titleControl.X:0.#}..{titleControl.Right:0.#} {titleControl.Width:0.#}x{titleControl.Height:0.#}");
    Console.WriteLine(report);
    SameEdges(titleRoot, detailsRoot, report);
    SameEdges(titleControl, detailsControl, report);
    ((double)detailsRoot.Height).ShouldBeGreaterThanOrEqualTo(140d, report);
    ((double)detailsControl.Height).ShouldBeGreaterThan(100d, report);
  }

  private static void SameEdges(EdgeBox expected, EdgeBox actual, string report)
  {
    ((double)Math.Abs(expected.X - actual.X)).ShouldBeLessThan(4d, report);
    ((double)Math.Abs(expected.Right - actual.Right)).ShouldBeLessThan(4d, report);
  }

  private static async Task<EdgeBox> HostBoxAsync(IPage page, string qa)
  {
    LocatorBoundingBoxResult box = (await page.Locator($"[data-qa={qa}]").BoundingBoxAsync()).ShouldNotBeNull();
    return new EdgeBox(box.X, box.Y, box.Width, box.Height);
  }

  private static async Task<EdgeBox> ShadowBoxAsync(IPage page, string qa, string part)
  {
    ILocator locator = page.Locator($"[data-qa={qa}]").Locator($"[part={part}]");
    LocatorBoundingBoxResult box = (await locator.BoundingBoxAsync()).ShouldNotBeNull($"{qa} part={part}");
    return new EdgeBox(box.X, box.Y, box.Width, box.Height);
  }

  private readonly record struct EdgeBox(float X, float Y, float Width, float Height)
  {
    public float Right => X + Width;
  }

  private static async Task SignInWithNewPasskeyAsync(IPage page, IBrowserContext context)
  {
    // A platform authenticator that always verifies the user, so the passkey ceremony completes headless.
    ICDPSession cdp = await context.NewCDPSessionAsync(page);
    await cdp.SendAsync("WebAuthn.enable");
    await cdp.SendAsync("WebAuthn.addVirtualAuthenticator", new Dictionary<string, object>
    {
      ["options"] = new Dictionary<string, object>
      {
        ["protocol"] = "ctap2",
        ["transport"] = "internal",
        ["hasResidentKey"] = true,
        ["hasUserVerification"] = true,
        ["isUserVerified"] = true,
        ["automaticPresenceSimulation"] = true,
      },
    });

    await page.GotoAsync(InProcTestPorts.WebHostUrl, new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    await page.Locator(".twe-appbar").WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
    await page.Locator("[data-qa=HomeSignIn]").ClickAsync();
    await page.WaitForURLAsync("**/Login**", new PageWaitForURLOptions { Timeout = 30_000 });
    await page.Locator("[data-qa=CreatePasskey]").ClickAsync();
    await page.WaitForURLAsync("**/Settings", new PageWaitForURLOptions { Timeout = 60_000 });
    await page.Locator(".twe-appbar").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
  }

  private static void InstallChromium()
  {
    int install = Microsoft.Playwright.Program.Main(["install", "chromium"]);
    if (install == 0)
    {
      return;
    }

    Environment.SetEnvironmentVariable("PLAYWRIGHT_HOST_PLATFORM_OVERRIDE", "ubuntu24.04-x64");
    install = Microsoft.Playwright.Program.Main(["install", "chromium"]);
    install.ShouldBe(0, "Playwright chromium install failed");
  }

  private static string ScreenshotPath(string fileName) =>
    PlaywrightScreenshot.PathFor<FeedbackAttachment_Given_Wasm>(fileName);
}
