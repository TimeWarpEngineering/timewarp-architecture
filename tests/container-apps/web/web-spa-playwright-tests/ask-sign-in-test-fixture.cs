#region Purpose
// Shared fixture for the AskSignIn Playwright test classes: the signed-in/out Session, completion
// body parsers, and the Tag, @ picker and feedback assertions the scenarios reuse.
#endregion

namespace AskSignInPlaywright_;

using System.Text.Json;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features.Feedback;

internal static class AskSignInTestFixture
{
  internal const string ConfigurationPath = "/api/agent-chat/configuration";
  internal const string CompletionsPath = "/api/agent-chat/completions";

  internal const string AnyAskState =
    "[data-qa=AgentAskNotConfigured], [data-qa=AgentAskSignIn], [data-qa=AgentAskError], .sc-ai-input__textarea";

  internal sealed class Session : IAsyncDisposable
  {
    private readonly IBrowserContext Context;
    private readonly ConcurrentQueue<string> Console = new();
    private readonly ConcurrentQueue<int> Configuration = new();
    private readonly ConcurrentQueue<string> CompletionBodies = new();

    private Session(IBrowserContext context, IPage page)
    {
      Context = context;
      Page = page;
    }

    public IPage Page { get; }

    public List<int> ConfigurationStatuses => [.. Configuration];

    /// <summary>The JSON body of every completion request the browser sent, in order.</summary>
    public List<string> Completions => [.. CompletionBodies];

    public string Statuses => string.Join(", ", Configuration);

    public static async Task<Session> StartAsync()
    {
      IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
      IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
      {
        IgnoreHTTPSErrors = true,
        // The host locale can be en-us@posix, which .NET WASM rejects and never renders the shell.
        Locale = "en-US",
      });
      IPage page = await context.NewPageAsync();
      Session session = new(context, page);
      page.Console += (_, message) => session.Console.Enqueue($"{message.Type}: {message.Text}");
      page.PageError += (_, error) => session.Console.Enqueue(error);
      page.Request += (_, request) =>
      {
        if (request.Method == "POST"
          && new Uri(request.Url).AbsolutePath.Equals(CompletionsPath, StringComparison.OrdinalIgnoreCase)
          && request.PostData is { } body)
        {
          session.CompletionBodies.Enqueue(body);
        }
      };
      page.Response += (_, response) =>
      {
        if (new Uri(response.Url).AbsolutePath.Equals(ConfigurationPath, StringComparison.OrdinalIgnoreCase))
        {
          session.Configuration.Enqueue(response.Status);
        }
      };

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
      return session;
    }

    public async Task GoHomeAsync()
    {
      await Page.GotoAsync(InProcTestPorts.WebHostUrl, new PageGotoOptions
      {
        WaitUntil = WaitUntilState.DOMContentLoaded,
        Timeout = 120_000,
      });
      await Page.Locator(".twe-appbar").WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
    }

