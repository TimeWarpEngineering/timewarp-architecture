#region Purpose
// Browser proof of the Ask microphone (task 453): the button is in the composer, a refused
// microphone shows the denied sentence and a retry name, a recorded clip that the network drops
// shows the failure and a retry, and the retry inserts the fake transcript at the caret without
// sending. While an answer streams the button waits.
#endregion

#region Design
// Same host as AskFocus (in-process Web.Server, fake upstream, mock principal). The microphone
// and MediaRecorder are stubbed in an init script so the proof does not need a device. The first
// transcription request is aborted and the second is held, so transcribing and the error are
// visible before the fake transcript lands. A 422 drops the clip so the next press records. A
// recording already going can still be stopped while an answer streams. New conversation during
// transcribing drops the late transcript. AwaitingInput uses the same TurnInProgress check as
// Streaming, so this file does not stage an approval card. This file is a project test, not a
// Jaribu runfile. CI runs it with the Playwright suite.
#endregion

namespace AskVoiceInputPlaywright_;

public class AskVoiceInput_Given_Wasm
{
  private const string MockPrincipalId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
  private const string ComposerSelector = ".twe-agent-ask .sc-ai-input__textarea";
  private const string MicSelector = "[data-qa=AskVoiceInput]";

  private const string DeniedScript = """
    (() => {
      const devices = navigator.mediaDevices;
      if (!devices) {
        return;
      }

      devices.getUserMedia = () => Promise.reject(new DOMException("denied", "NotAllowedError"));
    })();
    """;

  private const string CaptureScript = """
    (() => {
      const bytes = new Uint8Array([1, 2, 3, 4, 5, 6, 7, 8]);
      class FakeRecorder {
        constructor(_stream, options) {
          this.mimeType = (options && options.mimeType) || "audio/webm";
          this.state = "inactive";
          this.listeners = {};
        }

        start() {
          this.state = "recording";
        }

        stop() {
          if (this.state === "inactive") {
            return;
          }

          this.state = "inactive";
          const blob = new Blob([bytes], { type: this.mimeType });
          for (const fn of this.listeners.dataavailable || []) {
            fn({ data: blob });
          }

          for (const fn of this.listeners.stop || []) {
            fn();
          }
        }

        addEventListener(type, fn) {
          (this.listeners[type] ||= []).push(fn);
        }
      }

      FakeRecorder.isTypeSupported = () => true;
      window.MediaRecorder = FakeRecorder;
      const devices = navigator.mediaDevices || {};
      devices.getUserMedia = async () => ({ getTracks: () => [{ stop() {} }] });
      navigator.mediaDevices = devices;
    })();
    """;

  private static int OverLimitBytes => (TranscribeAgentAudio.MaxDataLength / 4) * 3 + 1;

  private static string TooLongScript => $$"""
    (() => {
      class FakeRecorder {
        constructor(_stream, options) {
          this.mimeType = (options && options.mimeType) || "audio/webm";
          this.state = "inactive";
          this.listeners = {};
        }

        start() {
          this.state = "recording";
        }

        stop() {
          if (this.state === "inactive") {
            return;
          }

          this.state = "inactive";
          const bytes = new Uint8Array({{OverLimitBytes}});
          const blob = new Blob([bytes], { type: this.mimeType });
          for (const fn of this.listeners.dataavailable || []) {
            fn({ data: blob });
          }

          for (const fn of this.listeners.stop || []) {
            fn();
          }
        }

        addEventListener(type, fn) {
          (this.listeners[type] ||= []).push(fn);
        }
      }

      FakeRecorder.isTypeSupported = () => true;
      window.MediaRecorder = FakeRecorder;
      const devices = navigator.mediaDevices || {};
      devices.getUserMedia = async () => ({ getTracks: () => [{ stop() {} }] });
      navigator.mediaDevices = devices;
    })();
    """;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskVoiceInput_Given_Wasm>();

  public static async Task Button_Shows_Denied_Recording_Failure_And_Inserts_On_Retry()
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

    await using IBrowserContext recorded = await OpenAsync(browser, CaptureScript);
    IPage ask = recorded.Pages[0];
    int transcriptions = 0;
    await ask.RouteAsync("**/api/agent-chat/transcriptions", async route =>
    {
      int hit = Interlocked.Increment(ref transcriptions);
      if (hit == 1)
      {
        await route.AbortAsync();
        return;
      }

      await Task.Delay(1200);
      await route.ContinueAsync();
    });

