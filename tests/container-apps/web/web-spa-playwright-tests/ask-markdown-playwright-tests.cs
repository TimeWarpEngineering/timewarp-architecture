#region Purpose
// Browser proof that Ask renders an assistant reply as safe Markdown (task 380): bold, a list and
// links are elements, raw HTML is text, the user's own message stays plain, and an in-app link
// navigates through the routing State action without reloading the page.
#endregion

#region Design
// Same host as AskPanelDesign_Given_Wasm (in-process Web.Server, fake upstream, mock principal).
// "markdown sample" makes the fake answer with its MarkdownSample. A marker set on window before
// the click survives only if the page did not reload. /Login is the in-app link because the SPA is
// signed out here. The external link carries target=_blank and rel noopener noreferrer.
#endregion

namespace AskMarkdownPlaywright_;

public class AskMarkdown_Given_Wasm
{
  private const string MockPrincipalId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskMarkdown_Given_Wasm>();

  public static async Task Reply_Should_RenderMarkdown_And_NavigateInApp()
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
    ILocator input = page.Locator(".twe-agent-ask .sc-ai-input__textarea");
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

    await input.FillAsync("markdown sample **not bold**");
    await input.PressAsync("Enter");

    ILocator reply = page.Locator(".twe-agent-ask .sc-ai-message--assistant [data-qa=AskReplyMarkdown]");
    await reply.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    await reply.Locator("a[href='/Login']").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });

    (await reply.Locator("strong").InnerTextAsync()).ShouldBe("bold");
    (await reply.Locator("ul > li").CountAsync()).ShouldBe(2);
    (await reply.Locator("ul > li").Nth(0).InnerTextAsync()).ShouldBe("first item");

    ILocator external = reply.Locator("a[href='https://example.com']");
    (await external.GetAttributeAsync("target")).ShouldBe("_blank");
    (await external.GetAttributeAsync("rel") ?? "").ShouldContain("noopener noreferrer");
    (await reply.Locator("a[href='/Login']").GetAttributeAsync("target")).ShouldBeNull();

    (await page.Locator("[data-qa=AskRawHtml]").CountAsync()).ShouldBe(0);
    (await reply.InnerTextAsync()).ShouldContain("<b data-qa=\"AskRawHtml\">raw</b>");

    ILocator user = page.Locator(".twe-agent-ask .sc-ai-message--user");
    (await user.InnerTextAsync()).ShouldContain("**not bold**");
    (await user.Locator("strong").CountAsync()).ShouldBe(0);

    await page.Locator(".twe-agent-ask").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = PlaywrightScreenshot.PathFor<AskMarkdown_Given_Wasm>("ask-markdown-reply.png"),
    });

    await page.EvaluateAsync("() => { window.__askNoReload = true; }");
    await reply.Locator("a[href='/Login']").ClickAsync();
    await page.WaitForURLAsync(url => new Uri(url).AbsolutePath.StartsWith("/Login", StringComparison.OrdinalIgnoreCase),
      new PageWaitForURLOptions { Timeout = 30_000 });

    (await page.EvaluateAsync<bool>("() => window.__askNoReload === true")).ShouldBeTrue(
      "The in-app link reloaded the page instead of routing in the SPA");
  }
}
