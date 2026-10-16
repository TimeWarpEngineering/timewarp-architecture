#region Purpose
// Browser proof for task 303-001: the app draws its own page to a real PNG, masks data-no-capture,
// and both Ask and Feedback attach the capture as a removable thumbnail and send it.
#endregion

#region Design
// Real InteractiveWebAssembly against Web.Server with the fake chat upstream (no request leaves the
// process) and a passkey account from a Chrome virtual authenticator, the same sign-in as
// FeedbackAttachment_Given_Wasm. On /Settings the test puts two probe boxes at the top of the page
// body: a red one and a blue one marked data-no-capture. Ask's camera button dispatches
// ScreenshotState.CaptureScreenshot; the thumbnail's data: URI is the stored PNG. The test checks
// the PNG signature and IHDR size in .NET, then decodes it in the browser and counts pixels: red
// must be there (the page really was drawn), blue must not (the mask held), and the mask grey must
// cover the secret box. Remove takes the thumbnail away; a second capture puts it back, and the
// message sent with it carries one image/png on the user turn of the recorded completion request,
// which the fake upstream answers with "image: image/png WxH". The thumbnail is gone after send.
// Feedback is reached by client-side navigation (Blazor.navigateTo), so the store keeps the
// /Settings capture: "Attach capture of /Settings" (the page behind) and "Capture this page" each
// add a removable preview through the normal upload action; one is removed, the item is filed, and
// its permalink shows exactly one attachment image whose size matches the capture. The PNG and the
// panel and form shots go to test output (PlaywrightScreenshot).
#endregion

namespace ScreenshotPlaywright_;

using System.Buffers.Binary;
using System.Text.Json;

public class Screenshot_Given_Wasm
{
  private const string CompletionsPath = "/api/agent-chat/completions";
  private const int MaskGrey = 96;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Screenshot_Given_Wasm>();