    ILocator input = ask.Locator(ComposerSelector);
    await input.FillAsync("Please");
    ILocator micButton = ask.Locator(MicSelector);
    await micButton.ClickAsync();
    await Assertions.Expect(micButton).ToHaveAttributeAsync("aria-pressed", "true");
    await Assertions.Expect(micButton).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Stop);
    string elapsed = await ask.Locator("[data-qa=AskVoiceElapsed]").InnerTextAsync();
    IsElapsed(elapsed).ShouldBeTrue(elapsed);
    await Task.Delay(300);
    await micButton.ClickAsync();
    await Assertions.Expect(ask.Locator("[data-qa=AskVoiceStatus]")).ToHaveTextAsync(AskVoiceCopy.Failed);
    await Assertions.Expect(micButton).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Retry);
    await micButton.ClickAsync();
    await Assertions.Expect(micButton).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Transcribing);
    await Assertions.Expect(micButton).ToBeDisabledAsync();
    await Assertions.Expect(input).ToHaveValueAsync("Please " + TranscribeAgentAudio.FakeTranscript);
    await Assertions.Expect(input).ToBeFocusedAsync();

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

  public static async Task Refused_Clip_Records_Again_And_Stop_Works_While_Streaming()
  {
    const string noSpeech = "The recording had no speech.";
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
    {
      services.PostConfigure<XaiChatOptions>(options =>
      {
        options.UseFakeUpstream = true;
        options.ApiKey = "playwright-placeholder-key";
      });
    });

    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
    await using (IBrowserContext recorded = await OpenAsync(browser, CaptureScript))
    {
      IPage ask = recorded.Pages[0];
      int hits = 0;
      await ask.RouteAsync("**/api/agent-chat/transcriptions", async route =>
      {
        Interlocked.Increment(ref hits);
        await route.FulfillAsync(new RouteFulfillOptions
        {
          Status = 422,
          ContentType = "application/problem+json",
          Body = """{"title":"No speech recognized","status":422,"detail":"The recording had no speech."}""",
        });
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
      await Assertions.Expect(ask.Locator("[data-qa=AskVoiceStatus]")).ToHaveTextAsync(noSpeech);
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Retry);
      hits.ShouldBe(1);
      await mic.ClickAsync();
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-pressed", "true");
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Stop);
      await Task.Delay(300);
      hits.ShouldBe(1);

      await ask.Locator(".twe-agent-ask .sc-ai-input__send").ClickAsync();
      await Assertions.Expect(ask.Locator("[data-qa=AskStop]")).ToBeVisibleAsync();
      await Assertions.Expect(mic).ToBeEnabledAsync();
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Stop);
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-pressed", "true");
      await mic.ClickAsync();
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-pressed", "false");
      await Assertions.Expect(mic).ToBeDisabledAsync();
      await Assertions.Expect(ask.Locator("[data-qa=AskStop]")).ToBeVisibleAsync();
    }

    await using (IBrowserContext limited = await OpenAsync(browser, TooLongScript))
    {
      IPage ask = limited.Pages[0];
      int hits = 0;
      await ask.RouteAsync("**/api/agent-chat/transcriptions", route =>
      {
        Interlocked.Increment(ref hits);
        return route.AbortAsync();
      });
      ILocator mic = ask.Locator(MicSelector);
      await mic.ClickAsync();
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-pressed", "true");
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Stop);
      await mic.ClickAsync();
      await Assertions.Expect(ask.Locator("[data-qa=AskVoiceStatus]")).ToHaveTextAsync(AskVoiceCopy.TooLong);
      hits.ShouldBe(0);
      await mic.ClickAsync();
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-pressed", "true");
      await Assertions.Expect(mic).ToHaveAttributeAsync("aria-label", AskVoiceCopy.Stop);
    }

    await using IBrowserContext closing = await OpenAsync(browser, CaptureScript);
    IPage page = closing.Pages[0];
    TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    await page.RouteAsync("**/api/agent-chat/transcriptions", async route =>
    {
      await released.Task;
      await route.ContinueAsync();
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
