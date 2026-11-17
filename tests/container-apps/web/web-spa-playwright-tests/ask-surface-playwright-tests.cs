#region Purpose
// Browser proof that Ask renders in WebAssembly, with and without an xAI key.
#endregion

#region Design
// WebApplicationHost layers this project's appsettings (UseMock, InteractiveWebAssembly,
// prerender off) and strips secrets.json. PostConfigure clears any machine XAI__ApiKey.
// The configured pass sets UseFakeUpstream so no request leaves the process. The mock
// principal header lets the relay's identity-session policy succeed. Opening Ctrl-K dispatches
// ApplicationState.SetActiveModal through StateTransactionBehavior, so a visible palette proves a
// store action ran in WASM. The SPA is signed out, so Ask opens from the app-bar button. Console text is
// checked for the TimeWarp.State SemaphoreSlim.Wait failure noted on 2026-10-09.
// The configured pass also proves the review-round-1 behaviours a fake upstream can reach: the
// transcript survives an edit-mode rebuild and Close plus reopen; Close resets the mode to Ask
// before editing; @ insertion does not double the @
// and adds one space unless whitespace already follows;
// Copy writes the last answer (not the whole panel) to the clipboard. Copy conversation (task 444)
// writes the two-turn thread in order: "You:" then the prompt, then "{AssistantName}:" then the
// answer, for each turn. The name is not pinned here because another suite may rename it.
// Geometry (task 296): at 1280 the aside is the right 450px — panel x about 830, its right
// edge on the viewport, the layout wider than 800, and FluentLayout has no mobile attribute.
// Closed, the layout is the viewport. Below 880 and when expanded, the panel covers the
// viewport. Shots go to test output (PlaywrightScreenshot).
// Composer (task 307): the inner textarea.sc-ai-input__textarea starts inside the
// .sc-ai-input padding (12px, --twe-space-3), its width is at least 90% of that box, and send
// sits below it. The Tag button (task 346) sits in the lead slot. Shift+Enter grows the field; Enter sends.
// Edit mode (task 355): the pill sits on Tag's row, right of it, and the old "Edit behavior" row is
// gone. The pill's data-mode is the state's mode. With the mouse, clicking the pill opens the radio
// menu and clicking Automatically edit switches the mode and closes it. With the keyboard, Enter
// or Space opens it (aria-expanded true, focus in the menu), the arrows move, Enter chooses, and Esc closes
// it without a change; focus returns to the pill. Showing the Copy tooltip while that menu is open
// leaves the menu open (task 371): the tooltip is a hint popover, and the test does not reopen a
// menu that closed. Not configured, there is no composer, so no pill.
// Settings (task 356): the sliders button sits on the footer row left of send and is a menu trigger.
// Enter opens it with the credential status first (disabled), Esc closes it and focus returns to
// the button. The same Copy tooltip leaves this menu open. The SPA is signed out here, so no
// credential is minted and the status is Absent.
// Choosing Configure permissions closes the panel and routes to Agent links (or to Login with
// Agent links as the return path, since the SPA is signed out).
// @ references (task 345): typing @ in the textarea opens the picker with focus in its input.
// The SPA itself is signed out here, so it lists no types; Esc closes it, keeps the typed @,
// and returns focus to the textarea. A mid-word @ (ada@example.com) opens nothing. The
// signed-in choose-and-send flow is AskSignInReprobe_Given_Wasm.
// Send (task 357): the library send button keeps its "Send message" name and click, its paper
// plane is hidden, and AskSendButton's arrow-up glyph sits over it. Empty, the button is muted
// (not the brand primary); once text is typed it is solid primary, and the click sends.
// Each pass opens its own context on the suite's shared browser (PlaywrightBrowserSession).
#endregion

namespace AskSurfacePlaywright_;

using static AskSurfacePlaywright_.AskSurfaceLayoutAsserts;
using static AskSurfacePlaywright_.AskSurfaceInteractionAsserts;

public class AskSurface_Given_Wasm
{
  private const string MockPrincipalId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskSurface_Given_Wasm>();

