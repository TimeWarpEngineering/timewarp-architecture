#region Purpose
// Browser proof for task 465: in dark mode every shell surface and page is dark, and the top-bar
// theme button switches Fluent's mode, remembers it, and switches back to the unchanged light shell.
#endregion

#region Design
// One in-process host with prerender on and InteractiveWebAssembly and the fake chat upstream,
// the same graph the shell-nav and Ask suites use.
// Fluent marks dark mode with body[data-theme="dark"], and tokens.css keys its dark overrides on
// that attribute. On first load Fluent takes the stored mode, or App.razor's data-theme="light"
// when none is stored. The walk test stores mode dark (what the theme button leaves) in an init
// script, signs in with a new passkey, and adds the Developer role so the demo and developer
// pages render. Then it opens every PageRegistry entry and shoots each page to test output
// (PlaywrightScreenshot). The Ask panel is opened from the top bar and shot too. The checks are
// not pixel checks. The body background must be dark and its text light. No visible light-DOM
// box may paint a near-white background (relative luminance over 0.6). A design-system swatch
// ([data-token]) paints its token on purpose and is skipped. Fluent's shadow DOM is Fluent's own
// theme and is not walked.
// Fluent's default sheet paints body itself (colorNeutralBackground1) in both modes, so the
// light check reads the shell's own token surfaces: the app bar is --twe-paper (#ffffff), the
// nav column is --twe-paper-2 (#f5f5f7), and --twe-ink is #1a1d28. The toggle test starts in a
// light context and checks those. The button dispatches ApplicationState.ToggleTheme. A reload
// keeps dark, because Fluent stores the mode, and a second click returns to the same light
// values. With light stored, an OS switch to dark leaves the page light.
#endregion

namespace DarkThemePlaywright_;

using DesignSystemCatalogPlaywright_;
using FeedbackAttachmentPlaywright_;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Configuration;

public class DarkTheme_Given_Shell
{
  private const string LightPaper = "rgb(255, 255, 255)";
  private const string LightPaper2 = "rgb(245, 245, 247)";
  private const string LightInk = "#1a1d28";

  // Relative luminance of each computed background, alpha under 0.5 ignored. Accepts rgb() and
  // the color(srgb …) form that color-mix() computes to. Returns up to 20 offenders.
  private const string BrightSurfacesScript =
    """
    () => {
      const channel = v => v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4);
      const luminance = text => {
        let parts, scale = 255;
        let m = text.match(/^rgba?\(([^)]+)\)$/);
        if (!m) { m = text.match(/^color\(srgb ([^)]+)\)$/); scale = 1; }
        if (!m) return null;
        parts = m[1].split(/[\s,\/]+/).filter(Boolean).map(Number);
        if (parts.length > 3 && parts[3] < 0.5) return null;
        return 0.2126 * channel(parts[0] / scale) + 0.7152 * channel(parts[1] / scale) + 0.0722 * channel(parts[2] / scale);
      };
      const out = [];
      for (const el of document.querySelectorAll('body *')) {
        const box = el.getBoundingClientRect();
        if (box.width < 24 || box.height < 16) continue;
        const style = getComputedStyle(el);
        if (style.visibility === 'hidden' || style.display === 'none') continue;
        if (el.closest('[data-token]')) continue;
        const l = luminance(style.backgroundColor);
        if (l !== null && l > 0.6) {
          out.push(el.tagName.toLowerCase() + [...el.classList].map(c => '.' + c).join('') + ' ' + style.backgroundColor);
          if (out.length >= 20) break;
        }
      }
      return out;
    }
    """;

  // The stored choice the theme button leaves behind. BrandThemeScript merges onto it and keeps mode.
  private const string StoreDarkScript =
    "localStorage.setItem('fluentui-blazor:theme-settings', JSON.stringify({ mode: 'dark' }));";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<DarkTheme_Given_Shell>();

