#region Purpose
// Browser proof of the Ask microphone (tasks 453 and 505): the button is in the composer, a
// refused microphone shows the denied sentence and a retry name, a dictation streams a live
// preview into the composer and stop replaces it with the final text without sending, a dropped
// socket and each server error show their sentence, and a late transcript after New conversation
// does not land. While an answer streams the button waits, except to stop a recording.
#endregion

#region Design
// Same host as AskFocus (in-process Web.Server, fake upstream, mock principal). The microphone
// is a stub that returns an oscillator stream, so the real worklet, socket, and relay run: the
// fake upstream answers the first audio frame with the interim "Voice" and the stop with
// TranscribeAgentAudio.FakeTranscript. The failure cases route the dictation socket instead
// (RouteWebSocketAsync without ConnectToServer): it closes at once, or it says ready and answers
// done with an error code, or it holds the final text until the test releases it. The routed
// server messages are built from TranscribeAgentAudio so the test does not copy the protocol.
// AwaitingInput uses the same TurnInProgress check as Streaming, so this file does not stage an
// approval card. This file is a project test, not a Jaribu runfile. CI runs it with the
// Playwright suite.
#endregion

namespace AskVoiceInputPlaywright_;

using System.Text.RegularExpressions;

public class AskVoiceInput_Given_Wasm
{
  private const string MockPrincipalId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
  private const string ComposerSelector = ".twe-agent-ask .sc-ai-input__textarea";
  private const string MicSelector = "[data-qa=AskVoiceInput]";

  private static readonly Regex StreamUrl = new(Regex.Escape(TranscribeAgentAudio.StreamPath), RegexOptions.None, TimeSpan.FromSeconds(1));

  private const string DeniedScript = """
    (() => {
      const devices = navigator.mediaDevices;
      if (!devices) {
        return;
      }

      devices.getUserMedia = () => Promise.reject(new DOMException("denied", "NotAllowedError"));
    })();
    """;

  private const string MicrophoneScript = """
    (() => {
      const devices = navigator.mediaDevices || {};
      devices.getUserMedia = async () => {
        const context = new AudioContext();
        const tone = context.createOscillator();
        const sink = context.createMediaStreamDestination();
        tone.connect(sink);
        tone.start();
        return sink.stream;
      };
      navigator.mediaDevices = devices;
    })();
    """;

  private static string Ready => $$"""{"type":"{{TranscribeAgentAudio.MessageTypes.Ready}}"}""";

  private static string Error(string code) =>
    $$"""{"type":"{{TranscribeAgentAudio.MessageTypes.Error}}","code":"{{code}}"}""";

  private static string Done(string text) =>
    $$"""{"type":"{{TranscribeAgentAudio.MessageTypes.Done}}","text":"{{text}}"}""";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskVoiceInput_Given_Wasm>();