  public static async Task CtrlK_Should_ShowAsk_And_TheKeyState()
  {
    await RunAsync(useFakeUpstream: false);
    await RunAsync(useFakeUpstream: true);
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

    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
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
    // is already in the document. Retry Ctrl-K until the palette input is visible.
    ILocator paletteInput = page.Locator("[data-qa=CommandPaletteInput]");
    bool opened = false;
    for (int attempt = 0; attempt < 15 && !opened; attempt++)
    {
      await page.Keyboard.PressAsync("Control+k");
      try
      {
        await paletteInput.WaitForAsync(new LocatorWaitForOptions
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

    opened.ShouldBeTrue("Ctrl-K did not open the palette");
    (await page.Locator("[data-qa=CommandPaletteAsk]").CountAsync()).ShouldBe(0, "The footer Ask button was replaced by the derived row (task 301)");
    await page.ScreenshotAsync(new PageScreenshotOptions
    {
      Path = ScreenshotPath(useFakeUpstream ? "ctrl-k-palette-configured-page.png" : "ctrl-k-palette-page.png"),
    });
    await page.Keyboard.PressAsync("Escape");
    await page.Locator("[data-qa=CommandPalette]").WaitForAsync(new LocatorWaitForOptions
    {
      Timeout = 30_000,
      State = WaitForSelectorState.Hidden,
    });

    // Signed out, the palette lists no commands (task 301 kept the signed-in-only rule), so the
    // app-bar button opens Ask here; AskSignInReprobe_Given_Wasm proves the Ctrl-K row signed in.
    await page.Locator("[data-qa=AskAiButton]").ClickAsync();
    if (useFakeUpstream)
    {
      ILocator input = page.Locator(".sc-ai-input__textarea");
      await input.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
      (await page.Locator("[data-qa=AskStarterPrompt]").CountAsync()).ShouldBeGreaterThanOrEqualTo(3);
      (await page.Locator("[data-qa=AskPrivacyNotice]").CountAsync()).ShouldBe(0);
      await page.Locator("[data-qa=AskEditMode]").WaitForAsync();
      (await page.Locator(".twe-agent-ask__edit").CountAsync()).ShouldBe(0);
      (await page.GetByText("Edit behavior").CountAsync()).ShouldBe(0);
      await AssertComposerLayoutAsync(page);
      await AssertAtPickerWithNoTypesAsync(page);
      await AssertSendMutedThenSolidAsync(page, input, "What is on this page?");
      await page.Locator(".sc-ai-input__send").ClickAsync();
      // Enter already produced one answer. The click send is the second turn.
      ILocator answer = page.Locator(".sc-ai-root").GetByText("page_context:").Nth(1);
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
      await AssertIconButtonAsync(page, "AskCopyConversation", "Copy conversation");
      await AssertIconButtonAsync(page, "AskThumbsUp", "Thumbs up");
      await AssertIconButtonAsync(page, "AskThumbsDown", "Thumbs down");
      await page.Locator("[data-qa=AskSupport]").WaitForAsync();
      await AssertCopyWritesTheLastAnswerAsync(page);
      await AssertCopyConversationWritesBothTurnsAsync(page);
      await AssertReferenceInsertReplacesTypedAtAsync(page);

      await ModeShouldBeAsync(page, "AskBeforeEditing");
      await page.Locator("[data-qa=AskEditMode]").ClickAsync();
      ILocator autoEdit = page.Locator("[data-qa=AskAutomaticallyEdit]");
      await autoEdit.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10_000 });
      (await page.Locator("[data-qa=AskBeforeEditing]").EvaluateAsync<bool>("item => item.checked")).ShouldBeTrue();
      (await autoEdit.EvaluateAsync<bool>("item => item.checked")).ShouldBeFalse();
      await ShowCopyTooltipMustKeepMenuOpenAsync(page, "AskAutomaticallyEdit");
      await page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
      {
        Path = ScreenshotPath("ask-edit-mode-menu.png"),
      });
      await autoEdit.ClickAsync();
      await ModeShouldBeAsync(page, "AutomaticallyEdit");
      await autoEdit.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 10_000 });
      // The mode change rebuilds the agent; the turn must still be on screen afterwards.
      await page.Locator("[data-qa=AgentAsk][data-built-mode=AutomaticallyEdit]").WaitForAsync(new LocatorWaitForOptions
      {
        Timeout = 30_000,
      });
      await answer.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });

      await AssertDockedPanelAsync(page);

      // Close + reopen: the transcript is restored and the edit mode is back to Ask before editing.
      await page.Locator(".sc-ai-root").GetByText("page_context:").Nth(1).WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
      await ModeShouldBeAsync(page, "AskBeforeEditing");
      await AssertEditModeKeyboardAsync(page);
      await page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
      {
        Path = ScreenshotPath("ctrl-k-ask-result.png"),
      });
      await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("ctrl-k-ask-result-page.png") });
      // Last: Configure permissions navigates away and closes the panel.
      await AssertSettingsMenuAsync(page);
    }
    else
    {
      ILocator missing = page.Locator("[data-qa=AgentAskNotConfigured]");
      await missing.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
      (await missing.InnerTextAsync()).ShouldBe("AI not configured");
      string command = await page.Locator("[data-qa=AgentAskSetupCommand]").InnerTextAsync();
      command.ShouldBe(XaiChatDefaults.SetupCommand);
      (await page.Locator("[data-qa=AskEditMode]").CountAsync()).ShouldBe(0);
      (await page.Locator("[data-qa=AskSettings]").CountAsync()).ShouldBe(0);
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
}