  public static async Task StoredDark_Should_Darken_EveryPage_And_Ask()
  {
    await using HostGraph graph = await CreateHostAsync();
    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
    await using IBrowserContext context = await NewContextAsync(browser, ColorScheme.Light);
    await context.AddInitScriptAsync(StoreDarkScript);
    IPage page = await context.NewPageAsync();
    await FeedbackAttachmentTestFixture.SignInWithNewPasskeyAsync(page, context);
    await DesignSystemCatalog_Given_Wasm.GrantDeveloperAsync(graph);

    List<string> failures = [];
    foreach (PageRegistryEntry entry in PageRegistry.All)
    {
      await GotoAsync(page, entry.Url);
      try
      {
        await WaitInteractiveAsync(page, timeout: 30_000);
      }
      catch (TimeoutException)
      {
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = ShotPath("stuck", entry.Url), FullPage = true });
        failures.Add($"{entry.Url}: not interactive at {page.Url}");
        continue;
      }

      await WaitThemeAsync(page, dark: true);
      await page.WaitForTimeoutAsync(500);
      await page.ScreenshotAsync(new PageScreenshotOptions { Path = ShotPath("dark", entry.Url), FullPage = true });
      failures.AddRange(await DarkFailuresAsync(page, entry.Url));
    }

    await GotoAsync(page, "/");
    await WaitInteractiveAsync(page);
    await WaitThemeAsync(page, dark: true);
    await OpenAskAsync(page);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = PlaywrightScreenshot.PathFor<DarkTheme_Given_Shell>("dark-ask.png") });
    failures.AddRange(await DarkFailuresAsync(page, "Ask"));

    failures.ShouldBeEmpty("Dark mode left light surfaces:\n" + string.Join('\n', failures));
    _ = graph;
  }

  public static async Task ThemeButton_Should_Toggle_Remember_And_Restore_Light()
  {
    await using HostGraph graph = await CreateHostAsync();
    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
    await using IBrowserContext context = await NewContextAsync(browser, ColorScheme.Light);
    IPage page = await context.NewPageAsync();

    await GotoAsync(page, "/");
    await WaitInteractiveAsync(page);
    await WaitThemeAsync(page, dark: false);
    await AssertLightAsync(page);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = PlaywrightScreenshot.PathFor<DarkTheme_Given_Shell>("light-home.png") });

    ILocator toggle = page.Locator("[data-qa=ThemeToggle]");
    await ClickUntilThemeAsync(page, toggle, dark: true);
    (await SettledDarkFailuresAsync(page, "/ after toggle")).ShouldBeEmpty();

    await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
    await WaitInteractiveAsync(page);
    await WaitThemeAsync(page, dark: true);

    await ClickUntilThemeAsync(page, toggle, dark: false);
    await AssertLightAsync(page);

    // An OS switch to dark does not override the stored light choice.
    await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
    await page.WaitForTimeoutAsync(500);
    await AssertLightAsync(page);
    _ = graph;
  }

  private static Task<HostGraph> CreateHostAsync() =>
    HostGraphFactory.CreateWebAsync(services =>
    {
      services.AddSingleton<IOptions<BlazorSettings>>(Options.Create(new BlazorSettings
      {
        RenderMode = TimeWarp.Architecture.Configuration.RenderMode.InteractiveWebAssembly,
        Prerender = true,
      }));
      services.PostConfigure<XaiChatOptions>(options =>
      {
        options.UseFakeUpstream = true;
        options.ApiKey = "playwright-placeholder-key";
      });
    });

  private static async Task<IBrowserContext> NewContextAsync(IBrowser browser, ColorScheme scheme) =>
    await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      // The host locale can be en-us@posix, which .NET WASM rejects and never renders the shell.
      Locale = "en-US",
      ColorScheme = scheme,
      ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
    });

  private static Task GotoAsync(IPage page, string url) =>
    page.GotoAsync
    (
      new Uri(new Uri(InProcTestPorts.WebHostUrl), url).ToString(),
      new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 120_000 }
    );

  private static Task WaitInteractiveAsync(IPage page, float timeout = 120_000) =>
    page.WaitForFunctionAsync
    (
      "() => { const e = document.querySelector('.twe-footer__mode'); return e && e.textContent.trim() !== 'Static'; }",
      null,
      new PageWaitForFunctionOptions { Timeout = timeout }
    );

  private static Task WaitThemeAsync(IPage page, bool dark) =>
    page.WaitForFunctionAsync
    (
      dark
        ? "() => document.body.getAttribute('data-theme') === 'dark'"
        : "() => document.body.getAttribute('data-theme') !== 'dark'",
      null,
      new PageWaitForFunctionOptions { Timeout = 30_000 }
    );

  // The button is live after the first interactive render. Retry until the mode flips.
  private static async Task ClickUntilThemeAsync(IPage page, ILocator toggle, bool dark)
  {
    await toggle.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
    for (int attempt = 0; attempt < 10; attempt++)
    {
      await toggle.ClickAsync();
      try
      {
        await WaitThemeAsync(page, dark);
        return;
      }
      catch (TimeoutException) when (attempt < 9)
      {
      }
    }
  }

  private static async Task OpenAskAsync(IPage page)
  {
    ILocator button = page.Locator("[data-qa=AskAiButton]");
    await button.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
    ILocator input = page.Locator(".twe-agent-ask .sc-ai-input__textarea");
    for (int attempt = 0; attempt < 15; attempt++)
    {
      await button.ClickAsync();
      try
      {
        await input.WaitForAsync(new LocatorWaitForOptions { Timeout = 4_000 });
        await page.WaitForTimeoutAsync(500);
        return;
      }
      catch (TimeoutException) when (attempt < 14)
      {
      }
    }
  }

  private static async Task<List<string>> DarkFailuresAsync(IPage page, string label)
  {
    List<string> failures = [];
    double background = await page.EvaluateAsync<double>(LuminanceOf("getComputedStyle(document.body).backgroundColor"));
    double ink = await page.EvaluateAsync<double>(LuminanceOf("getComputedStyle(document.body).color"));
    if (background > 0.05)
    {
      failures.Add($"{label}: body background luminance {background:0.000}");
    }

    if (ink < 0.5)
    {
      failures.Add($"{label}: body text luminance {ink:0.000}");
    }

    string[] bright = await page.EvaluateAsync<string[]>(BrightSurfacesScript);
    failures.AddRange(bright.Select(offender => $"{label}: {offender}"));
    return failures;
  }

  // FluentNav items animate their background, so the first read after the attribute flips can
  // land mid-transition. Poll until the page settles or two seconds pass.
  private static async Task<List<string>> SettledDarkFailuresAsync(IPage page, string label)
  {
    List<string> failures = await DarkFailuresAsync(page, label);
    for (int attempt = 0; attempt < 10 && failures.Count > 0; attempt++)
    {
      await page.WaitForTimeoutAsync(200);
      failures = await DarkFailuresAsync(page, label);
    }

    return failures;
  }

  private static async Task AssertLightAsync(IPage page)
  {
    (await page.EvaluateAsync<string>("() => getComputedStyle(document.querySelector('.twe-appbar')).backgroundColor"))
      .ShouldBe(LightPaper);
    (await page.EvaluateAsync<string>("() => getComputedStyle(document.querySelector('.twe-nav-column')).backgroundColor"))
      .ShouldBe(LightPaper2);
    (await page.EvaluateAsync<string>("() => getComputedStyle(document.body).getPropertyValue('--twe-ink').trim()"))
      .ShouldBe(LightInk);
  }

  private static string LuminanceOf(string expression) =>
    "() => { const p = " + expression + ".match(/[\\d.]+/g).map(Number);"
    + " const c = v => { v /= 255; return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4); };"
    + " return 0.2126 * c(p[0]) + 0.7152 * c(p[1]) + 0.0722 * c(p[2]); }";

  private static string ShotPath(string mode, string url)
  {
    string slug = url.Trim('/').Replace('/', '-').ToLowerInvariant();
    return PlaywrightScreenshot.PathFor<DarkTheme_Given_Shell>($"{mode}-{(slug.Length == 0 ? "home" : slug)}.png");
  }
}
