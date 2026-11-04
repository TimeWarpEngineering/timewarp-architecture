#region Purpose
// Browser proof for task 293: Ask never reports a 401 as "AI not configured", and it re-reads its
// readiness when the user signs in inside the SPA. Also covers the signed-in settings, @ picker,
// feedback Tag and Ctrl-K scenarios that share that sign-in flow (tasks 356, 345, 346, 301).
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
#endregion

namespace AskSignInPlaywright_;

using TimeWarp.Architecture.Features.Applications;
using static AskSignInPlaywright_.AskSignInTestFixture;

public class AskSignInReprobe_Given_Wasm
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskSignInReprobe_Given_Wasm>();

  private static string ScreenshotPath(string fileName) =>
    PlaywrightScreenshot.PathFor<AskSignInReprobe_Given_Wasm>(fileName);

  public static async Task SignIn_Should_ReprobeAsk_And_NeverShowA401AsNotConfigured()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
      services.PostConfigure<XaiChatOptions>(options =>
      {
        options.UseFakeUpstream = true;
        options.ApiKey = "playwright-placeholder-key";
      }));

    await using Session session = await Session.StartAsync<AskSignInReprobe_Given_Wasm>();
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
      string token = await AssertAtReferencePickerAsync<AskSignInReprobe_Given_Wasm>(session.Page);
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
        string tagReference = await AssertTagPicksTheFeedbackRowAsync<AskSignInReprobe_Given_Wasm>(session.Page);
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

    await using Session session = await Session.StartAsync<AskSignInReprobe_Given_Wasm>();
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
}