    /// <summary>
    /// Opens Ask. Signed in, through Ctrl-K (task 301): type "ask" and Enter on the derived
    /// AgentSurface.OpenAskPanel row; the palette must close and the docked panel open. Signed out the
    /// palette lists no commands (signed-in-only rule), so the app-bar Ask button opens it.
    /// </summary>
    public async Task<string> OpenAskAsync(bool signedIn)
    {
      if (signedIn)
      {
        // The palette listener attaches on the shell's first interactive render. Retry Ctrl-K.
        ILocator input = Page.Locator("[data-qa=CommandPaletteInput]");
        bool opened = false;
        for (int attempt = 0; attempt < 20 && !opened; attempt++)
        {
          await Page.Keyboard.PressAsync("Control+k");
          try
          {
            await input.WaitForAsync(new LocatorWaitForOptions { Timeout = 2_000, State = WaitForSelectorState.Visible });
            opened = true;
          }
          catch (TimeoutException)
          {
            await Page.Keyboard.PressAsync("Escape");
          }
        }

        opened.ShouldBeTrue("Ctrl-K did not open the palette");
        await input.FillAsync("ask");
        // With an empty query row 0 (a Page row) is already active, so wait on the filtered state: the
        // active row must be a Command whose name contains "ask" before the screenshot and Enter.
        ILocator active = Page.Locator(".twe-palette__row--active");
        await Assertions.Expect(active.Locator(".twe-palette__kind"))
          .ToHaveTextAsync("Command", new LocatorAssertionsToHaveTextOptions { Timeout = 10_000 });
        await Assertions.Expect(active.Locator(".twe-palette__name"))
          .ToContainTextAsync("ask", new LocatorAssertionsToContainTextOptions { IgnoreCase = true, Timeout = 10_000 });
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("ctrl-k-ask-row.png") });
        await input.PressAsync("Enter");
        await Page.Locator("[data-qa=CommandPalette]").WaitForAsync(new LocatorWaitForOptions
        {
          Timeout = 30_000,
          State = WaitForSelectorState.Hidden,
        });
        await Page.Locator("[data-qa=AgentAsk]").WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("ctrl-k-ask-panel-open.png") });
      }
      else
      {
        await Page.Locator("[data-qa=AskAiButton]").ClickAsync();
      }

      ILocator state = Page.Locator(AnyAskState).First;
      await state.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000, State = WaitForSelectorState.Visible });
      foreach (string qa in new[] { "AgentAskNotConfigured", "AgentAskSignIn", "AgentAskError" })
      {
        if (await Page.Locator($"[data-qa={qa}]").IsVisibleAsync())
        {
          return qa;
        }
      }

      return "Configured";
    }

    public async Task<string> AskTextAsync() =>
      (await Page.Locator("[data-qa=AgentAsk]").InnerTextAsync()).ReplaceLineEndings(" | ");

    public async Task SignInFromAskWithNewPasskeyAsync()
    {
      await Page.Locator("[data-qa=AgentAskSignInButton]").ClickAsync();
      await Page.WaitForURLAsync("**/Login**", new PageWaitForURLOptions { Timeout = 30_000 });
      await Page.Locator("[data-qa=AgentAsk]").WaitForAsync(new LocatorWaitForOptions
      {
        State = WaitForSelectorState.Hidden,
        Timeout = 10_000,
      });
      await CreatePasskeyAsync();
    }

    public async Task SignInWithNewPasskeyAsync()
    {
      await Page.Locator("[data-qa=HomeSignIn]").ClickAsync();
      await Page.WaitForURLAsync("**/Login**", new PageWaitForURLOptions { Timeout = 30_000 });
      await CreatePasskeyAsync();
    }

    private async Task CreatePasskeyAsync()
    {
      await Page.Locator("[data-qa=CreatePasskey]").ClickAsync();
      await Page.WaitForURLAsync("**/Settings", new PageWaitForURLOptions { Timeout = 60_000 });
      await Page.Locator(".twe-appbar").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    }

    /// <summary>
    /// Waits until a configuration probe answers after <paramref name="priorCount"/> recorded probes,
    /// so Ask is not opened while the store still holds the signed-out answer. Returns without
    /// throwing on timeout: the old code never re-probes, and the caller's assertions report that.
    /// </summary>
    public async Task WaitForConfigurationProbeAsync(int priorCount)
    {
      DateTime deadline = DateTime.UtcNow.AddSeconds(30);
      while (Configuration.Count <= priorCount && DateTime.UtcNow < deadline)
      {
        await Page.WaitForTimeoutAsync(100);
      }

      // The response event fires before the handler stores the result; let the dispatch finish.
      await Page.WaitForTimeoutAsync(250);
    }

    public void AssertNoWasmFailures()
    {
      string log = string.Join('\n', Console);
      log.ShouldNotContain("PlatformNotSupportedException");
      log.ShouldNotContain("SemaphoreSlim.Wait");
      log.ShouldNotContain("Unhandled exception rendering component");
    }

    // The browser is session-owned (PlaywrightBrowserSession); only this test's context is closed.
    public ValueTask DisposeAsync() => Context.DisposeAsync();
  }

  internal const string TaggedFeedbackTitle = "Dark theme broken";

  /// <summary>The text of the last user turn in a completion request body.</summary>
  internal static string LastUserText(string body)
  {
    using JsonDocument document = JsonDocument.Parse(body);
    string text = "";
    foreach (JsonElement turn in document.RootElement.GetProperty("messages").EnumerateArray())
    {
      if (string.Equals(turn.GetProperty("role").GetString(), "user", StringComparison.OrdinalIgnoreCase)
        && turn.TryGetProperty("text", out JsonElement turnText))
      {
        text = turnText.GetString() ?? "";
      }
    }

    return text;
  }

  internal static string InstructionsOf(string body)
  {
    using JsonDocument document = JsonDocument.Parse(body);
    return document.RootElement.TryGetProperty("instructions", out JsonElement instructions)
      ? instructions.GetString() ?? ""
      : "";
  }

  /// <summary>Tab cycles the candidates; returns every hint it showed.</summary>
  internal static async Task<List<string>> CycleHintsAsync(IPage page, int steps)
  {
    List<string> hints = [];
    for (int step = 0; step < steps; step++)
    {
      await page.Keyboard.PressAsync("Tab");
      hints.Add(await page.Locator("[data-qa=AskTagHint]").InnerTextAsync());
    }

    return hints;
  }

  /// <summary>Files one item, then reloads /Feedback: "Your filings" loads when the page opens.</summary>
  internal static async Task FileFeedbackAsync(IPage page, string feedbackUrl)
  {
    await page.Locator("[data-qa=FeedbackTitle]").Locator("input").FillAsync(TaggedFeedbackTitle);
    await page.Locator("[data-qa=FeedbackBody]").Locator("textarea").FillAsync("Text is unreadable in dark mode.");
    await page.Locator("[data-qa=FeedbackBody]").Locator("textarea").PressAsync("Tab");
    // Submit is disabled while any action runs (blurring Details dispatches one). Click once,
    // after it has stayed enabled for 500ms, as FeedbackAttachment_Given_Wasm does.
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

    await page.GotoAsync(feedbackUrl, new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    await page.Locator(".twe-appbar").WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
    await page.Locator("[data-qa=FeedbackList]").GetByText(TaggedFeedbackTitle).WaitForAsync(new LocatorWaitForOptions
    {
      Timeout = 60_000,
    });
  }

  /// <summary>Tag → Esc cancels; Tag → hover → click the filing row adds its chip. Returns the row's token.</summary>
  internal static async Task<string> AssertTagPicksTheFeedbackRowAsync(IPage page)
  {
    ILocator tag = page.Locator("[data-qa=AskTag]");
    ILocator picking = page.Locator("[data-qa=AskTagPicking]");
    ILocator outline = page.Locator("[data-qa=AskTagOutline]");
    ILocator chip = page.Locator("[data-qa=AskTagChip]");

    // Keyboard start: Enter on the focused Tag button; pick mode takes focus out of the panel, so
    // Tab drives the pick without a blur.
    await tag.FocusAsync();
    await page.Keyboard.PressAsync("Enter");
    await picking.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    await outline.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    // Privacy: marked elements the person cannot see are never candidates (private, aria-hidden).
    await page.EvaluateAsync(
      """
      () => {
        const body = document.querySelector('.twe-page__body');
        const add = (attributes, label) => {
          const element = document.createElement('div');
          element.setAttribute('data-ask-tag', 'Probe');
          element.setAttribute('data-ask-tag-label', label);
          for (const [name, value] of attributes) element.setAttribute(name, value);
          element.textContent = label;
          element.style.cssText = 'min-height:40px;min-width:40px';
          body.prepend(element);
        };
        add([['data-ask-tag-private', '']], 'PRIVATE-PROBE');
        add([['aria-hidden', 'true']], 'HIDDEN-PROBE');
      }
      """);
    List<string> hints = await CycleHintsAsync(page, 40);
    hints.ShouldNotContain(static hint => hint.Contains("PROBE", StringComparison.Ordinal));
    hints.ShouldContain(static hint => hint.Contains(AskTagMark.FeedbackRowKind, StringComparison.Ordinal));
    await page.Locator("text=PRIVATE-PROBE").HoverAsync(new LocatorHoverOptions { Force = true });
    await page.Locator("text=HIDDEN-PROBE").HoverAsync(new LocatorHoverOptions { Force = true });
    (await page.Locator("[data-qa=AskTagHint]").InnerTextAsync()).ShouldNotContain("PROBE");
    await page.EvaluateAsync("() => document.querySelectorAll('[data-ask-tag=Probe]').forEach(element => element.remove())");

    // Keyboard path: Tab to the filing row and Enter chooses it; Remove drops the chip again.
    string hint = "";
    for (int step = 0; step < 40 && !hint.StartsWith(AskTagMark.FeedbackRowKind, StringComparison.Ordinal); step++)
    {
      await page.Keyboard.PressAsync("Tab");
      hint = await page.Locator("[data-qa=AskTagHint]").InnerTextAsync();
    }

    hint.ShouldStartWith(AskTagMark.FeedbackRowKind + ": " + TaggedFeedbackTitle);
    await page.Keyboard.PressAsync("Enter");
    await picking.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 30_000 });
    await chip.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    await page.Locator("[data-qa=AskTagRemove]").ClickAsync();
    await chip.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached, Timeout = 30_000 });

    // Esc cancels with no chip, and focus inside the panel keeps Tab (Cancel is keyboard reachable).
    await tag.ClickAsync();
    await picking.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    await page.Locator("[data-qa=AskTagCancel]").FocusAsync();
    await page.Keyboard.PressAsync("Enter");
    await picking.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 30_000 });
    (await outline.CountAsync()).ShouldBe(0);
    (await chip.CountAsync()).ShouldBe(0);

    await tag.ClickAsync();
    // The status line renders only once ask-tag.ts has armed the pick (task 351), so it is the
    // readiness signal: the next key or click is caught.
    await picking.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    await page.Keyboard.PressAsync("Escape");
    await picking.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 30_000 });
    (await outline.CountAsync()).ShouldBe(0);
    (await chip.CountAsync()).ShouldBe(0);

    await tag.ClickAsync();
    await picking.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    ILocator row = page.Locator("[data-qa=FeedbackList]").GetByText(TaggedFeedbackTitle);
    await row.HoverAsync();
    await page.Locator(".twe-ask-tag-badge").GetByText(AskTagMark.FeedbackRowKind + ": " + TaggedFeedbackTitle)
      .WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("ask-tag-hover-row.png") });
    await row.ClickAsync();
    await picking.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 30_000 });
    page.Url.ShouldEndWith("/Feedback");

    await chip.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    (await page.Locator("[data-qa=AskTagChipText]").InnerTextAsync()).Trim()
      .ShouldBe(AskTagMark.FeedbackRowKind + ": " + TaggedFeedbackTitle);
    await page.Locator("[data-qa=AskTagChipText]").ClickAsync();
    string reference = (await page.Locator("[data-qa=AskTagChipReference]").InnerTextAsync()).Trim();
    reference.ShouldStartWith("@" + ListMyFeedback.ReferencePrefix + ":");
    await page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-tag-chip.png"),
    });
    return reference;
  }

  internal static async Task<string> AssertAtReferencePickerAsync(IPage page)
  {
    ILocator textarea = page.Locator("textarea.sc-ai-input__textarea");
    await textarea.ClickAsync();
    await textarea.PressSequentiallyAsync("@");
    ILocator picker = page.Locator("[data-qa=AskReferencePicker]");
    await picker.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    ILocator types = page.Locator("[data-qa=AskReferenceType]");
    await types.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    (await page.EvaluateAsync<string?>("() => document.activeElement?.getAttribute('data-qa')"))
      .ShouldBe("AskReferenceInput");
    await picker.ScreenshotAsync(new LocatorScreenshotOptions { Path = ScreenshotPath("ask-reference-types.png") });

    // Enter chooses the highlighted (first) type; Backspace on the empty term goes back.
    string? prefix = await types.First.GetAttributeAsync("data-prefix");
    prefix.ShouldNotBeNull();
    await page.Keyboard.PressAsync("Enter");
    await page.Locator("[data-qa=AskReferencePrefix]").GetByText("@" + prefix + ":").WaitForAsync();
    await page.Keyboard.PressAsync("Backspace");
    await types.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    await page.Keyboard.PressAsync("Enter");

    ILocator item = page.Locator("[data-qa=AskReferenceItem][aria-selected=true]");
    await item.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    string? token = await item.GetAttributeAsync("data-token");
    token.ShouldNotBeNull();
    token.ShouldStartWith("@" + prefix + ":");
    await picker.ScreenshotAsync(new LocatorScreenshotOptions { Path = ScreenshotPath("ask-reference-items.png") });
    await page.Keyboard.PressAsync("Enter");
    await picker.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 30_000 });

    // The token replaced the typed @, one space follows it, and focus is back in the textarea.
    string draft = await textarea.InputValueAsync();
    draft.ShouldBe(token + " ");
    (await page.EvaluateAsync<bool>(
      "() => document.activeElement?.classList.contains('sc-ai-input__textarea') === true")).ShouldBeTrue();
    return token;
  }

  internal static async Task AssertReasoningOmitsDictionaryAsync(Session session, List<string> failures, string pageName)
  {
    ILocator reasoning = session.Page.Locator("[data-qa=AskReasoning]").First;
    await reasoning.Locator("summary").ClickAsync();
    string text = await reasoning.InnerTextAsync();
    if (text.Contains("System.Collections.Generic", StringComparison.Ordinal))
    {
      failures.Add($"{pageName}: See reasoning printed a dictionary type name. Text: {text}");
    }

    if (await session.Page.Locator("[data-qa=AskReasoningArguments]").CountAsync() != 0)
    {
      failures.Add($"{pageName}: empty page_context arguments were shown.");
    }
  }

  internal static string ScreenshotPath(string fileName) =>
    PlaywrightScreenshot.PathFor<AskSignInReprobe_Given_Wasm>(fileName);
}
