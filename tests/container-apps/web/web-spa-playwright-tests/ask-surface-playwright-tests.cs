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
// signed-in choose-and-send flow is AskSignIn_Given_Wasm.
// Send (task 357): the library send button keeps its "Send message" name and click, its paper
// plane is hidden, and AskSendButton's arrow-up glyph sits over it. Empty, the button is muted
// (not the brand primary); once text is typed it is solid primary, and the click sends.
// Each pass opens its own context on the suite's shared browser (PlaywrightBrowserSession).
#endregion

namespace AskSurfacePlaywright_;

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
    // app-bar button opens Ask here; AskSignIn_Given_Wasm proves the Ctrl-K row signed in.
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

  private static string ScreenshotPath(string fileName) =>
    PlaywrightScreenshot.PathFor<AskSurface_Given_Wasm>(fileName);

  private static async Task AssertSendMutedThenSolidAsync(IPage page, ILocator input, string prompt)
  {
    const string sendSelector = ".twe-agent-ask .sc-ai-input__send";
    ILocator send = page.Locator(sendSelector);
    (await send.GetAttributeAsync("aria-label")).ShouldBe("Send message");
    (await send.Locator("svg").EvaluateAsync<string>("svg => getComputedStyle(svg).visibility")).ShouldBe("hidden");
    await page.Locator(".twe-agent-ask [data-qa=AskSendGlyph]").WaitForAsync();
    LocatorBoundingBoxResult sendBox = await BoxAsync(page, sendSelector);
    LocatorBoundingBoxResult glyphBox = await BoxAsync(page, ".twe-agent-ask [data-qa=AskSendGlyph]");
    Math.Abs(glyphBox.X - sendBox.X).ShouldBeLessThanOrEqualTo(1);
    Math.Abs(glyphBox.Y - sendBox.Y).ShouldBeLessThanOrEqualTo(1);
    (await input.InputValueAsync()).ShouldBeEmpty();

    // The background transitions, so wait for the end color rather than reading it once.
    const string isPrimary = """
      ([selector, wanted]) => {
        const send = document.querySelector(selector);
        const probe = document.createElement('span');
        probe.style.color = 'var(--twe-primary)';
        send.parentElement.appendChild(probe);
        const primary = getComputedStyle(probe).color;
        probe.remove();
        return (getComputedStyle(send).backgroundColor === primary) === wanted;
      }
      """;
    await page.WaitForFunctionAsync(isPrimary, new object[] { sendSelector, false });
    await page.Locator(".twe-agent-ask").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-send-muted.png"),
    });
    await input.FillAsync(prompt);
    await page.WaitForFunctionAsync(isPrimary, new object[] { sendSelector, true });
    (await send.IsEnabledAsync()).ShouldBeTrue();
    await page.Locator(".twe-agent-ask .sc-ai-input").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-send-solid.png"),
    });
  }

  private static async Task AssertComposerLayoutAsync(IPage page)
  {
    const string areaSelector = "textarea.sc-ai-input__textarea";
    const string composerSelector = ".twe-agent-ask .sc-ai-input";
    ILocator textarea = page.Locator(areaSelector);
    ILocator composer = page.Locator(composerSelector);
    string? placeholder = await textarea.GetAttributeAsync("placeholder");
    placeholder.ShouldNotBeNull();
    placeholder.ShouldContain("Type @ to reference a resource");
    (await page.Locator("[data-qa=AskTagResource]").CountAsync()).ShouldBe(0);

    LocatorBoundingBoxResult composerBox = await BoxAsync(page, composerSelector);
    LocatorBoundingBoxResult areaBox = await BoxAsync(page, areaSelector);
    LocatorBoundingBoxResult sendBox = await BoxAsync(page, ".twe-agent-ask .sc-ai-input__send");
    double inset = areaBox.X - composerBox.X;
    inset.ShouldBeGreaterThanOrEqualTo(4);
    inset.ShouldBeLessThanOrEqualTo(20);
    ((double)areaBox.Width).ShouldBeGreaterThanOrEqualTo(composerBox.Width * 0.9);
    ((double)sendBox.Y).ShouldBeGreaterThanOrEqualTo(areaBox.Y + areaBox.Height - 2);
    double sendInset = composerBox.X + composerBox.Width - (sendBox.X + sendBox.Width);
    sendInset.ShouldBeGreaterThanOrEqualTo(0);
    sendInset.ShouldBeLessThanOrEqualTo(24);
    // Task 346: Tag sits in the lead slot, on the send row, below the textarea.
    LocatorBoundingBoxResult tagBox = await BoxAsync(page, ".twe-agent-ask [data-qa=AskTag]");
    ((double)tagBox.Y).ShouldBeGreaterThanOrEqualTo(areaBox.Y + areaBox.Height - 2);
    ((double)tagBox.X).ShouldBeLessThan(sendBox.X);

    await composer.ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-composer-before.png"),
    });

    double before = areaBox.Height;
    await textarea.ClickAsync();
    await textarea.PressSequentiallyAsync("one");
    await textarea.PressAsync("Shift+Enter");
    await textarea.PressSequentiallyAsync("two");
    await textarea.PressAsync("Shift+Enter");
    await textarea.PressSequentiallyAsync("three");
    string draft = await textarea.InputValueAsync();
    draft.ShouldContain("one");
    draft.ShouldContain("\n");
    draft.ShouldContain("three");
    LocatorBoundingBoxResult grown = await WaitForBoxAsync(
      page,
      areaSelector,
      box => box.Height >= before + 8);
    ((double)grown.Height).ShouldBeGreaterThan(before);
    await composer.ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-composer-after.png"),
    });

    await textarea.PressAsync("Enter");
    await page.Locator(".sc-ai-message--user").GetByText("one").WaitForAsync(new LocatorWaitForOptions
    {
      Timeout = 30_000,
    });
    await page.Locator(".sc-ai-root").GetByText("page_context:").WaitForAsync(new LocatorWaitForOptions
    {
      Timeout = 60_000,
    });
    await WaitUntilInputEnabledAsync(page);
  }

  private static async Task AssertAtPickerWithNoTypesAsync(IPage page)
  {
    // The SPA is signed out here (the mock principal header authenticates API calls only), so
    // the picker opens with no types. AskSignIn_Given_Wasm proves the signed-in flow.
    ILocator textarea = page.Locator("textarea.sc-ai-input__textarea");
    await textarea.ClickAsync();
    await textarea.PressSequentiallyAsync("@");
    ILocator picker = page.Locator("[data-qa=AskReferencePicker]");
    await picker.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    await page.Locator("[data-qa=AskReferenceNoTypes]").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    (await page.EvaluateAsync<string?>("() => document.activeElement?.getAttribute('data-qa')"))
      .ShouldBe("AskReferenceInput");
    await page.Keyboard.PressAsync("Escape");
    await picker.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 30_000 });
    (await textarea.InputValueAsync()).ShouldBe("@");
    // Close renders the picker away first; FocusComposerAsync refocuses the textarea after, so wait for it.
    await page.WaitForFunctionAsync(
      "() => document.activeElement?.classList.contains('sc-ai-input__textarea') === true",
      null,
      new PageWaitForFunctionOptions { Timeout = 10_000 });

    // A mid-word @ (an email address) stays text: no picker, focus stays in the textarea.
    await textarea.FillAsync("");
    await textarea.PressSequentiallyAsync("ada@example.com");
    (await textarea.InputValueAsync()).ShouldBe("ada@example.com");
    (await picker.IsVisibleAsync()).ShouldBeFalse();
    await textarea.FillAsync("");
  }

  private static async Task WaitUntilInputEnabledAsync(IPage page)
  {
    ILocator textarea = page.Locator("textarea.sc-ai-input__textarea");
    for (int attempt = 0; attempt < 100; attempt++)
    {
      if (await textarea.GetAttributeAsync("disabled") is null)
      {
        return;
      }

      await Task.Delay(100);
    }

    throw new TimeoutException("Ask textarea stayed disabled after Enter");
  }

  private static async Task AssertDockedPanelAsync(IPage page)
  {
    // Close the @ picker if it is still open, so the state shots show the docked panel.
    ILocator picker = page.Locator("[data-qa=AskReferencePicker]");
    if (await picker.CountAsync() > 0 && await picker.IsVisibleAsync())
    {
      await page.Locator("[data-qa=AskReferenceClose]").ClickAsync();
      await picker.WaitForAsync(new LocatorWaitForOptions
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
    await AssertEditModePillBesideTagAsync(page);

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
    LocatorBoundingBoxResult? icon = await button.Locator("svg").First.BoundingBoxAsync();
    icon.ShouldNotBeNull(qa);
    ((double)icon.Width).ShouldBeGreaterThanOrEqualTo(16, qa);
    ((double)icon.Height).ShouldBeGreaterThanOrEqualTo(16, qa);
  }

  private static async Task AssertHeaderActionsSitBesideTheTitleAsync(IPage page)
  {
    LocatorBoundingBoxResult title = await BoxAsync(page, ".twe-agent-ask__title");
    LocatorBoundingBoxResult actions = await BoxAsync(page, ".twe-agent-ask__header-actions");
    actions.X.ShouldBeGreaterThan(title.X + title.Width);
  }

  private static async Task AssertEditModePillBesideTagAsync(IPage page)
  {
    LocatorBoundingBoxResult tag = await BoxAsync(page, ".twe-agent-ask [data-qa=AskTag]");
    LocatorBoundingBoxResult pill = await BoxAsync(page, ".twe-agent-ask [data-qa=AskEditMode]");
    ((double)pill.Y).ShouldBe(tag.Y, 4);
    pill.X.ShouldBeGreaterThan(tag.X + tag.Width - 2);
  }

  // Enter or Space opens, ArrowDown moves, Enter chooses; Esc closes without a change. Focus returns to the pill.
  private static async Task AssertEditModeKeyboardAsync(IPage page)
  {
    ILocator pill = page.Locator("[data-qa=AskEditMode]");
    (await pill.GetAttributeAsync("aria-haspopup")).ShouldNotBeNull();
    (await pill.GetAttributeAsync("aria-label")).ShouldBe("Edit mode: Ask before editing");

    await pill.FocusAsync();
    await page.Keyboard.PressAsync("Enter");
    await AttributeShouldBeAsync(pill, "aria-expanded", "true");
    await ActiveShouldBeAsync(page, "AskBeforeEditing");
    await page.Keyboard.PressAsync("ArrowDown");
    await ActiveShouldBeAsync(page, "AskAutomaticallyEdit");
    await page.Keyboard.PressAsync("Enter");
    await ModeShouldBeAsync(page, "AutomaticallyEdit");
    await AttributeShouldBeAsync(pill, "aria-expanded", "false");
    await ActiveShouldBeAsync(page, "AskEditMode");
    (await pill.GetAttributeAsync("aria-label")).ShouldBe("Edit mode: Automatically edit");

    await page.Keyboard.PressAsync("Enter");
    await AttributeShouldBeAsync(pill, "aria-expanded", "true");
    await page.Keyboard.PressAsync("Escape");
    await AttributeShouldBeAsync(pill, "aria-expanded", "false");
    await ActiveShouldBeAsync(page, "AskEditMode");
    await ModeShouldBeAsync(page, "AutomaticallyEdit");

    await page.Keyboard.PressAsync("Space");
    await AttributeShouldBeAsync(pill, "aria-expanded", "true");
    await page.Keyboard.PressAsync("Escape");
    await AttributeShouldBeAsync(pill, "aria-expanded", "false");
    await ActiveShouldBeAsync(page, "AskEditMode");

    await page.Keyboard.PressAsync("Enter");
    await AttributeShouldBeAsync(pill, "aria-expanded", "true");
    await page.Keyboard.PressAsync("Home");
    await ActiveShouldBeAsync(page, "AskBeforeEditing");
    await page.Keyboard.PressAsync("Enter");
    await ModeShouldBeAsync(page, "AskBeforeEditing");
    await page.Locator("[data-qa=AgentAsk][data-built-mode=AskBeforeEditing]").WaitForAsync(new LocatorWaitForOptions
    {
      Timeout = 30_000,
    });
  }

  private static async Task AssertSettingsMenuAsync(IPage page)
  {
    ILocator settings = page.Locator("[data-qa=AskSettings]");
    LocatorBoundingBoxResult button = await BoxAsync(page, ".twe-agent-ask [data-qa=AskSettings]");
    LocatorBoundingBoxResult send = await BoxAsync(page, ".twe-agent-ask .sc-ai-input__send");
    (button.X + button.Width).ShouldBeLessThanOrEqualTo(send.X + 1);
    ((double)(button.Y + (button.Height / 2))).ShouldBe(send.Y + (send.Height / 2), 6);
    (await settings.GetAttributeAsync("aria-haspopup")).ShouldNotBeNull();
    (await settings.GetAttributeAsync("aria-label")).ShouldBe("Akili settings");
    (await settings.GetAttributeAsync("data-status")).ShouldBe("Absent");

    await settings.FocusAsync();
    await page.Keyboard.PressAsync("Enter");
    await AttributeShouldBeAsync(settings, "aria-expanded", "true");
    ILocator status = page.Locator("[data-qa=AskAgentAccessStatus]");
    await status.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10_000 });
    (await status.InnerTextAsync()).ShouldContain("No agent credential");
    ILocator configure = page.Locator("[data-qa=AskConfigurePermissions]");
    (await configure.IsVisibleAsync()).ShouldBeTrue();
    await ShowCopyTooltipMustKeepMenuOpenAsync(page, "AskConfigurePermissions");
    await page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-settings-menu.png"),
    });
    await page.Keyboard.PressAsync("Escape");
    await AttributeShouldBeAsync(settings, "aria-expanded", "false");
    await ActiveShouldBeAsync(page, "AskSettings");

    await settings.ClickAsync();
    await configure.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10_000 });
    await configure.ClickAsync();
    await page.Locator("[data-qa=AgentAsk]").WaitForAsync(new LocatorWaitForOptions
    {
      State = WaitForSelectorState.Detached,
      Timeout = 30_000,
    });
    await page.WaitForURLAsync(url => url.Contains("AgentLinks", StringComparison.OrdinalIgnoreCase), new PageWaitForURLOptions
    {
      Timeout = 30_000,
    });
  }

  // Showing the Copy tooltip must leave the open menu item visible. The tooltip is a hint popover.
  private static async Task ShowCopyTooltipMustKeepMenuOpenAsync(IPage page, string menuItemQa)
  {
    string mode = await page.EvaluateAsync<string>(
      """
      (menuItemQa) => {
        const copy = document.querySelector("[data-qa=AskCopy]");
        const tooltip = [...document.querySelectorAll("fluent-tooltip")]
          .find((element) => element.getAttribute("anchor") === copy?.id);
        if (!(tooltip instanceof HTMLElement) || copy === null) return "missing";
        const item = document.querySelector(`[data-qa="${CSS.escape(menuItemQa)}"]`);
        const list = item?.closest("[popover]");
        if (!tooltip.matches(":popover-open")) tooltip.showPopover();
        const menuOpen = !!(list && list.matches(":popover-open"));
        if (tooltip.matches(":popover-open")) tooltip.hidePopover();
        const popover = tooltip.getAttribute("popover") ?? "";
        return menuOpen ? popover : popover + ":closed";
      }
      """,
      menuItemQa);
    mode.ShouldBe("hint");
    (await page.Locator($"[data-qa={menuItemQa}]").IsVisibleAsync()).ShouldBeTrue();
  }

  private static async Task ActiveShouldBeAsync(IPage page, string qa)
  {
    const string script = "() => document.activeElement?.getAttribute('data-qa')";
    string? active = null;
    for (int attempt = 0; attempt < 50; attempt++)
    {
      active = await page.EvaluateAsync<string?>(script);
      if (active == qa)
      {
        return;
      }

      await Task.Delay(100);
    }

    active.ShouldBe(qa);
  }

  private static async Task ModeShouldBeAsync(IPage page, string mode) =>
    await AttributeShouldBeAsync(page.Locator("[data-qa=AskEditMode]"), "data-mode", mode);

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

  private static async Task AttributeShouldBeAsync(ILocator element, string attribute, string expected)
  {
    for (int attempt = 0; attempt < 50; attempt++)
    {
      if (await element.GetAttributeAsync(attribute) == expected)
      {
        return;
      }

      await Task.Delay(100);
    }

    (await element.GetAttributeAsync(attribute)).ShouldBe(expected, attribute);
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

  private static async Task AssertCopyConversationWritesBothTurnsAsync(IPage page)
  {
    await page.EvaluateAsync("() => navigator.clipboard.writeText('')");
    await page.Locator("[data-qa=AskCopyConversation]").ClickAsync();
    string copied = "";
    for (int attempt = 0; attempt < 50 && !copied.StartsWith("You:", StringComparison.Ordinal); attempt++)
    {
      copied = await page.EvaluateAsync<string>("() => navigator.clipboard.readText()");
      await Task.Delay(100);
    }

    copied.ShouldStartWith("You:\none\ntwo\nthree\n\n");
    int second = copied.IndexOf("\n\nYou:\nWhat is on this page?\n\n", StringComparison.Ordinal);
    second.ShouldBeGreaterThan(0, copied);
    // Each turn has one "{AssistantName}:" paragraph whose answer starts with page_context:.
    (copied[..second].Split(":\npage_context:").Length - 1).ShouldBe(1, copied);
    (copied[second..].Split(":\npage_context:").Length - 1).ShouldBe(1, copied);
    copied.ShouldNotContain("Thumbs up");
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
        area.value = "see @ now";
        area.selectionStart = 5;
        area.selectionEnd = 5;
        module.InsertReference("@profile:ada");
        const beforeSpace = area.value;
        area.value = "";
        area.dispatchEvent(new Event("input", { bubbles: true }));
        return replaced + "|" + appended + "|" + beforeSpace;
      }
      """;
    string result = await page.EvaluateAsync<string>(script);
    // One space follows the token unless whitespace already does.
    result.ShouldBe("see @profile:ada |see @profile:ada |see @profile:ada now");
  }

  private static async Task<LocatorBoundingBoxResult> BoxAsync(IPage page, string selector)
  {
    LocatorBoundingBoxResult? box = await page.Locator(selector).BoundingBoxAsync();
    box.ShouldNotBeNull(selector);
    return box;
  }
}
