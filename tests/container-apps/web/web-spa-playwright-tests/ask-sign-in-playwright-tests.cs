#region Purpose
// Browser proof for task 293: Ask never reports a 401 as "AI not configured", and it re-reads its
// readiness when the user signs in inside the SPA.
#endregion

#region Design
// Real InteractiveWebAssembly against Web.Server with the real identity-session cookie. No mock
// principal header is sent, so the configuration endpoint authenticates exactly the way it does on
// a developer machine. Sign-in stays in the SPA. The first test signs in from Ask's own "Sign in"
// button, which closes the panel and routes to /Login. The second uses the Home "Sign in" button,
// the path Steven took on 2026-10-09. Both then Create account with a passkey.
// A Chrome DevTools virtual authenticator answers the WebAuthn ceremony. The app then navigates to
// /Settings without a page load, which is why a once-per-app probe never ran again.
// The signed-out and signed-in checks are collected and asserted together so a failing run on the
// old code reports both bugs: the masked 401 and the stale readiness after sign-in.
// Every GET api/agent-chat/configuration status is recorded so the test proves the probe re-ran and
// what the server answered. After sign-in the test waits for that re-probe's response before
// opening Ask, so it does not read the store while it still holds the signed-out answer. The configured pass uses the fake upstream (no request leaves the
// process); the no-key pass has no XAI:ApiKey. Screenshots go to test output (PlaywrightScreenshot),
// which CI uploads.
// Task 345: signed in (the first passkey account is Administrator), Settings' Ask types @ in the
// composer: Enter chooses the first permitted type, Backspace returns to the types, Enter again,
// and Enter on the highlighted item inserts its @prefix:id token in place of the @. The list
// query is the real in-memory API as that user. The sent message carries the token and the fake
// upstream's page_context answer lists it under references.
// Task 346: on /Feedback the account files one item, so "Your filings" has a row. Ask's Tag
// turns on pick mode; Esc cancels it with no chip. Tag again, hovering the row outlines it with
// its chip text, and clicking it picks it (the click does not follow the row's link). The chip
// shows "Feedback row: <title>" and its @my-feedback:<id> reference; the message sent with it
// moves it to page_context.tags, which the fake upstream's answer prints. The browser's own
// completion request is recorded: the user message must carry AskTagPayload's heading and the
// token, and the next turn's instructions must name the sent tag. A probe element marked
// data-ask-tag-private or aria-hidden is injected into the page body and must never be a Tab or
// hover candidate. Tab reaches the row and Enter chooses it; Enter on the panel's Cancel button
// (focus in the panel) cancels, because the panel keeps its native keys.
#endregion

namespace AskSignInPlaywright_;

using System.Text.Json;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Architecture.Features.Feedback;

public class AskSignIn_Given_Wasm
{
  private const string ConfigurationPath = "/api/agent-chat/configuration";
  private const string CompletionsPath = "/api/agent-chat/completions";

  private const string AnyAskState =
    "[data-qa=AgentAskNotConfigured], [data-qa=AgentAskSignIn], [data-qa=AgentAskError], .sc-ai-input__textarea";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskSignIn_Given_Wasm>();

  public static async Task SignIn_Should_ReprobeAsk_And_NeverShowA401AsNotConfigured()
  {
    InstallChromium();
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
      services.PostConfigure<XaiChatOptions>(options =>
      {
        options.UseFakeUpstream = true;
        options.ApiKey = "playwright-placeholder-key";
      }));

    await using Session session = await Session.StartAsync();
    List<string> failures = [];

