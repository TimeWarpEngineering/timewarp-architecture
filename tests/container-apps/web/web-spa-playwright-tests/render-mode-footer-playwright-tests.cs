#region Purpose
// Browser proof for task 510: the footer shows the render mode as an icon with a tooltip, not
// the RendererInfo.Name word, in Static, Server, and WebAssembly, light and dark.
#endregion

#region Design
// Two hosts, one per interactive mode: InteractiveServer and InteractiveWebAssembly, both with
// prerender on. Static is the Server host's prerendered page with blazor.web.js blocked, so the
// theme script still runs and dark mode still applies. Dark is the OS preference in a fresh
// context (BrandThemeScript follows it when no mode is stored).
// Each state checks data-mode, role="img", the aria-label words, an svg, and no visible name
// text, then screenshots the footer slot. With blazor.web.js blocked Fluent's components never
// load, so the Static pass also checks the un-upgraded tooltip words are not in the footer.
// The Server pass also hovers the icon and screenshots the open tooltip above the footer.
// Shots go to test output (PlaywrightScreenshot).
#endregion

namespace RenderModeFooterPlaywright_;

using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Configuration;

public class RenderModeFooter_Given_Shell
{
  private const string MockPrincipalId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RenderModeFooter_Given_Shell>();

  public static async Task Footer_Should_Show_Static_And_Server_As_Icons()
  {
    await using HostGraph graph = await CreateHostAsync(RenderMode.InteractiveServer);
    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;

    foreach (ColorScheme scheme in new[] { ColorScheme.Light, ColorScheme.Dark })
    {
      string theme = scheme == ColorScheme.Light ? "light" : "dark";

      await using (IBrowserContext staticContext = await NewContextAsync(browser, scheme))
      {
        await staticContext.RouteAsync("**/_framework/blazor.web*.js", route => route.AbortAsync());
        IPage page = await staticContext.NewPageAsync();
        await GotoAsync(page);
        await AssertModeAsync(page, "Static", "Pre-rendered page, not yet interactive.");
        (await page.Locator(".twe-footer").InnerTextAsync()).ShouldNotContain("Rendering:", Case.Sensitive, "the un-upgraded tooltip printed in the footer");
        await ShootFooterAsync(page, $"static-{theme}.png");
      }

      await using IBrowserContext context = await NewContextAsync(browser, scheme);
      IPage live = await context.NewPageAsync();
      await GotoAsync(live);
      await WaitModeAsync(live, "Server");
      await AssertModeAsync(live, "Server", "Running on the server over a live connection.");
      await ShootFooterAsync(live, $"server-{theme}.png");

      if (scheme == ColorScheme.Light)
      {
        await live.Locator("[data-qa=RenderMode]").HoverAsync();
        await live.WaitForFunctionAsync
        (
          "() => { const t = document.querySelector('fluent-tooltip[anchor=twe-render-mode]'); return t && t.matches(':popover-open'); }",
          null,
          new PageWaitForFunctionOptions { Timeout = 10_000 }
        );
        await live.WaitForTimeoutAsync(300);
        await ShootFooterAsync(live, "server-tooltip-light.png", extraAbove: 90);
      }
    }

    _ = graph;
  }

  public static async Task Footer_Should_Show_WebAssembly_As_An_Icon()
  {
    await using HostGraph graph = await CreateHostAsync(RenderMode.InteractiveWebAssembly);
    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;

    foreach (ColorScheme scheme in new[] { ColorScheme.Light, ColorScheme.Dark })
    {
      await using IBrowserContext context = await NewContextAsync(browser, scheme);
      IPage page = await context.NewPageAsync();
      await GotoAsync(page);
      await WaitModeAsync(page, "WebAssembly");
      await AssertModeAsync(page, "WebAssembly", "Running in your browser.");
      await ShootFooterAsync(page, $"webassembly-{(scheme == ColorScheme.Light ? "light" : "dark")}.png");
    }

    _ = graph;
  }

  private static Task<HostGraph> CreateHostAsync(RenderMode mode) =>
    HostGraphFactory.CreateWebAsync(services =>
      services.AddSingleton<IOptions<BlazorSettings>>(Options.Create(new BlazorSettings
      {
        RenderMode = mode,
        Prerender = true,
      })));

  private static async Task<IBrowserContext> NewContextAsync(IBrowser browser, ColorScheme scheme) =>
    await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      // The host locale can be en-us@posix, which .NET WASM rejects and never renders the shell.
      Locale = "en-US",
      ColorScheme = scheme,
      ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
      ExtraHTTPHeaders = new Dictionary<string, string> { ["X-TimeWarp-Mock-Principal-Id"] = MockPrincipalId },
    });

  private static Task GotoAsync(IPage page) =>
    page.GotoAsync(InProcTestPorts.WebHostUrl, new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });

  private static Task WaitModeAsync(IPage page, string mode) =>
    page.WaitForFunctionAsync
    (
      $"() => {{ const e = document.querySelector('[data-qa=RenderMode]'); return e && e.dataset.mode === '{mode}'; }}",
      null,
      new PageWaitForFunctionOptions { Timeout = 120_000 }
    );

  private static async Task AssertModeAsync(IPage page, string mode, string sentence)
  {
    ILocator slot = page.Locator("[data-qa=RenderMode]");
    (await slot.CountAsync()).ShouldBe(1);
    (await slot.GetAttributeAsync("data-mode")).ShouldBe(mode);
    (await slot.GetAttributeAsync("role")).ShouldBe("img");
    (await slot.GetAttributeAsync("aria-label")).ShouldBe($"Rendering: {mode}. {sentence}");
    (await slot.Locator("svg").CountAsync()).ShouldBe(1, $"[{mode}] no icon in the footer slot");
    (await slot.InnerTextAsync()).Trim().ShouldBeEmpty($"[{mode}] the footer still prints the mode name");
  }

  private static async Task ShootFooterAsync(IPage page, string fileName, int extraAbove = 0)
  {
    // Let Fluent's web components upgrade, the theme settle, and the fade finish.
    await page.WaitForTimeoutAsync(500);
    LocatorBoundingBoxResult? box = await page.Locator(".fluent-layout-item[area=footer]").BoundingBoxAsync();
    box.ShouldNotBeNull("footer has no box");
    float top = Math.Max(0, box.Y - extraAbove);
    await page.ScreenshotAsync(new PageScreenshotOptions
    {
      Path = PlaywrightScreenshot.PathFor<RenderModeFooter_Given_Shell>(fileName),
      Clip = new Clip { X = box.X, Y = top, Width = box.Width, Height = box.Y + box.Height - top },
    });
  }
}
