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
// The configured pass also proves the review-round-1 behaviours a fake upstream can reach: the
// edit-mode buttons toggle aria-pressed; the transcript survives an edit-mode rebuild and Close
// plus reopen; Close resets the mode to Ask before editing; @ insertion does not double the @;
// Copy writes the last answer (not the whole panel) to the clipboard.
// Geometry (task 296): at 1280 the aside is the right 450px — panel x about 830, its right
// edge on the viewport, the layout wider than 800, and FluentLayout has no mobile attribute.
// Closed, the layout is the viewport. Below 880 and when expanded, the panel covers the
// viewport. Shots are written beside task 296.
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
      ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
      // The host locale can be en-us@posix, which .NET WASM rejects and never renders the shell.
      Locale = "en-US",
      Permissions = ["clipboard-read", "clipboard-write"],
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
    await page.Locator("[data-qa=AskAiButton]").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });

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
      (await page.Locator("[data-qa=AskSuggestion]").CountAsync()).ShouldBeGreaterThanOrEqualTo(3);
      (await page.Locator("[data-qa=AskPrivacyNotice]").CountAsync()).ShouldBe(0);
      await page.Locator("[data-qa=AskEditMode]").WaitForAsync();
      await page.Locator("[data-qa=AskTagResource]").ClickAsync();
      await page.Locator("[data-qa=AskResourceEmpty]").WaitForAsync();
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
      await page.Locator("[data-qa=AskCopy]").WaitForAsync();
      await page.Locator("[data-qa=AskThumbsUp]").WaitForAsync();
      await page.Locator("[data-qa=AskThumbsDown]").WaitForAsync();
      await AssertIconButtonAsync(page, "AskCopy", "Copy");
      await AssertIconButtonAsync(page, "AskThumbsUp", "Thumbs up");
      await AssertIconButtonAsync(page, "AskThumbsDown", "Thumbs down");
      await page.Locator("[data-qa=AskSupport]").WaitForAsync();
      await AssertCopyWritesTheLastAnswerAsync(page);
      await AssertReferenceInsertReplacesTypedAtAsync(page);

      await PressedShouldBeAsync(page, "AskBeforeEditing", "true");
      await page.Locator("[data-qa=AskAutomaticallyEdit]").ClickAsync();
      await PressedShouldBeAsync(page, "AskAutomaticallyEdit", "true");
      await PressedShouldBeAsync(page, "AskBeforeEditing", "false");
      // The mode change rebuilds the agent; the turn must still be on screen afterwards.
      await page.Locator("[data-qa=AgentAsk][data-built-mode=AutomaticallyEdit]").WaitForAsync(new LocatorWaitForOptions
      {
        Timeout = 30_000,
      });
      await answer.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });

      await AssertDockedPanelAsync(page);

      // Close + reopen: the transcript is restored and the edit mode is back to Ask before editing.
      await page.Locator(".sc-ai-root").GetByText("page_context:").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
      await PressedShouldBeAsync(page, "AskBeforeEditing", "true");
      await PressedShouldBeAsync(page, "AskAutomaticallyEdit", "false");
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
      await page.Locator("[data-qa=AskEditMode]").WaitForAsync();
      await AssertIconButtonAsync(page, "AskNewConversation", "New conversation");
      await AssertIconButtonAsync(page, "AskExpand", "Expand");
      await AssertIconButtonAsync(page, "AgentAskClose", "Close");
      (await page.Locator("[data-qa=AskPrivacyNotice]").CountAsync()).ShouldBe(0);
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
    string folder = Directory.GetDirectories(Path.Combine(directory, "kanban"), "296-*", SearchOption.AllDirectories)
      .Single(path => File.Exists(Path.Combine(path, "task.md")));
    return Path.Combine(folder, fileName);
  }

  private static async Task AssertDockedPanelAsync(IPage page)
  {
    // The configured pass opens the tag menu before geometry. Close it so the state shots
    // show the docked panel, not the resource list.
    ILocator resourceMenu = page.Locator("[data-qa=AskResourceMenu]");
    if (await resourceMenu.CountAsync() > 0 && await resourceMenu.IsVisibleAsync())
    {
      await page.Locator("[data-qa=AskResourceClose]").ClickAsync();
      await resourceMenu.WaitForAsync(new LocatorWaitForOptions
      {
        State = WaitForSelectorState.Hidden,
        Timeout = 30_000,
      });
    }

    await page.SetViewportSizeAsync(1280, 800);
    await AssertIconButtonAsync(page, "AskNewConversation", "New conversation");
    await AssertIconButtonAsync(page, "AskExpand", "Expand");
    await AssertIconButtonAsync(page, "AgentAskClose", "Close");
    await AssertHeaderActionsSitBesideTheTitleAsync(page);
    await AssertSegmentsShareARowAsync(page);

    double width = page.ViewportSize!.Width;
    LocatorBoundingBoxResult panel = await WaitForBoxAsync(page, "[data-qa=AgentAsk]", box =>
      Math.Abs(box.X - (width - 450)) <= 8
      && Math.Abs(box.X + box.Width - width) <= 2
      && Math.Abs(box.Width - 450) <= 2);
    LocatorBoundingBoxResult layout = await BoxAsync(page, ".twe-shell__layout");
    LocatorBoundingBoxResult content = await BoxAsync(page, ".fluent-layout-item[area=content]");
    layout.Width.ShouldBeGreaterThan(800);
    content.Width.ShouldBeGreaterThan(400);
    (await page.Locator(".fluent-layout").GetAttributeAsync("mobile")).ShouldBeNull();
    ((double)panel.X).ShouldBe(width - 450, 8);
    ((double)(panel.X + panel.Width)).ShouldBe(width, 2);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("ask-docked-1280.png") });

    await page.Locator("[data-qa=AskExpand]").ClickAsync();
    await AssertIconButtonAsync(page, "AskExpand", "Collapse");
    double height = page.ViewportSize!.Height;
    LocatorBoundingBoxResult expanded = await WaitForBoxAsync(page, "[data-qa=AgentAsk]", box =>
      box.X <= 2
      && box.Y <= 2
      && Math.Abs(box.Width - width) <= 2
      && Math.Abs(box.Height - height) <= 2);
    ((double)expanded.Width).ShouldBe(width, 2);
    ((double)expanded.Height).ShouldBe(height, 2);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("ask-expanded-1280.png") });

    await page.Locator("[data-qa=AskExpand]").ClickAsync();
    await AssertIconButtonAsync(page, "AskExpand", "Expand");
    await WaitForBoxAsync(page, "[data-qa=AgentAsk]", box => Math.Abs(box.X - (width - 450)) <= 8);

    await page.SetViewportSizeAsync(800, 700);
    LocatorBoundingBoxResult narrow = await WaitForBoxAsync(page, "[data-qa=AgentAsk]", box =>
      box.X <= 2
      && box.Y <= 2
      && Math.Abs(box.Width - 800) <= 2
      && Math.Abs(box.Height - 700) <= 2);
    ((double)narrow.Width).ShouldBe(800, 2);
    ((double)narrow.Height).ShouldBe(700, 2);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("ask-narrow-800.png") });

    await page.SetViewportSizeAsync(1280, 800);
    await page.Locator("[data-qa=AgentAskClose]").ClickAsync();
    await page.Locator("[data-qa=AgentAsk]").WaitForAsync(new LocatorWaitForOptions
    {
      State = WaitForSelectorState.Hidden,
      Timeout = 30_000,
    });
    LocatorBoundingBoxResult closed = await WaitForBoxAsync(page, ".twe-shell__layout", box =>
      box.X <= 2 && Math.Abs(box.Width - page.ViewportSize!.Width) <= 2);
    ((double)closed.Width).ShouldBe(page.ViewportSize!.Width, 2);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("ask-closed-1280.png") });

    await page.Locator("[data-qa=AskAiButton]").ClickAsync();
    await page.Locator("[data-qa=AgentAsk]").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    await WaitForBoxAsync(page, "[data-qa=AgentAsk]", box =>
      Math.Abs(box.X - (page.ViewportSize!.Width - 450)) <= 8);
  }

  private static async Task AssertIconButtonAsync(IPage page, string qa, string accessibleName)
  {
    ILocator button = page.Locator($"[data-qa={qa}]");
    (await button.GetAttributeAsync("aria-label")).ShouldBe(accessibleName, qa);
    (await button.GetAttributeAsync("title")).ShouldBe(accessibleName, qa);
  }

  private static async Task AssertHeaderActionsSitBesideTheTitleAsync(IPage page)
  {
    LocatorBoundingBoxResult title = await BoxAsync(page, ".twe-agent-ask__title");
    LocatorBoundingBoxResult actions = await BoxAsync(page, ".twe-agent-ask__header-actions");
    actions.X.ShouldBeGreaterThan(title.X + title.Width);
  }

  private static async Task AssertSegmentsShareARowAsync(IPage page)
  {
    LocatorBoundingBoxResult askFirst = await BoxAsync(page, "[data-qa=AskBeforeEditing]");
    LocatorBoundingBoxResult automatic = await BoxAsync(page, "[data-qa=AskAutomaticallyEdit]");
    ((double)askFirst.Y).ShouldBe(automatic.Y, 4);
    automatic.X.ShouldBeGreaterThan(askFirst.X + askFirst.Width - 2);
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

    string detail = last is null
      ? "no box"
      : $"x={last.X} y={last.Y} width={last.Width} height={last.Height}";
    throw new TimeoutException($"{selector} did not reach the expected box ({detail})");
  }

  private static async Task PressedShouldBeAsync(IPage page, string qa, string expected)
  {
    ILocator button = page.Locator($"[data-qa={qa}]");
    for (int attempt = 0; attempt < 50; attempt++)
    {
      if (await button.GetAttributeAsync("aria-pressed") == expected)
      {
        return;
      }

      await Task.Delay(100);
    }

    (await button.GetAttributeAsync("aria-pressed")).ShouldBe(expected, qa);
  }

  private static async Task AssertCopyWritesTheLastAnswerAsync(IPage page)
  {
    await page.Locator("[data-qa=AskCopy]").ClickAsync();
    string copied = "";
    for (int attempt = 0; attempt < 50 && !copied.StartsWith("page_context:", StringComparison.Ordinal); attempt++)
    {
      copied = await page.EvaluateAsync<string>("() => navigator.clipboard.readText()");
      await Task.Delay(100);
    }

    copied.ShouldStartWith("page_context:");
    copied.ShouldNotContain("Thumbs up");
    copied.ShouldNotContain("What is on this page?");
  }

  private static async Task AssertReferenceInsertReplacesTypedAtAsync(IPage page)
  {
    const string script = """
      async () => {
        const area = document.querySelector(".twe-agent-ask .sc-ai-input__textarea");
        area.value = "see @";
        area.selectionStart = area.value.length;
        area.selectionEnd = area.value.length;
        const module = await import(new URL("./js/features/ask-ai.js", document.baseURI).href);
        module.InsertReference("@profile:ada");
        const replaced = area.value;
        area.value = "see ";
        area.selectionStart = area.value.length;
        area.selectionEnd = area.value.length;
        module.InsertReference("@profile:ada");
        const appended = area.value;
        area.value = "";
        area.dispatchEvent(new Event("input", { bubbles: true }));
        return replaced + "|" + appended;
      }
      """;
    string result = await page.EvaluateAsync<string>(script);
    result.ShouldBe("see @profile:ada|see @profile:ada");
  }

  private static async Task<LocatorBoundingBoxResult> BoxAsync(IPage page, string selector)
  {
    LocatorBoundingBoxResult? box = await page.Locator(selector).BoundingBoxAsync();
    box.ShouldNotBeNull(selector);
    return box;
  }
}