    // Signed out. The server must answer 401 and Ask must say so, not "AI not configured".
    await session.GoHomeAsync();
    string signedOut = await session.OpenAskAsync();
    await session.Page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-signed-out.png"),
    });
    int signedOutProbes = session.ConfigurationStatuses.Count;
    if (!session.ConfigurationStatuses.Contains(401))
    {
      failures.Add($"Signed out: expected GET {ConfigurationPath} to return 401. Saw [{session.Statuses}].");
    }

    if (signedOut != "AgentAskSignIn")
    {
      failures.Add($"Signed out: the server answered 401 but Ask showed {signedOut} ('{await session.AskTextAsync()}').");
    }

    // Sign in inside the SPA: Ask "Sign in" closes the panel -> /Login -> Create account (virtual passkey) -> /Settings.
    if (signedOut == "AgentAskSignIn")
    {
      await session.SignInFromAskWithNewPasskeyAsync();
    }
    else
    {
      await session.SignInWithNewPasskeyAsync();
    }

    await session.WaitForConfigurationProbeAsync(signedOutProbes);
    string signedIn = await session.OpenAskAsync();
    await session.Page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath(signedIn == "Configured" ? "ask-signed-in-configured.png" : "ask-signed-in-stale.png"),
    });
    List<int> afterSignIn = [.. session.ConfigurationStatuses.Skip(signedOutProbes)];
    if (!afterSignIn.Contains(200))
    {
      failures.Add(
        $"Signed in: GET {ConfigurationPath} was not re-run with a 200 after sign-in. "
        + $"All statuses: [{session.Statuses}], after sign-in: [{string.Join(", ", afterSignIn)}].");
    }

    if (signedIn != "Configured")
    {
      failures.Add($"Signed in with XAI:ApiKey set: Ask showed {signedIn} ('{await session.AskTextAsync()}').");
    }
    else
    {
      // Task 345: @ -> types -> search -> choose, then send; page_context resolves the token.
      ILocator input = session.Page.Locator(".sc-ai-input__textarea");
      string token = await AssertAtReferencePickerAsync(session.Page);
      await input.PressSequentiallyAsync(" What is on this page?");
      await session.Page.Locator(".sc-ai-input__send").ClickAsync();
      await session.Page.Locator(".sc-ai-message--user").GetByText(token).WaitForAsync(new LocatorWaitForOptions
      {
        Timeout = 30_000,
      });
      ILocator answer = session.Page.Locator(".sc-ai-root").GetByText("page_context:");
      await answer.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
      await session.Page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
      {
        Path = ScreenshotPath("ask-signed-in-configured-answer.png"),
      });
      string settingsAnswer = await session.Page.Locator(".sc-ai-root").InnerTextAsync();
      if (settingsAnswer.Contains("System.Collections.Generic", StringComparison.Ordinal))
      {
        failures.Add("Settings: the answer rendered a dictionary type name.");
      }

      if (!settingsAnswer.Contains("/Settings", StringComparison.Ordinal))
      {
        failures.Add($"Settings: page_context did not report /Settings. Answer: {settingsAnswer}");
      }

      if (!settingsAnswer.Contains("\"references\"", StringComparison.Ordinal)
        || !settingsAnswer.Contains("\"token\":\"" + token + "\"", StringComparison.Ordinal))
      {
        failures.Add($"Settings: page_context.references did not resolve {token}. Answer: {settingsAnswer}");
      }

      await AssertReasoningOmitsDictionaryAsync(session, failures, "Settings");

      string feedbackUrl = InProcTestPorts.WebHostUrl.TrimEnd('/') + "/Feedback";
      await session.Page.GotoAsync(feedbackUrl, new PageGotoOptions
      {
        WaitUntil = WaitUntilState.DOMContentLoaded,
        Timeout = 120_000,
      });
      await session.Page.Locator(".twe-appbar").WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
      await session.Page.Locator("[data-qa=FeedbackBody]").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
      await FileFeedbackAsync(session.Page, feedbackUrl);
      string feedbackState = await session.OpenAskAsync();
      if (feedbackState != "Configured")
      {
        failures.Add($"Feedback: Ask showed {feedbackState} ('{await session.AskTextAsync()}').");
      }
      else
      {
        string tagReference = await AssertTagPicksTheFeedbackRowAsync(session.Page);
        ILocator feedbackInput = session.Page.Locator(".sc-ai-input__textarea");
        await feedbackInput.FillAsync("Explain this page");
        int completionsBefore = session.Completions.Count;
        await session.Page.Locator(".sc-ai-input__send").ClickAsync();
        ILocator feedbackAnswer = session.Page.Locator(".sc-ai-root").GetByText("page_context:");
        await feedbackAnswer.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
        // The outgoing user message itself carries the payload, not just the live page_context.
        string firstUserText = LastUserText(session.Completions[completionsBefore]);
        if (!firstUserText.Contains(AskTagPayload.MessageHeading, StringComparison.Ordinal)
          || !firstUserText.Contains(tagReference, StringComparison.Ordinal))
        {
          failures.Add($"Feedback: the outgoing user message did not carry the tag payload and {tagReference}. Text: {firstUserText}");
        }

        string feedbackText = await session.Page.Locator(".sc-ai-root").InnerTextAsync();
        await session.Page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
        {
          Path = ScreenshotPath("ask-feedback-page-context.png"),
        });
        if (feedbackText.Contains("System.Collections.Generic", StringComparison.Ordinal))
        {
          failures.Add("Feedback: the answer rendered a dictionary type name.");
        }

        if (!feedbackText.Contains("/Feedback", StringComparison.Ordinal)
          || !feedbackText.Contains("\"page\":\"Feedback\"", StringComparison.Ordinal))
        {
          failures.Add($"Feedback: page_context did not describe /Feedback. Answer: {feedbackText}");
        }

        if (!feedbackText.Contains("\"tags\"", StringComparison.Ordinal)
          || !feedbackText.Contains("\"kind\":\"Feedback row\"", StringComparison.Ordinal)
          || !feedbackText.Contains("\"label\":\"" + TaggedFeedbackTitle + "\"", StringComparison.Ordinal)
          || !feedbackText.Contains("\"token\":\"" + tagReference + "\"", StringComparison.Ordinal))
        {
          failures.Add($"Feedback: page_context.tags did not carry the picked row {tagReference}. Answer: {feedbackText}");
        }

        if (await session.Page.Locator("[data-qa=AskTagChip]").CountAsync() != 0)
        {
          failures.Add("Feedback: the tag chip stayed in the composer after the message was sent.");
        }

        // A later turn: the chip is not sent again, but the request's instructions still name it.
        int secondTurn = session.Completions.Count;
        await feedbackInput.FillAsync("And again?");
        await session.Page.Locator(".sc-ai-input__send").ClickAsync();
        await session.Page.Locator(".sc-ai-root").GetByText("And again?").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
        for (int wait = 0; wait < 100 && session.Completions.Count <= secondTurn; wait++)
        {
          await Task.Delay(100);
        }

        if (session.Completions.Count <= secondTurn)
        {
          failures.Add("Feedback: the second message sent no completion request.");
        }
        else
        {
          string secondBody = session.Completions[secondTurn];
          if (LastUserText(secondBody).Contains(AskTagPayload.MessageHeading, StringComparison.Ordinal))
          {
            failures.Add("Feedback: the second message sent the tag payload again.");
          }

          if (!InstructionsOf(secondBody).Contains(tagReference, StringComparison.Ordinal))
          {
            failures.Add($"Feedback: the second turn's instructions did not carry the sent tag {tagReference}.");
          }
        }

        await AssertReasoningOmitsDictionaryAsync(session, failures, "Feedback");
      }
    }

    Console.WriteLine($"task-293 configuration statuses: [{session.Statuses}]; signed out: {signedOut}; signed in: {signedIn}");
    failures.ShouldBeEmpty(string.Join(Environment.NewLine, failures));
    session.AssertNoWasmFailures();
  }

  public static async Task SignedIn_WithoutKey_Should_ShowTheSetupCommand()
  {
    InstallChromium();
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
      services.PostConfigure<XaiChatOptions>(options =>
      {
        options.UseFakeUpstream = false;
        options.ApiKey = null;
      }));

    await using Session session = await Session.StartAsync();
    await session.GoHomeAsync();
    await session.WaitForConfigurationProbeAsync(0);
    int signedOutProbes = session.ConfigurationStatuses.Count;
    await session.SignInWithNewPasskeyAsync();
    await session.WaitForConfigurationProbeAsync(signedOutProbes);
    string state = await session.OpenAskAsync();
    await session.Page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-signed-in-no-key.png"),
    });

    state.ShouldBe("AgentAskNotConfigured", $"Ask text: {await session.AskTextAsync()}; statuses [{session.Statuses}]");
    session.ConfigurationStatuses.ShouldContain(200);
    (await session.Page.Locator("[data-qa=AgentAskNotConfigured]").InnerTextAsync()).ShouldBe("AI not configured");
    (await session.Page.Locator("[data-qa=AgentAskSetupCommand]").InnerTextAsync()).ShouldBe(XaiChatDefaults.SetupCommand);
    session.AssertNoWasmFailures();
  }

  private sealed class Session : IAsyncDisposable
  {
    private readonly IPlaywright Playwright;
    private readonly IBrowser Browser;
    private readonly IBrowserContext Context;
    private readonly ConcurrentQueue<string> Console = new();
    private readonly ConcurrentQueue<int> Configuration = new();
    private readonly ConcurrentQueue<string> CompletionBodies = new();

    private Session(IPlaywright playwright, IBrowser browser, IBrowserContext context, IPage page)
    {
      Playwright = playwright;
      Browser = browser;
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
      IPlaywright playwright = await Microsoft.Playwright.Playwright.CreateAsync();
      IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
      IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
      {
        IgnoreHTTPSErrors = true,
        // The host locale can be en-us@posix, which .NET WASM rejects and never renders the shell.
        Locale = "en-US",
      });
      IPage page = await context.NewPageAsync();
      Session session = new(playwright, browser, context, page);
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

    public async Task<string> OpenAskAsync()
    {
      // The palette listener attaches on the shell's first interactive render. Retry Ctrl-K.
      ILocator ask = Page.Locator("[data-qa=CommandPaletteAsk]");
      bool opened = false;
      for (int attempt = 0; attempt < 20 && !opened; attempt++)
      {
        await Page.Keyboard.PressAsync("Control+k");
        try
        {
          await ask.WaitForAsync(new LocatorWaitForOptions { Timeout = 2_000, State = WaitForSelectorState.Visible });
          opened = true;
        }
        catch (TimeoutException)
        {
          await Page.Keyboard.PressAsync("Escape");
        }
      }

      opened.ShouldBeTrue("Ctrl-K did not show Ask");
      await ask.ClickAsync();
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

    public async ValueTask DisposeAsync()
    {
      await Context.DisposeAsync();
      await Browser.DisposeAsync();
      Playwright.Dispose();
    }
  }

  private const string TaggedFeedbackTitle = "Dark theme broken";

  /// <summary>The text of the last user turn in a completion request body.</summary>
  private static string LastUserText(string body)
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

  private static string InstructionsOf(string body)
  {
    using JsonDocument document = JsonDocument.Parse(body);
    return document.RootElement.TryGetProperty("instructions", out JsonElement instructions)
      ? instructions.GetString() ?? ""
      : "";
  }

  /// <summary>Tab cycles the candidates (focus on the page, not in the panel); returns every hint it showed.</summary>
  private static async Task<List<string>> CycleHintsAsync(IPage page, int steps)
  {
    List<string> hints = [];
    await page.EvaluateAsync("() => document.activeElement instanceof HTMLElement && document.activeElement.blur()");
    for (int step = 0; step < steps; step++)
    {
      await page.Keyboard.PressAsync("Tab");
      hints.Add(await page.Locator("[data-qa=AskTagHint]").InnerTextAsync());
    }

    return hints;
  }

  /// <summary>Files one item, then reloads /Feedback: "Your filings" loads when the page opens.</summary>
  private static async Task FileFeedbackAsync(IPage page, string feedbackUrl)
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
  private static async Task<string> AssertTagPicksTheFeedbackRowAsync(IPage page)
  {
    ILocator tag = page.Locator("[data-qa=AskTag]");
    ILocator picking = page.Locator("[data-qa=AskTagPicking]");
    ILocator outline = page.Locator("[data-qa=AskTagOutline]");
    ILocator chip = page.Locator("[data-qa=AskTagChip]");

    await tag.ClickAsync();
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

  private static async Task<string> AssertAtReferencePickerAsync(IPage page)
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

  private static async Task AssertReasoningOmitsDictionaryAsync(Session session, List<string> failures, string pageName)
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

  private static string ScreenshotPath(string fileName) =>
    PlaywrightScreenshot.PathFor<AskSignIn_Given_Wasm>(fileName);
}
