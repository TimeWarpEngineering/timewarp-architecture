#region Purpose
// Browser proof that the Ask composer has focus whenever the panel opens (task 381): the top-bar
// button, close and reopen, and the expand toggle each leave the caret in the textarea so typing
// works with no click; a send returns focus to it once the answer lands; close hands focus back
// to the top-bar button that opened the panel.
#endregion

#region Design
// Same host as AskMarkdown_Given_Wasm (in-process Web.Server, fake upstream, mock principal). The
// SPA is signed out here, so the palette lists no commands: the Ctrl-K path and the approval card
// are proven in AskSignIn_Given_Wasm, which signs in. Focus checks are retrying expectations
// because the textarea mounts a render after the panel. Every typing step uses the keyboard on
// whatever has focus, never a click or Fill on the textarea. The send uses the send button, so
// focus has really left the textarea before the answer returns it.
#endregion

namespace AskFocusPlaywright_;

public class AskFocus_Given_Wasm
{
  private const string MockPrincipalId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
  private const string ComposerSelector = ".twe-agent-ask .sc-ai-input__textarea";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskFocus_Given_Wasm>();

  public static async Task Composer_Should_HaveFocus_OnEveryOpen_AfterSend_And_ReturnItOnClose()
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
    await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
      // The host locale can be en-us@posix, which .NET WASM rejects and never renders the shell.
      Locale = "en-US",
      ExtraHTTPHeaders = new Dictionary<string, string>
      {
        ["X-TimeWarp-Mock-Principal-Id"] = MockPrincipalId,
      },
    });
    IPage page = await context.NewPageAsync();
    await page.GotoAsync(InProcTestPorts.WebHostUrl, new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    ILocator button = page.Locator("[data-qa=AskAiButton]");
    await button.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });

    // The button is interactive after the shell's first WebAssembly render. Retry until Ask opens.
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

    // Open from the top-bar button: the caret is in the composer.
    await ExpectComposerFocusedAsync(input);
    await page.Keyboard.TypeAsync("markdown sample");
    await Assertions.Expect(input).ToHaveValueAsync("markdown sample");

    // Send with the button, so focus leaves the textarea; the answer returns it.
    await page.Locator(".twe-agent-ask .sc-ai-input__send").ClickAsync();
    await page.Locator(".twe-agent-ask .sc-ai-message--assistant [data-qa=AskReplyMarkdown] strong")
      .WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    await ExpectComposerFocusedAsync(input);

    // Expand takes focus when clicked; the composer gets it back.
    await page.Locator("[data-qa=AskExpand]").ClickAsync();
    await ExpectComposerFocusedAsync(input);
    await page.Keyboard.TypeAsync("expanded");
    (await input.InputValueAsync()).ShouldEndWith("expanded");

    // Close hands focus back to the opener.
    await page.Locator("[data-qa=AgentAskClose]").ClickAsync();
    await page.Locator("[data-qa=AgentAsk]").WaitForAsync(new LocatorWaitForOptions
    {
      State = WaitForSelectorState.Detached,
      Timeout = 30_000,
    });
    await Assertions.Expect(button).ToBeFocusedAsync(new LocatorAssertionsToBeFocusedOptions { Timeout = 10_000 });

    // Reopen: a second open focuses too.
    await button.ClickAsync();
    await ExpectComposerFocusedAsync(input);
    await page.Keyboard.TypeAsync("again");
    (await input.InputValueAsync()).ShouldEndWith("again");
  }

  private static Task ExpectComposerFocusedAsync(ILocator input) =>
    Assertions.Expect(input).ToBeFocusedAsync(new LocatorAssertionsToBeFocusedOptions { Timeout = 30_000 });
}
