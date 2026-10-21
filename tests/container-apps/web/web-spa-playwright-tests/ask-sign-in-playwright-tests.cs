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
// Task 356: signed in, Ask's settings button reports the minted credential as Active and the menu
// shows its expiry.
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
// (focus in the panel) cancels, because the panel keeps its native keys. Task 351: the "picking"
// status line appears only once the browser has armed the pick, so every step that waits for it
// may press a key or click at once.
// Task 301: signed in, Ask opens through Ctrl-K: "ask" highlights the derived OpenAskPanel row,
// Enter closes the palette and opens [data-qa=AgentAsk] (screenshots ctrl-k-ask-row.png and
// ctrl-k-ask-panel-open.png). Signed out the palette has no commands, so the app-bar button opens Ask.
// Task 361: signed in as the first (Administrator) account, /Admin/Authentication renames the
// assistant to Zawadi. Without a reload the top-bar button reads "Ask Zawadi", the derived Ctrl-K
// row reads "Ask Zawadi", and the panel it opens has the header Zawadi with its avatar and the
// greeting "Hi, I'm Zawadi"; no "Akili" is left in the panel (assistant-rename-*.png).
// Task 300: signed in, back on Home, Ask answers "Where can I add a passkey?" through site_map:
// the fake upstream links the first site map page whose title or description names a passkey,
// which is Settings, and the request carried site_map, navigate and a site map naming /Settings
// in its instructions. "Take me there" calls navigate; in Ask before editing the approval card
// shows navigate and /Settings and the page stays on Home until Approve, then the app lands on
// /Settings without a reload (ask-home-where-passkey*.png, ask-navigate-approval.png,
// ask-navigate-landed-settings.png).
// Task 381: Ask opened from the Ctrl-K row leaves the caret in the composer (the palette's own focus
// return does not land on top of it), so the question is typed with the keyboard and no click. While
// the navigate approval card waits, the composer does not take focus; the check waits a second
// first so a late focus move would be seen. Ask opened from the palette that the search field
// opened returns focus to the search field on close without reopening the palette.
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
    string signedOut = await session.OpenAskAsync(signedIn: false);
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
    string signedIn = await session.OpenAskAsync(signedIn: true);
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
      // Task 356: signed in, the panel minted a credential, so settings reports it active with an expiry.
      ILocator askSettings = session.Page.Locator("[data-qa=AskSettings]");
      string? credentialStatus = await askSettings.GetAttributeAsync("data-status");
      if (credentialStatus != "Active")
      {
        failures.Add($"Signed in: Ask settings showed credential status '{credentialStatus}', not Active.");
      }
      else
      {
        await askSettings.FocusAsync();
        await session.Page.Keyboard.PressAsync("Enter");
        ILocator expiry = session.Page.Locator("[data-qa=AskAgentAccessExpiry]");
        await expiry.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10_000 });
        (await session.Page.Locator("[data-qa=AskAgentAccessStatus]").InnerTextAsync()).ShouldContain("Agent access active");

        // The menu content shows before fluent-menu syncs aria-expanded on the trigger; an Escape sent in that
        // gap is ignored and the menu then settles open. A person cannot beat it, so wait for the synced state.
        await Assertions.Expect(askSettings).ToHaveAttributeAsync("aria-expanded", "true");
        await session.Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(askSettings).ToHaveAttributeAsync("aria-expanded", "false");
        await expiry.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 10_000 });
      }

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
      string feedbackState = await session.OpenAskAsync(signedIn: true);
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
    string state = await session.OpenAskAsync(signedIn: true);
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

  public static async Task AdminRename_Should_Relabel_Every_Ask_Entry_Point()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
      services.PostConfigure<XaiChatOptions>(options =>
      {
        options.UseFakeUpstream = true;
        options.ApiKey = "playwright-placeholder-key";
      }));

    await using Session session = await Session.StartAsync();
    IPage page = session.Page;
    await session.GoHomeAsync();
    ILocator askButton = page.Locator("[data-qa=AskAiButton]");
    await Assertions.Expect(askButton).ToHaveTextAsync("Ask Akili", new LocatorAssertionsToHaveTextOptions { Timeout = 30_000 });
    await session.SignInWithNewPasskeyAsync();

    // Rename through the admin form (same UpdateSiteSettings flow and Version token).
    await page.GotoAsync(InProcTestPorts.WebHostUrl.TrimEnd('/') + "/Admin/Authentication", new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    ILocator nameInput = page.Locator("[data-qa=AssistantName]").Locator("input");
    await nameInput.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
    await Assertions.Expect(nameInput).ToHaveValueAsync("Akili", new LocatorAssertionsToHaveValueOptions { Timeout = 30_000 });
    await nameInput.FillAsync("Zawadi");
    await nameInput.PressAsync("Tab");
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("assistant-rename-admin.png") });
    await page.Locator("[data-qa=AuthenticationSave]").ClickAsync();

    // Top bar: the page reloads the public identity after a successful save, so no page load.
    await Assertions.Expect(askButton).ToHaveTextAsync("Ask Zawadi", new LocatorAssertionsToHaveTextOptions { Timeout = 30_000 });

    // Ctrl-K: the derived AgentSurface.OpenAskPanel row carries the runtime override.
    ILocator paletteInput = page.Locator("[data-qa=CommandPaletteInput]");
    bool opened = false;
    for (int attempt = 0; attempt < 20 && !opened; attempt++)
    {
      await page.Keyboard.PressAsync("Control+k");
      try
      {
        await paletteInput.WaitForAsync(new LocatorWaitForOptions { Timeout = 2_000, State = WaitForSelectorState.Visible });
        opened = true;
      }
      catch (TimeoutException)
      {
        await page.Keyboard.PressAsync("Escape");
      }
    }

    opened.ShouldBeTrue("Ctrl-K did not open the palette");
    await paletteInput.FillAsync("zawadi");
    ILocator active = page.Locator(".twe-palette__row--active");
    await Assertions.Expect(active.Locator(".twe-palette__name"))
      .ToHaveTextAsync("Ask Zawadi", new LocatorAssertionsToHaveTextOptions { Timeout = 10_000 });
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("assistant-rename-ctrl-k.png") });
    await paletteInput.PressAsync("Enter");

    // Panel: header name and avatar, and the empty-state greeting once the agent is built.
    ILocator panel = page.Locator("[data-qa=AgentAsk]");
    await panel.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
    await Assertions.Expect(page.Locator("[data-qa=AskTitle]"))
      .ToHaveTextAsync("Zawadi", new LocatorAssertionsToHaveTextOptions { Timeout = 30_000 });
    await Assertions.Expect(page.Locator("[data-qa=AskAvatar]"))
      .ToHaveAttributeAsync("alt", "Zawadi", new LocatorAssertionsToHaveAttributeOptions { Timeout = 30_000 });
    await Assertions.Expect(page.Locator("[data-qa=AskGreeting]"))
      .ToContainTextAsync("Hi, I'm Zawadi", new LocatorAssertionsToContainTextOptions { Timeout = 60_000 });
    (await page.Locator("[data-qa=AskEmptyAvatar]").GetAttributeAsync("alt")).ShouldBe("Zawadi");
    await panel.ScreenshotAsync(new LocatorScreenshotOptions { Path = ScreenshotPath("assistant-rename-panel.png") });
    (await panel.InnerTextAsync()).ShouldNotContain("Akili");
    session.AssertNoWasmFailures();
  }

  public static async Task Home_Should_Point_Passkeys_To_Settings_And_Navigate_After_Approval()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
      services.PostConfigure<XaiChatOptions>(options =>
      {
        options.UseFakeUpstream = true;
        options.ApiKey = "playwright-placeholder-key";
      }));

    await using Session session = await Session.StartAsync();
    IPage page = session.Page;
    await session.GoHomeAsync();
    await session.WaitForConfigurationProbeAsync(0);
    int signedOutProbes = session.ConfigurationStatuses.Count;
    await session.SignInWithNewPasskeyAsync();
    await session.WaitForConfigurationProbeAsync(signedOutProbes);

    // Back to Home: the page Steven asked from, with no passkey controls of its own.
    await session.GoHomeAsync();
    new Uri(page.Url).AbsolutePath.ShouldBe("/");
    string state = await session.OpenAskAsync(signedIn: true);
    state.ShouldBe("Configured", $"Ask text: {await session.AskTextAsync()}");
    await Assertions.Expect(page.Locator("[data-qa=AskEditMode]"))
      .ToHaveAttributeAsync("data-mode", nameof(AgentEditMode.AskBeforeEditing), new LocatorAssertionsToHaveAttributeOptions { Timeout = 30_000 });

    // The fake upstream calls site_map for a "where" question and links the matching page.
    int completionsBefore = session.Completions.Count;
    ILocator input = page.Locator(".sc-ai-input__textarea");
    await Assertions.Expect(input).ToBeFocusedAsync(new LocatorAssertionsToBeFocusedOptions { Timeout = 30_000 });
    await page.Keyboard.TypeAsync("Where can I add a passkey?");
    await Assertions.Expect(input).ToHaveValueAsync("Where can I add a passkey?");
    await page.Locator(".sc-ai-input__send").ClickAsync();
    ILocator answer = page.Locator(".sc-ai-root").GetByText("You can do that on the");
    await answer.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
    string answered = await page.Locator(".sc-ai-root").InnerTextAsync();
    answered.ShouldContain("Settings");
    answered.ShouldNotContain("doesn't expose");
    session.Completions.Count.ShouldBeGreaterThan(completionsBefore);
    string firstRequest = session.Completions[completionsBefore];
    firstRequest.ShouldContain("\"site_map\"");
    firstRequest.ShouldContain("\"navigate\"");
    InstructionsOf(firstRequest).ShouldContain("/Settings");
    InstructionsOf(firstRequest).ShouldNotContain("this page only");
    // The link is the page's url, whether the answer renders markdown or shows it as text.
    bool linked = await page.Locator(".sc-ai-root a[href$='/Settings']").CountAsync() > 0;
    (linked || answered.Contains("](/Settings)", StringComparison.Ordinal)).ShouldBeTrue($"Answer: {answered}");
    await page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-home-where-passkey.png"),
    });
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("ask-home-where-passkey-page.png") });

    // "take me there": navigate asks first in Ask before editing, and nothing moves until approved.
    await input.FillAsync("Take me there");
    await page.Locator(".sc-ai-input__send").ClickAsync();
    ILocator approval = page.Locator("[data-qa=AgentApproval]").Last;
    await approval.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
    (await approval.InnerTextAsync()).ShouldContain("navigate");
    (await page.Locator("[data-qa=AgentApprovalArguments]").Last.InnerTextAsync()).ShouldContain("/Settings");
    new Uri(page.Url).AbsolutePath.ShouldBe("/");
    await page.WaitForTimeoutAsync(1_000);
    await Assertions.Expect(input).Not.ToBeFocusedAsync();
    await page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-navigate-approval.png"),
    });

    await approval.Locator("[data-qa=AgentApprove]").ClickAsync();
    await page.WaitForURLAsync("**/Settings", new PageWaitForURLOptions { Timeout = 60_000 });
    await page.Locator("[data-qa=CreatePasskey], .twe-appbar").First.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("ask-navigate-landed-settings.png") });
    session.AssertNoWasmFailures();
  }

  public static async Task Close_Should_Not_Reopen_Palette_When_Ask_Came_From_Search_Field()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
      services.PostConfigure<XaiChatOptions>(options =>
      {
        options.UseFakeUpstream = true;
        options.ApiKey = "playwright-placeholder-key";
      }));

    await using Session session = await Session.StartAsync();
    IPage page = session.Page;
    await session.GoHomeAsync();
    await session.WaitForConfigurationProbeAsync(0);
    int signedOutProbes = session.ConfigurationStatuses.Count;
    await session.SignInWithNewPasskeyAsync();
    await session.WaitForConfigurationProbeAsync(signedOutProbes);
    await session.GoHomeAsync();

    // Open the palette from the appbar search field (its trigger), not Ctrl-K.
    ILocator search = page.Locator(".twe-appbar__search");
    ILocator paletteInput = page.Locator("[data-qa=CommandPaletteInput]");
    bool opened = false;
    for (int attempt = 0; attempt < 20 && !opened; attempt++)
    {
      await search.ClickAsync();
      try
      {
        await paletteInput.WaitForAsync(new LocatorWaitForOptions { Timeout = 2_000, State = WaitForSelectorState.Visible });
        opened = true;
      }
      catch (TimeoutException)
      {
        await page.Keyboard.PressAsync("Escape");
      }
    }

    opened.ShouldBeTrue("The search field did not open the palette");
    await paletteInput.FillAsync("ask");
    ILocator active = page.Locator(".twe-palette__row--active");
    await Assertions.Expect(active.Locator(".twe-palette__kind"))
      .ToHaveTextAsync("Command", new LocatorAssertionsToHaveTextOptions { Timeout = 10_000 });
    await Assertions.Expect(active.Locator(".twe-palette__name"))
      .ToContainTextAsync("ask", new LocatorAssertionsToContainTextOptions { IgnoreCase = true, Timeout = 10_000 });
    await paletteInput.PressAsync("Enter");
    await page.Locator("[data-qa=AgentAsk]").WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
    await Assertions.Expect(page.Locator(".sc-ai-input__textarea"))
      .ToBeFocusedAsync(new LocatorAssertionsToBeFocusedOptions { Timeout = 30_000 });

    // Close hands focus back to the search field without reopening the palette.
    await page.Locator("[data-qa=AgentAskClose]").ClickAsync();
    await page.Locator("[data-qa=AgentAsk]").WaitForAsync(new LocatorWaitForOptions
    {
      State = WaitForSelectorState.Detached,
      Timeout = 30_000,
    });
    await page.WaitForTimeoutAsync(1_000);
    (await page.Locator("[data-qa=CommandPalette]").IsVisibleAsync()).ShouldBeFalse("Closing Ask reopened the command palette");
    (await page.EvaluateAsync<bool>("() => document.activeElement?.closest('.twe-appbar__search') != null"))
      .ShouldBeTrue("Focus did not return to the search field");
    session.AssertNoWasmFailures();
  }

  private sealed class Session : IAsyncDisposable
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

  /// <summary>Tab cycles the candidates; returns every hint it showed.</summary>
  private static async Task<List<string>> CycleHintsAsync(IPage page, int steps)
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

  private static string ScreenshotPath(string fileName) =>
    PlaywrightScreenshot.PathFor<AskSignIn_Given_Wasm>(fileName);
}
