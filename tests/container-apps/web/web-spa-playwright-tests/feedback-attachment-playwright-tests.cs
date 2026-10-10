#region Purpose
// Browser proof that the feedback form accepts a picked file and a pasted image.
#endregion

#region Design
// Same host as the other WASM proofs: appsettings UseMock on the server, wwwroot UseMock left
// false so the page calls the real in-memory API. /Feedback is [Authorize] for
// feedback.file.self. That gate reads the SPA authentication state, which comes from the
// identity-session cookie. X-TimeWarp-Mock-Principal-Id authenticates server API calls only,
// so a direct visit while signed out renders RedirectToLogin (no app bar). The test creates
// an account with a virtual passkey, the same ceremony as AskSignIn_Given_Wasm, then opens
// /Feedback. A new account's role includes feedback.file.self. Shots are written beside task
// 295. Chromium install retries with the ubuntu24.04 build when the host distro is newer than
// Playwright's platform list.
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

    Volatile.Read(ref sawWasm).ShouldBe(1, "InteractiveWebAssembly did not download a .wasm");

    LocatorBoundingBoxResult titleBox = (await page.Locator("[data-qa=FeedbackTitle]").BoundingBoxAsync())
      .ShouldNotBeNull();
    LocatorBoundingBoxResult detailsBox = (await page.Locator("[data-qa=FeedbackBody]").BoundingBoxAsync())
      .ShouldNotBeNull();
    ((double)Math.Abs(titleBox.Width - detailsBox.Width)).ShouldBeLessThan(8d);
    ((double)detailsBox.Height).ShouldBeGreaterThan(100d);

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

  private static string ScreenshotPath(string fileName)
  {
    string? directory = AppContext.BaseDirectory;
    while (directory is not null && !File.Exists(Path.Combine(directory, "timewarp-architecture.slnx")))
    {
      directory = Path.GetDirectoryName(directory);
    }

    directory.ShouldNotBeNull();
    string folder = Directory.GetDirectories(Path.Combine(directory, "kanban"), "295-*", SearchOption.AllDirectories)
      .Single(path => File.Exists(Path.Combine(path, "task.md")));
    return Path.Combine(folder, fileName);
  }
}
