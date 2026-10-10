#region Purpose
// Browser proof that the feedback form accepts a picked file and a pasted image,
// and that the Details visible box matches Title.
#endregion

#region Design
// Same host as the other WASM proofs: appsettings UseMock on the server, wwwroot UseMock left
// false so the page calls the real in-memory API. /Feedback is [Authorize] for
// feedback.file.self. That gate reads the SPA authentication state, which comes from the
// identity-session cookie. X-TimeWarp-Mock-Principal-Id authenticates server API calls only,
// so a direct visit while signed out renders RedirectToLogin (no app bar). The test creates
// an account with a virtual passkey, the same ceremony as AskSignIn_Given_Wasm, then opens
// /Feedback. A new account's role includes feedback.file.self. Attachment shots are written
// beside task 295. The Details layout shot is written beside task 302. Without kanban/ (a
// generated app) shots go to the test output folder. Chromium install
// retries with the ubuntu24.04 build when the host distro is newer than Playwright's platform
// list. Layout measures fluent-textarea's shadow part=root and part=control against Title's
// shadow part=root and input. The host element can be full width while the visible box stays
// at the component's 18rem inline size, so a host BoundingBox check does not prove the fix.
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
      Path = ScreenshotPath("feedback-details-302.png", "302"),
      FullPage = true,
    });

    const float dragDistance = 80f;
    ILocator detailsRoot = page.Locator("[data-qa=FeedbackBody]").Locator("[part=root]");
    await detailsRoot.ScrollIntoViewIfNeededAsync();
    EdgeBox before = await ShadowBoxAsync(page, "FeedbackBody", "root");
    float gripX = before.Right - 8f;
    float gripY = before.Y + before.Height - 4f;
    await page.Mouse.MoveAsync(gripX, gripY);
    await page.Mouse.DownAsync();
    await page.Mouse.MoveAsync(gripX, gripY + dragDistance, new MouseMoveOptions { Steps = 12 });
    await page.Mouse.UpAsync();
    EdgeBox after = await ShadowBoxAsync(page, "FeedbackBody", "root");
    double grown = after.Height - before.Height;
    string dragReport = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"drag={dragDistance:0.#} before={before.Height:0.#} after={after.Height:0.#} grown={grown:0.#}");
    Console.WriteLine(dragReport);
    grown.ShouldBeGreaterThan(dragDistance - 20d, dragReport);
    grown.ShouldBeLessThan(dragDistance + 20d, dragReport);

    await page.SetViewportSizeAsync(600, 900);
    await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
    await AssertDetailsMatchesTitleAsync(page, "narrow-600");
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

  private static string ScreenshotPath(string fileName, string taskPrefix = "295")
  {
    string? directory = AppContext.BaseDirectory;
    while (directory is not null && !File.Exists(Path.Combine(directory, "timewarp-architecture.slnx")))
    {
      directory = Path.GetDirectoryName(directory);
    }

    // Generated apps ship tests/ without kanban/, so the shot falls back to the test output folder.
    string? kanban = directory is null ? null : Path.Combine(directory, "kanban");
    string? folder = kanban is not null && Directory.Exists(kanban)
      ? Directory.GetDirectories(kanban, taskPrefix + "-*", SearchOption.AllDirectories)
        .SingleOrDefault(path => File.Exists(Path.Combine(path, "task.md")))
      : null;
    return Path.Combine(folder ?? AppContext.BaseDirectory, fileName);
  }
}
