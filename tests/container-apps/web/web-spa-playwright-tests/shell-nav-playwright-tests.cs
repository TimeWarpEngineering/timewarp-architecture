#region Purpose
// Browser proof that the nav toggle collapses and expands the shell column and remembers the choice.
#endregion

#region Design
// One in-process host with prerender on. The suite's appsettings leave prerender off, which
// emits an empty Blazor marker, so a blocked blazor.web.js would have no column to measure.
// 1440px starts full: collapse, reload, expand, reload. 1100px starts as the rail and the
// toggle widens it. 600px hides the toggle. A context that blocks blazor.web.js and already
// has twe.shellNav=rail still paints the 68px column, so the head script, not the interactive
// render, applies the choice.
#endregion

namespace ShellNavPlaywright_;

using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Configuration;

public class NavWidth_Given_Shell
{
  private const string MockPrincipalId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<NavWidth_Given_Shell>();

  public static async Task Toggle_Should_Collapse_Expand_And_Remember()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
      services.AddSingleton<IOptions<BlazorSettings>>(Options.Create(new BlazorSettings
      {
        RenderMode = TimeWarp.Architecture.Configuration.RenderMode.InteractiveWebAssembly,
        Prerender = true,
      })));
    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;

    await using IBrowserContext wide = await NewContextAsync(browser, 1440, 900);
    IPage widePage = await wide.NewPageAsync();
    await GotoAsync(widePage);
    await WaitInteractiveAsync(widePage);

    await AssertWidthAsync(widePage, 240);
    await AssertToggleAsync(widePage, "Collapse navigation", expanded: true);
    await widePage.GetByRole(AriaRole.Button, new() { Name = "Collapse navigation", Exact = true }).ClickAsync();
    await AssertWidthAsync(widePage, 68);
    await AssertToggleAsync(widePage, "Expand navigation", expanded: false);
    await AssertRailDetailsAsync(widePage);
    (await StoredChoiceAsync(widePage)).ShouldBe("rail");

    await widePage.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
    await WaitInteractiveAsync(widePage);
    await AssertWidthAsync(widePage, 68);
    await AssertToggleAsync(widePage, "Expand navigation", expanded: false);

    await widePage.GetByRole(AriaRole.Button, new() { Name = "Expand navigation", Exact = true }).ClickAsync();
    await AssertWidthAsync(widePage, 240);
    await AssertToggleAsync(widePage, "Collapse navigation", expanded: true);
    (await StoredChoiceAsync(widePage)).ShouldBe("full");

    await widePage.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
    await WaitInteractiveAsync(widePage);
    await AssertWidthAsync(widePage, 240);
    await AssertToggleAsync(widePage, "Collapse navigation", expanded: true);

    await using IBrowserContext mid = await NewContextAsync(browser, 1100, 800);
    IPage midPage = await mid.NewPageAsync();
    await GotoAsync(midPage);
    await WaitInteractiveAsync(midPage);
    await AssertWidthAsync(midPage, 68);
    await AssertToggleAsync(midPage, "Expand navigation", expanded: false);
    await midPage.GetByRole(AriaRole.Button, new() { Name = "Expand navigation", Exact = true }).ClickAsync();
    await AssertWidthAsync(midPage, 240);
    await AssertToggleAsync(midPage, "Collapse navigation", expanded: true);
    (await StoredChoiceAsync(midPage)).ShouldBe("full");

    await using IBrowserContext drawer = await NewContextAsync(browser, 600, 800);
    IPage drawerPage = await drawer.NewPageAsync();
    await GotoAsync(drawerPage);
    await WaitInteractiveAsync(drawerPage);
    await Assertions.Expect(drawerPage.Locator("[data-qa=NavWidthToggle]")).ToBeHiddenAsync();

    await using IBrowserContext early = await NewContextAsync(browser, 1440, 900);
    await early.AddInitScriptAsync("localStorage.setItem('twe.shellNav','rail');");
    await early.RouteAsync("**/_framework/blazor.web*.js", route => route.AbortAsync());
    IPage earlyPage = await early.NewPageAsync();
    await GotoAsync(earlyPage);
    await AssertWidthAsync(earlyPage, 68);
    await AssertToggleAsync(earlyPage, "Expand navigation", expanded: false);
    (await earlyPage.EvaluateAsync<string>("() => document.documentElement.getAttribute('data-twe-nav')")).ShouldBe("rail");

    _ = graph;
  }

  public static async Task Toggle_Icon_Should_Render_At_Full_Size()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
      services.AddSingleton<IOptions<BlazorSettings>>(Options.Create(new BlazorSettings
      {
        RenderMode = TimeWarp.Architecture.Configuration.RenderMode.InteractiveWebAssembly,
        Prerender = true,
      })));
    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;

    await using IBrowserContext wide = await NewContextAsync(browser, 1440, 900);
    IPage page = await wide.NewPageAsync();
    await GotoAsync(page);
    await WaitInteractiveAsync(page);

    // Full column: the collapse toggle must be a normal icon button.
    await AssertToggleSizeAsync(page, "Collapse navigation");

    // Rail: the expand toggle replaces it, centered, still a normal icon button.
    await page.Locator("[data-qa=NavWidthToggle] [aria-label='Collapse navigation']").ClickAsync();
    await page.WaitForFunctionAsync
    (
      "() => document.documentElement.getAttribute('data-twe-nav') === 'rail'",
      null,
      new PageWaitForFunctionOptions { Timeout = 10_000 }
    );
    await AssertToggleSizeAsync(page, "Expand navigation");

    _ = graph;
  }

  private static async Task AssertToggleSizeAsync(IPage page, string name)
  {
    ILocator button = page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });
    await Assertions.Expect(button).ToBeVisibleAsync();

    double buttonWidth = await button.EvaluateAsync<double>("el => el.getBoundingClientRect().width");
    double buttonHeight = await button.EvaluateAsync<double>("el => el.getBoundingClientRect().height");
    buttonWidth.ShouldBeGreaterThanOrEqualTo(24.0, customMessage: $"[{name}] visible button width");
    buttonHeight.ShouldBeGreaterThanOrEqualTo(24.0, customMessage: $"[{name}] visible button height");

    double iconWidth = await button.EvaluateAsync<double>
    (
      "el => { const icon = el.querySelector('fluent-icon') || el.querySelector('svg') || el; return icon.getBoundingClientRect().width; }"
    );
    double iconHeight = await button.EvaluateAsync<double>
    (
      "el => { const icon = el.querySelector('fluent-icon') || el.querySelector('svg') || el; return icon.getBoundingClientRect().height; }"
    );
    iconWidth.ShouldBeGreaterThanOrEqualTo(16.0, customMessage: $"[{name}] visible icon width");
    iconHeight.ShouldBeGreaterThanOrEqualTo(16.0, customMessage: $"[{name}] visible icon height");
  }

  private static async Task<IBrowserContext> NewContextAsync(IBrowser browser, int width, int height) =>
    await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      Locale = "en-US",
      ViewportSize = new ViewportSize { Width = width, Height = height },
      ExtraHTTPHeaders = new Dictionary<string, string> { ["X-TimeWarp-Mock-Principal-Id"] = MockPrincipalId },
    });

  private static Task GotoAsync(IPage page) =>
    page.GotoAsync(InProcTestPorts.WebHostUrl, new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });

  private static Task WaitInteractiveAsync(IPage page) =>
    page.WaitForFunctionAsync
    (
      "() => { const e = document.querySelector('.twe-footer__mode'); return e && e.textContent.trim() !== 'Static'; }",
      null,
      new PageWaitForFunctionOptions { Timeout = 120_000 }
    );

  private static async Task AssertWidthAsync(IPage page, double expected)
  {
    await page.WaitForFunctionAsync
    (
      $"() => {{ const nav = document.querySelector('.fluent-layout-item[area=nav]'); return nav && Math.abs(nav.getBoundingClientRect().width - {expected}) <= 2; }}",
      null,
      new PageWaitForFunctionOptions { Timeout = 10_000 }
    );
    double width = await page.EvaluateAsync<double>
    (
      "() => document.querySelector('.fluent-layout-item[area=nav]').getBoundingClientRect().width"
    );
    width.ShouldBe(expected, 2);
  }

  private static async Task AssertToggleAsync(IPage page, string name, bool expanded)
  {
    ILocator button = page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });
    await Assertions.Expect(button).ToBeVisibleAsync();
    await Assertions.Expect(button).ToHaveAttributeAsync("aria-expanded", expanded ? "true" : "false");
    string other = name == "Collapse navigation" ? "Expand navigation" : "Collapse navigation";
    await Assertions.Expect(page.Locator($"[aria-label='{other}']")).ToBeHiddenAsync();
  }

  private static async Task AssertRailDetailsAsync(IPage page)
  {
    await page.WaitForFunctionAsync
    (
      "() => { const link = document.querySelector('a[href=\"https://github.com/TimeWarpEngineering/timewarp-architecture\"]'); const label = document.querySelector('.twe-nav__label'); return link && link.getAttribute('title') === 'GitHub' && label && label.getBoundingClientRect().width <= 1; }",
      null,
      new PageWaitForFunctionOptions { Timeout = 10_000 }
    );
    int cards = await page.Locator("[data-qa=ShellUserCard]").CountAsync();
    if (cards > 0)
    {
      double meta = await page.EvaluateAsync<double>
      (
        "() => document.querySelector('.twe-user-card__meta').getBoundingClientRect().width"
      );
      meta.ShouldBeLessThanOrEqualTo(1);
    }
  }

  private static Task<string?> StoredChoiceAsync(IPage page) =>
    page.EvaluateAsync<string?>("() => localStorage.getItem('twe.shellNav')");
}