  public static async Task Capture_Should_DrawThePage_MaskSecrets_And_FeedAskAndFeedback()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
      services.PostConfigure<XaiChatOptions>(options =>
      {
        options.UseFakeUpstream = true;
        options.ApiKey = "playwright-placeholder-key";
      }));
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
    ConcurrentQueue<string> completions = new();
    page.Console += (_, message) => console.Enqueue($"{message.Type}: {message.Text}");
    page.PageError += (_, error) => console.Enqueue(error);
    page.Request += (_, request) =>
    {
      if (request.Method == "POST"
        && new Uri(request.Url).AbsolutePath.Equals(CompletionsPath, StringComparison.OrdinalIgnoreCase)
        && request.PostData is { } body)
      {
        completions.Enqueue(body);
      }
    };

    await SignInWithNewPasskeyAsync(page, context);
    await page.Locator(".twe-page__body").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    await InjectProbesAsync(page);

    // Ask: capture the page behind the panel.
    await page.Locator("[data-qa=AskAiButton]").ClickAsync();
    ILocator input = page.Locator(".sc-ai-input__textarea");
    await input.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000, State = WaitForSelectorState.Visible });
    ILocator chip = page.Locator("[data-qa=AskScreenshot]");
    await page.Locator("[data-qa=AskCapture]").ClickAsync();
    try
    {
      await chip.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    }
    catch (TimeoutException exception)
    {
      await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("ask-capture-failed.png") });
      string text = await page.Locator("body").InnerTextAsync();
      throw new TimeoutException(
        $"Ask showed no screenshot. Page text:\n{text}\nConsole:\n{string.Join('\n', console)}",
        exception);
    }

    await page.GetByText("Screenshot captured").First.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
    (await page.Locator("[data-qa=AskScreenshotCaption]").InnerTextAsync()).ShouldContain("of /Settings");

    string source = (await page.Locator("[data-qa=AskScreenshotThumbnail]").GetAttributeAsync("src")).ShouldNotBeNull();
    const string prefix = "data:image/png;base64,";
    source.ShouldStartWith(prefix);
    byte[] png = Convert.FromBase64String(source[prefix.Length..]);
    await File.WriteAllBytesAsync(ScreenshotPath("capture-settings.png"), png);
    (int width, int height) = PngSize(png);
    width.ShouldBeGreaterThan(100);
    height.ShouldBeGreaterThan(48);
    png.Length.ShouldBeGreaterThan(1_000);

    int[] pixels = await page.EvaluateAsync<int[]>(
      """
      async (src) => {
        const image = new Image();
        image.src = src;
        await image.decode();
        const canvas = document.createElement('canvas');
        canvas.width = image.naturalWidth;
        canvas.height = image.naturalHeight;
        const context = canvas.getContext('2d');
        context.drawImage(image, 0, 0);
        const data = context.getImageData(0, 0, canvas.width, canvas.height).data;
        let red = 0, blue = 0, grey = 0;
        const colors = new Set();
        for (let index = 0; index < data.length; index += 4) {
          const r = data[index], g = data[index + 1], b = data[index + 2];
          if (r > 200 && g < 40 && b < 40) red++;
          if (b > 200 && r < 40 && g < 40) blue++;
          if (r === 96 && g === 96 && b === 96) grey++;
          if (colors.size < 64) colors.add((r << 16) | (g << 8) | b);
        }
        return [canvas.width, canvas.height, red, blue, grey, colors.size];
      }
      """,
      source);
    pixels[0].ShouldBe(width);
    pixels[1].ShouldBe(height);
    pixels[2].ShouldBeGreaterThan(4_000, "the unmasked red probe (160x48) must be drawn");
    pixels[3].ShouldBe(0, "the data-no-capture blue probe must not appear");
    pixels[4].ShouldBeGreaterThan(4_000, $"the mask (rgb {MaskGrey}) must cover the secret probe");
    pixels[5].ShouldBeGreaterThan(8, "the capture must not be a blank picture");
    await page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions { Path = ScreenshotPath("ask-screenshot-thumbnail.png") });

    // Removable, then attached again and sent with the message.
    await page.Locator("[data-qa=AskScreenshotRemove]").ClickAsync();
    await chip.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached, Timeout = 10_000 });
    await page.Locator("[data-qa=AskCapture]").ClickAsync();
    await chip.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    int before = completions.Count;
    await input.FillAsync("What is wrong on this page?");
    await page.Locator(".sc-ai-input__send").ClickAsync();
    await page.Locator(".sc-ai-root").GetByText("image: image/png").WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
    await chip.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached, Timeout = 10_000 });
    string sent = completions.Skip(before).First();
    (string mediaType, string data) = LastUserImage(sent);
    mediaType.ShouldBe("image/png");
    (int sentWidth, int sentHeight) = PngSize(Convert.FromBase64String(data));
    (await page.Locator(".sc-ai-root").InnerTextAsync()).ShouldContain($"image: image/png {sentWidth}x{sentHeight}");
    await page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions { Path = ScreenshotPath("ask-screenshot-sent.png") });
    await page.Locator("[data-qa=AgentAskClose]").ClickAsync();

    // Feedback: the page behind is the /Settings capture; this page can be captured too.
    await page.EvaluateAsync("() => Blazor.navigateTo('/Feedback')");
    await page.Locator("[data-qa=FeedbackBody]").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    await page.Locator("[data-qa=FeedbackTitle]").Locator("input").FillAsync("Settings looks off");
    await page.Locator("[data-qa=FeedbackBody]").Locator("textarea").FillAsync("See the screenshot.");
    ILocator behind = page.Locator("[data-qa=FeedbackAttachPageBehind]");
    (await behind.InnerTextAsync()).ShouldContain("/Settings");
    await behind.ClickAsync();
    ILocator attachments = page.Locator("[data-qa=FeedbackAttachment]");
    await Assertions.Expect(attachments).ToHaveCountAsync(1, new LocatorAssertionsToHaveCountOptions { Timeout = 30_000 });
    await behind.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached, Timeout = 10_000 });
    await page.Locator("[data-qa=FeedbackCaptureScreenshot]").ClickAsync();
    await Assertions.Expect(attachments).ToHaveCountAsync(2, new LocatorAssertionsToHaveCountOptions { Timeout = 30_000 });
    (await attachments.Locator("img").CountAsync()).ShouldBe(2);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("feedback-screenshots.png") });
    await attachments.Nth(1).Locator("[data-qa=FeedbackAttachmentRemove]").ClickAsync();
    await Assertions.Expect(attachments).ToHaveCountAsync(1, new LocatorAssertionsToHaveCountOptions { Timeout = 30_000 });

    await SubmitFeedbackAsync(page);
    await page.Locator("[data-qa=FeedbackPermalink]").ClickAsync();
    ILocator filed = page.Locator("img[src*='/api/Feedback/attachments/']");
    await filed.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    (await filed.CountAsync()).ShouldBe(1);
    await page.WaitForFunctionAsync(
      "() => { const image = document.querySelector(\"img[src*='/api/Feedback/attachments/']\"); return image && image.complete && image.naturalWidth > 0; }",
      null,
      new PageWaitForFunctionOptions { Timeout = 30_000 });
    int filedWidth = await filed.First.EvaluateAsync<int>("image => image.naturalWidth");
    filedWidth.ShouldBe(width, "the filed attachment is the /Settings capture");
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("feedback-item-screenshot.png") });

    string log = string.Join('\n', console);
    log.ShouldNotContain("Unhandled exception rendering component");
    log.ShouldNotContain("Screenshot capture failed");
  }

  private static async Task InjectProbesAsync(IPage page)
  {
    string injected = await page.EvaluateAsync<string>(
      """
      () => {
        const body = document.querySelector('.twe-page__body');
        if (!body) {
          return 'missing-body';
        }
        const probes = document.createElement('div');
        probes.id = 'capture-probes';
        const visible = document.createElement('div');
        visible.style.cssText = 'width:160px;height:48px;background:rgb(255,0,0)';
        const secret = document.createElement('div');
        secret.setAttribute('data-no-capture', '');
        secret.textContent = 'SECRET';
        secret.style.cssText = 'width:160px;height:48px;background:rgb(0,0,255);color:rgb(0,0,255)';
        probes.append(visible, secret);
        body.prepend(probes);
        return 'injected';
      }
      """);
    injected.ShouldBe("injected");
  }

  private static (int Width, int Height) PngSize(byte[] png)
  {
    png.Length.ShouldBeGreaterThan(24);
    png[..8].ShouldBe(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
    System.Text.Encoding.ASCII.GetString(png, 12, 4).ShouldBe("IHDR");
    return
    (
      BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)),
      BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4))
    );
  }

  /// <summary>The first image on the last user turn of a completion request body.</summary>
  private static (string MediaType, string Data) LastUserImage(string body)
  {
    using JsonDocument document = JsonDocument.Parse(body);
    JsonElement? last = null;
    foreach (JsonElement turn in document.RootElement.GetProperty("messages").EnumerateArray())
    {
      if (string.Equals(turn.GetProperty("role").GetString(), "user", StringComparison.OrdinalIgnoreCase))
      {
        last = turn.Clone();
      }
    }

    JsonElement image = last.ShouldNotBeNull().GetProperty("images").EnumerateArray().Single();
    return (image.GetProperty("mediaType").GetString() ?? "", image.GetProperty("data").GetString() ?? "");
  }

  private static async Task SubmitFeedbackAsync(IPage page)
  {
    // Submit is disabled while any action runs. Click once, after it has stayed enabled for 500ms.
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
    await page.Locator("[data-qa=FeedbackReceiptId]").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
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

  private static string ScreenshotPath(string fileName) =>
    PlaywrightScreenshot.PathFor<Screenshot_Given_Wasm>(fileName);
}
