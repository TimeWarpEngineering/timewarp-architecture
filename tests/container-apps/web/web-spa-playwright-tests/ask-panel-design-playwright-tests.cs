#region Purpose
// Browser proof of the Ask panel's empty-state design (task 349): header, greeting, starter
// cards, the privacy notice, and the composer, docked at 1280 and full-viewport at 800.
#endregion

#region Design
// Same host as AskSurface_Given_Wasm (in-process Web.Server, fake upstream, mock principal), with
// XAI:RecordChats on and a PrivacyUrl so the notice and its link render. The top-bar Ask AI
// button opens the panel. The checks are layout facts, not pixels: the greeting sits above the
// cards, every card has its icon tile, title and subtitle, the notice sits directly above the
// composer, and the composer is the last block of the panel body. Shots go to test output
// (PlaywrightScreenshot) as the before/after record for design changes.
#endregion

namespace AskPanelDesignPlaywright_;

public class AskPanelDesign_Given_Wasm
{
  private const string MockPrincipalId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskPanelDesign_Given_Wasm>();

  public static async Task EmptyState_Should_ShowGreetingCardsNoticeAndComposer()
  {
    if (Microsoft.Playwright.Program.Main(["install", "chromium"]) != 0)
    {
      // Playwright 1.55 has no ubuntu26.04 browser build. The 24.04 build is the fallback.
      Environment.SetEnvironmentVariable("PLAYWRIGHT_HOST_PLATFORM_OVERRIDE", "ubuntu24.04-x64");
      Microsoft.Playwright.Program.Main(["install", "chromium"]).ShouldBe(0, "Playwright chromium install failed");
    }

    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
    {
      services.PostConfigure<XaiChatOptions>(options =>
      {
        options.UseFakeUpstream = true;
        options.ApiKey = "playwright-placeholder-key";
        options.RecordChats = true;
        options.PrivacyUrl = "/privacy";
      });
    });

    using IPlaywright playwright = await Playwright.CreateAsync();
    await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
      // The host locale can be en-us@posix, which .NET WASM rejects and never renders the shell.
      Locale = "en-US",
      ExtraHTTPHeaders = new Dictionary<string, string>
      {
        ["X-TimeWarp-Mock-Principal-Id"] = MockPrincipalId,
      },
    });
    IPage page = await context.NewPageAsync();
    await page.GotoAsync(InProcTestPorts.WebHostUrl, new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    ILocator button = page.Locator("[data-qa=AskAiButton]");
    await button.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });

    // The button is interactive after the shell's first WebAssembly render. Retry until Ask opens.
    ILocator input = page.Locator(".twe-agent-ask .sc-ai-input__textarea");
    bool opened = false;
    for (int attempt = 0; attempt < 15 && !opened; attempt++)
    {
      await button.ClickAsync();
      try
      {
        await input.WaitForAsync(new LocatorWaitForOptions { Timeout = 4_000 });
        opened = true;
      }
      catch (TimeoutException)
      {
      }
    }

    opened.ShouldBeTrue("Ask AI did not open the panel with a composer");
    ILocator notice = page.Locator("[data-qa=AskPrivacyNotice]");
    await notice.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    await page.Locator("[data-qa=AskPrivacyLink]").WaitForAsync();

    await page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-empty-state-1280.png"),
    });

    await page.SetViewportSizeAsync(800, 700);
    await WaitForBoxAsync(page, "[data-qa=AgentAsk]", box => box.X <= 2 && Math.Abs(box.Width - 800) <= 2);
    await page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-empty-state-800.png"),
    });

    await page.SetViewportSizeAsync(1280, 800);
    await WaitForBoxAsync(page, "[data-qa=AgentAsk]", box => Math.Abs(box.Width - 450) <= 2);

    LocatorBoundingBoxResult greeting = await BoxAsync(page, "[data-qa=AskGreeting]");
    LocatorBoundingBoxResult firstCard = await BoxAsync(page, "[data-qa=AskStarterPrompt] >> nth=0");
    ((double)firstCard.Y).ShouldBeGreaterThan(greeting.Y + greeting.Height - 1);

    int cards = await page.Locator("[data-qa=AskStarterPrompt]").CountAsync();
    cards.ShouldBeGreaterThanOrEqualTo(3);
    for (int index = 0; index < cards; index++)
    {
      ILocator card = page.Locator("[data-qa=AskStarterPrompt]").Nth(index);
      (await card.Locator(".twe-ask-starters__icon svg").CountAsync()).ShouldBe(1, $"card {index} icon");
      (await card.Locator("[data-qa=AskStarterPromptTitle]").InnerTextAsync()).ShouldNotBeNullOrWhiteSpace();
      (await card.Locator("[data-qa=AskStarterPromptSubtitle]").InnerTextAsync()).ShouldNotBeNullOrWhiteSpace();
    }

    LocatorBoundingBoxResult noticeBox = await BoxAsync(page, "[data-qa=AskPrivacyNotice]");
    LocatorBoundingBoxResult composer = await BoxAsync(page, ".twe-agent-ask .sc-ai-input");
    double gap = composer.Y - (noticeBox.Y + noticeBox.Height);
    gap.ShouldBeGreaterThanOrEqualTo(0);
    gap.ShouldBeLessThanOrEqualTo(24);
    LocatorBoundingBoxResult panel = await BoxAsync(page, "[data-qa=AgentAsk]");
    ((double)(composer.Y + composer.Height)).ShouldBeLessThanOrEqualTo(panel.Y + panel.Height + 1);
  }

  private static string ScreenshotPath(string fileName) =>
    PlaywrightScreenshot.PathFor<AskPanelDesign_Given_Wasm>(fileName);

  private static async Task<LocatorBoundingBoxResult> BoxAsync(IPage page, string selector)
  {
    LocatorBoundingBoxResult? box = await page.Locator(selector).BoundingBoxAsync();
    box.ShouldNotBeNull(selector);
    return box;
  }

  private static async Task<LocatorBoundingBoxResult> WaitForBoxAsync(
    IPage page,
    string selector,
    Func<LocatorBoundingBoxResult, bool> ready)
  {
    LocatorBoundingBoxResult? last = null;
    for (int attempt = 0; attempt < 50; attempt++)
    {
      last = await page.Locator(selector).BoundingBoxAsync();
      if (last is not null && ready(last))
      {
        return last;
      }

      await Task.Delay(100);
    }

    last.ShouldNotBeNull(selector);
    ready(last).ShouldBeTrue($"{selector} never reached the expected box: x={last.X}, width={last.Width}");
    return last;
  }
}