  public static async Task Button_Shows_Denied_Streams_A_Preview_And_Inserts_On_Stop()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
    {
      services.PostConfigure<XaiChatOptions>(options =>
      {
        options.UseFakeUpstream = true;
        options.ApiKey = "playwright-placeholder-key";
      });
    });

    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
    await using (IBrowserContext denied = await OpenAsync(browser, DeniedScript))
    {
      IPage page = denied.Pages[0];
      ILocator mic = page.Locator(MicSelector);
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Dictate);
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-pressed", "false");
      await mic.ClickAsync();
      await Assertions.Expect(page.Locator("[data-qa=AskVoiceStatus]")).ToHaveTextAsync(AskVoiceCopy.Denied);
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Retry);
      (await page.Locator(ComposerSelector).InputValueAsync()).ShouldBeEmpty();
    }

    await using (IBrowserContext dropped = await OpenAsync(browser, MicrophoneScript))
    {
      IPage page = dropped.Pages[0];
      int sockets = 0;
      await page.RouteWebSocketAsync(StreamUrl, async socket =>
      {
        Interlocked.Increment(ref sockets);
        await socket.CloseAsync();
      });
      ILocator mic = page.Locator(MicSelector);
      await mic.ClickAsync();
      await Assertions.Expect(page.Locator("[data-qa=AskVoiceStatus]")).ToHaveTextAsync(AskVoiceCopy.Failed);
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Retry);
      await mic.ClickAsync();
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Stop);
      await Assertions.Expect(page.Locator("[data-qa=AskVoiceStatus]")).ToHaveTextAsync(AskVoiceCopy.Failed);
      sockets.ShouldBe(2);
    }

    await using IBrowserContext recorded = await OpenAsync(browser, MicrophoneScript);
    IPage ask = recorded.Pages[0];
    ILocator input = ask.Locator(ComposerSelector);
    await input.FillAsync("Please");
    ILocator micButton = ask.Locator(MicSelector);
    await micButton.ClickAsync();
    await Assertions.Expect(micButton).ToHaveAttributeAsync("aria-pressed", "true");
    await Assertions.Expect(micButton).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Stop);
    string elapsed = await ask.Locator("[data-qa=AskVoiceElapsed]").InnerTextAsync();
    IsElapsed(elapsed).ShouldBeTrue(elapsed);
    await Assertions.Expect(input).ToHaveValueAsync("Please Voice");
    await micButton.ClickAsync();
    await Assertions.Expect(input).ToHaveValueAsync("Please " + TranscribeAgentAudio.FakeTranscript);
    await Assertions.Expect(input).ToBeFocusedAsync();
    await Assertions.Expect(micButton).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Dictate);
    (await ask.Locator("[data-qa=AskVoiceStatus]").CountAsync()).ShouldBe(0);

    await ask.RouteAsync("**/api/agent-chat/completions", async route =>
    {
      await Task.Delay(1500);
      await route.ContinueAsync();
    });
    await ask.Locator(".twe-agent-ask .sc-ai-input__send").ClickAsync();
    await Assertions.Expect(ask.Locator("[data-qa=AskStop]")).ToBeVisibleAsync();
    await Assertions.Expect(micButton).ToBeDisabledAsync();
    await Assertions.Expect(ask.Locator("[data-qa=AskStop]")).ToBeHiddenAsync();
    await Assertions.Expect(micButton).ToBeEnabledAsync();
  }

  public static async Task Server_Errors_Show_Their_Sentence_And_Stop_Works_While_Streaming()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
    {
      services.PostConfigure<XaiChatOptions>(options =>
      {
        options.UseFakeUpstream = true;
        options.ApiKey = "playwright-placeholder-key";
      });
    });

    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
    await using (IBrowserContext refused = await OpenAsync(browser, MicrophoneScript))
    {
      IPage ask = refused.Pages[0];
      string answer = TranscribeAgentAudio.ErrorCodes.NoSpeech;
      await ask.RouteWebSocketAsync(StreamUrl, socket =>
      {
        socket.OnMessage(frame =>
        {
          if (frame.Text == TranscribeAgentAudio.ClientMessages.Done)
          {
            socket.Send(Error(answer));
          }
        });
        socket.Send(Ready);
      });
      await ask.RouteAsync("**/api/agent-chat/completions", async route =>
      {
        await Task.Delay(5000);
        await route.ContinueAsync();
      });

      ILocator input = ask.Locator(ComposerSelector);
      await input.FillAsync("Please");
      ILocator mic = ask.Locator(MicSelector);
      await mic.ClickAsync();
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Stop);
      await mic.ClickAsync();
      await Assertions.Expect(ask.Locator("[data-qa=AskVoiceStatus]")).ToHaveTextAsync(AskVoiceCopy.NoSpeech);
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Retry);

      answer = TranscribeAgentAudio.ErrorCodes.TimedOut;
      await mic.ClickAsync();
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Stop);
      await mic.ClickAsync();
      await Assertions.Expect(ask.Locator("[data-qa=AskVoiceStatus]")).ToHaveTextAsync(AskVoiceCopy.TimedOut);

      await mic.ClickAsync();
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-pressed", "true");
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Stop);
      await ask.Locator(".twe-agent-ask .sc-ai-input__send").ClickAsync();
      await Assertions.Expect(ask.Locator("[data-qa=AskStop]")).ToBeVisibleAsync();
      await Assertions.Expect(mic).ToBeEnabledAsync();
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Stop);
      await mic.ClickAsync();
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-pressed", "false");
      await Assertions.Expect(mic).ToBeDisabledAsync();
      await Assertions.Expect(ask.Locator("[data-qa=AskStop]")).ToBeVisibleAsync();
    }

    await using (IBrowserContext keyless = await OpenAsync(browser, MicrophoneScript))
    {
      IPage ask = keyless.Pages[0];
      await ask.RouteWebSocketAsync(StreamUrl, socket => socket.Send(Error(TranscribeAgentAudio.ErrorCodes.NeedsKey)));
      ILocator mic = ask.Locator(MicSelector);
      await mic.ClickAsync();
      await Assertions.Expect(ask.Locator("[data-qa=AskVoiceStatus]")).ToHaveTextAsync(AskVoiceCopy.NeedsKey);
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Retry);
    }

    await using IBrowserContext closing = await OpenAsync(browser, MicrophoneScript);
    IPage page = closing.Pages[0];
    TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    await page.RouteWebSocketAsync(StreamUrl, socket =>
    {
      socket.OnMessage(frame =>
      {
        if (frame.Text == TranscribeAgentAudio.ClientMessages.Done)
        {
          _ = released.Task.ContinueWith(_ => socket.Send(Done(TranscribeAgentAudio.FakeTranscript)), TaskScheduler.Default);
        }
      });
      socket.Send(Ready);
    });
    ILocator composer = page.Locator(ComposerSelector);
    await composer.FillAsync("Keep");
    ILocator microphone = page.Locator(MicSelector);
    await microphone.ClickAsync();
    await Assertions.Expect(microphone).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Stop);
    await microphone.ClickAsync();
    await Assertions.Expect(microphone).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Transcribing);
    await page.Locator("[data-qa=AskNewConversation]").ClickAsync();
    await Assertions.Expect(microphone).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Dictate);
    released.SetResult();
    await Task.Delay(500);
    await Assertions.Expect(microphone).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Dictate);
    string value = await composer.InputValueAsync();
    value.ShouldNotContain(TranscribeAgentAudio.FakeTranscript);
    (await page.Locator("[data-qa=AskVoiceStatus]").CountAsync()).ShouldBe(0);
  }

  private static async Task<IBrowserContext> OpenAsync(IBrowser browser, string script)
  {
    IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
      Locale = "en-US",
      ExtraHTTPHeaders = new Dictionary<string, string>
      {
        ["X-TimeWarp-Mock-Principal-Id"] = MockPrincipalId,
      },
    });
    await context.AddInitScriptAsync(script);
    IPage page = await context.NewPageAsync();
    page.SetDefaultTimeout(30_000);
    await page.GotoAsync(InProcTestPorts.WebHostUrl, new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    ILocator button = page.Locator("[data-qa=AskAiButton]");
    await button.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
    ILocator input = page.Locator(ComposerSelector);
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

    opened.ShouldBeTrue("The top-bar Ask button did not open the panel with a composer");
    await page.Locator(MicSelector).WaitForAsync();
    return context;
  }

  private static bool IsElapsed(string text)
  {
    int colon = text.IndexOf(':', StringComparison.Ordinal);
    if (colon < 1 || text.Length != colon + 3)
    {
      return false;
    }

    for (int index = 0; index < text.Length; index++)
    {
      if (index == colon)
      {
        continue;
      }

      if (!char.IsAsciiDigit(text[index]))
      {
        return false;
      }
    }

    return true;
  }
}
