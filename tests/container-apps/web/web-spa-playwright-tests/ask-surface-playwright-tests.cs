#region Purpose
// Browser proof that Ctrl-K Ask renders in WebAssembly, with and without an xAI key.
#endregion

#region Design
// WebApplicationHost layers this project's appsettings (UseMock, InteractiveWebAssembly,
// prerender off) and strips secrets.json. PostConfigure clears any machine XAI__ApiKey.
// The configured pass sets UseFakeUpstream so no request leaves the process. The mock
// principal header lets the relay's identity-session policy succeed. Opening Ctrl-K dispatches
// ApplicationState.SetActiveModal through StateTransactionBehavior, so a visible Ask proves a
// store action ran in WASM. Console text is
// checked for the TimeWarp.State SemaphoreSlim.Wait failure noted on 2026-10-09.
// Chromium install retries with the ubuntu24.04 build when the host distro is newer than
// Playwright 1.55's platform list.
#endregion

namespace AskSurfacePlaywright_;

public class AskSurface_Given_Wasm
{
  private const string MockPrincipalId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskSurface_Given_Wasm>();

  public static async Task CtrlK_Should_ShowAsk_And_TheKeyState()
  {
    InstallChromium();

    await RunAsync(useFakeUpstream: false);
    await RunAsync(useFakeUpstream: true);
  }

  private static void InstallChromium()
  {
    int install = Microsoft.Playwright.Program.Main(["install", "chromium"]);
    if (install == 0)
    {
      return;
    }

    // Playwright 1.55 has no ubuntu26.04 browser build. The 24.04 build is the fallback.
    Environment.SetEnvironmentVariable("PLAYWRIGHT_HOST_PLATFORM_OVERRIDE", "ubuntu24.04-x64");
    install = Microsoft.Playwright.Program.Main(["install", "chromium"]);
    install.ShouldBe(0, "Playwright chromium install failed");
  }

  private static async Task RunAsync(bool useFakeUpstream)
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
    {
      services.PostConfigure<XaiChatOptions>(options =>
      {
        options.UseFakeUpstream = useFakeUpstream;
        // Configured pass: a placeholder XAI:ApiKey is set; the fake upstream still wins, so no
        // request leaves the process. Not-configured pass: no key at all.
        options.ApiKey = useFakeUpstream ? "playwright-placeholder-key" : null;
      });
    });

    using IPlaywright playwright = await Playwright.CreateAsync();
    await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
      Headless = true,
    });
    await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      // The host locale can be en-us@posix, which .NET WASM rejects and never renders the shell.
      Locale = "en-US",
      ExtraHTTPHeaders = new Dictionary<string, string>
      {
        ["X-TimeWarp-Mock-Principal-Id"] = MockPrincipalId,
      },
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

    await page.GotoAsync(InProcTestPorts.WebHostUrl, new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    ILocator appBar = page.Locator(".twe-appbar");
    try
    {
      await appBar.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
    }
    catch (TimeoutException exception)
    {
      string startupLog = string.Join('\n', console);
      throw new TimeoutException($"App bar did not render. Console:\n{startupLog}", exception);
    }

    Volatile.Read(ref sawWasm).ShouldBe(1, "InteractiveWebAssembly did not download a .wasm");
    string html = await page.ContentAsync();
    html.ShouldContain("webassembly", Case.Insensitive);

    // The palette listener is attached on the shell's first interactive render, after the app bar
    // is already in the document. Retry Ctrl-K until Ask is visible.
    ILocator ask = page.Locator("[data-qa=CommandPaletteAsk]");
    bool opened = false;
    for (int attempt = 0; attempt < 15 && !opened; attempt++)
    {
      await page.Keyboard.PressAsync("Control+k");
      try
      {
        await ask.WaitForAsync(new LocatorWaitForOptions
        {
          Timeout = 2_000,
          State = WaitForSelectorState.Visible,
        });
        opened = true;
      }
      catch (TimeoutException)
      {
        await page.Keyboard.PressAsync("Escape");
      }
    }

    opened.ShouldBeTrue("Ctrl-K did not show Ask");
    await ask.ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath(useFakeUpstream ? "ctrl-k-ask-configured.png" : "ctrl-k-ask.png"),
    });
    await page.ScreenshotAsync(new PageScreenshotOptions
    {
      Path = ScreenshotPath(useFakeUpstream ? "ctrl-k-palette-configured-page.png" : "ctrl-k-palette-page.png"),
    });

    await ask.ClickAsync();
    if (useFakeUpstream)
    {
      ILocator input = page.Locator(".sc-ai-input__textarea");
      await input.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
      await input.FillAsync("What is on this page?");
      await page.Locator(".sc-ai-input__send").ClickAsync();
      ILocator answer = page.Locator(".sc-ai-root").GetByText("page_context:");
      await answer.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
      string text = await page.Locator(".sc-ai-root").InnerTextAsync();
      text.ShouldContain("page_context:");
      text.ShouldContain("path");
      text.ShouldNotContain("\\u0022");
      // The reply must not push the modal title out of the dialog (ChatPage is 100vh by default).
      LocatorBoundingBoxResult? title = await page.Locator(".twe-agent-ask__title").BoundingBoxAsync();
      LocatorBoundingBoxResult? chat = await page.Locator(".sc-ai-chat-page").BoundingBoxAsync();
      title.ShouldNotBeNull();
      chat.ShouldNotBeNull();
      title.Y.ShouldBeGreaterThanOrEqualTo(0);
      (chat.Y + chat.Height).ShouldBeLessThanOrEqualTo(page.ViewportSize!.Height);
      await page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
      {
        Path = ScreenshotPath("ctrl-k-ask-result.png"),
      });
      await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("ctrl-k-ask-result-page.png") });
    }
    else
    {
      ILocator missing = page.Locator("[data-qa=AgentAskNotConfigured]");
      await missing.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
      (await missing.InnerTextAsync()).ShouldBe("AI not configured");
      string command = await page.Locator("[data-qa=AgentAskSetupCommand]").InnerTextAsync();
      command.ShouldBe(XaiChatDefaults.SetupCommand);
      await page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
      {
        Path = ScreenshotPath("ctrl-k-ask-not-configured.png"),
      });
      await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("ctrl-k-ask-not-configured-page.png") });
    }

    string log = string.Join('\n', console);
    log.ShouldNotContain("PlatformNotSupportedException");
    log.ShouldNotContain("SemaphoreSlim.Wait");
  }

  private static string ScreenshotPath(string fileName)
  {
    string? directory = AppContext.BaseDirectory;
    while (directory is not null && !File.Exists(Path.Combine(directory, "timewarp-architecture.slnx")))
    {
      directory = Path.GetDirectoryName(directory);
    }

    directory.ShouldNotBeNull();
    string[] matches = Directory.GetDirectories(Path.Combine(directory, "kanban"), "289-*", SearchOption.AllDirectories);
    string folder = matches.Single(path => File.Exists(Path.Combine(path, "task.md")));
    return Path.Combine(folder, fileName);
  }
}
