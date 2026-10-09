#region Purpose
// Browser proof for task 290: the server-prerendered shell is styled on first paint and matches
// the settled interactive layout, so the page no longer flashes while Blazor starts.
#endregion

#region Design
// The host runs the app's real default: InteractiveAuto with prerender on. The test project's
// appsettings turn prerender off for the Ask test, so this class replaces IOptions<BlazorSettings>.
// First paint is measured two ways. A context with JavaScript disabled sees only the prerendered
// HTML and the stylesheets in <head>. A JavaScript context with blazor.web.js blocked sees the
// same markup in a real script-capable page that never goes interactive. The settled pass loads
// normally and waits until the footer's render-mode label leaves "Static".
// The prerender bug was CSS written as a C# string inside <style>@(...)</style>. Prerender
// HTML-encoded it (&#xA;, &quot;) and browsers do not decode entities inside <style>, so every
// shell rule was dropped until interactive render rewrote the text. Two guards cover that:
// a source scan (no component under source/ authors a <style> element) and a prerendered-HTML
// scan (no <style> in the served page contains an HTML entity). FluentLayout emits its own
// container-query <style>; it is framework markup, so the HTML scan checks entities, not count.
// Screenshots go to artifacts/playwright/290 under the repo root. CI uploads that folder.
// Brand purple is --twe-purple (#55409c), the same value App.razor puts in data-theme-color.
#endregion

namespace FirstPaintPlaywright_;

using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Configuration;

public partial class FirstPaint_Given_PrerenderedShell
{
  private const string MockPrincipalId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
  private const string BrandPurple = "rgb(85, 64, 156)";
  private const string Paper = "rgb(255, 255, 255)";
  private const string FluentWebBlue = "#0f6cbd";
  // --twe-appbar-height (70px) minus FluentLayoutItem's 8px top and bottom padding.
  private const double AppBarHeight = 54;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FirstPaint_Given_PrerenderedShell>();

  public static Task Components_Should_AuthorNoStyleElements()
  {
    string root = RepoRoot();
    string source = Path.Combine(root, "source");
    List<string> offenders = [];
    foreach (string file in Directory.EnumerateFiles(source, "*.razor", SearchOption.AllDirectories))
    {
      if (IsBuildOutput(file))
      {
        continue;
      }

      // Razor comments may name the banned pattern; only markup counts.
      string text = RazorComment().Replace(File.ReadAllText(file), string.Empty);
      if (StyleOpenTag().IsMatch(text))
      {
        offenders.Add(Path.GetRelativePath(root, file));
      }
    }

    offenders.ShouldBeEmpty(
      "Components must not author <style> elements; use Component.razor.css (skills/tw-blazor-css-strategy). "
      + "Offenders: " + string.Join(", ", offenders));
    return Task.CompletedTask;
  }

  public static async Task FirstPaint_Should_MatchTheSettledShell()
  {
    InstallChromium();
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
      services.AddSingleton<IOptions<BlazorSettings>>(Options.Create(new BlazorSettings
      {
        RenderMode = TimeWarp.Architecture.Configuration.RenderMode.InteractiveAuto,
        Prerender = true,
      })));

    using IPlaywright playwright = await Playwright.CreateAsync();
    string html = await FetchPrerenderedHtmlAsync(playwright);
    await File.WriteAllTextAsync(ScreenshotPath("prerendered.html"), html);
    AssertStyleElementsAreNotEncoded(html);
    html.ShouldContain("data-theme-color=\"#55409c\"");

    await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

    ShellMetrics jsOff = await MeasureAsync(browser, javaScriptEnabled: false, blockBlazor: false, "first-paint-js-off.png");
    ShellMetrics preInteractive = await MeasureAsync(browser, javaScriptEnabled: true, blockBlazor: true, "first-paint-pre-interactive.png");
    ShellMetrics settled = await MeasureAsync(browser, javaScriptEnabled: true, blockBlazor: false, "settled-interactive.png");

    foreach ((string name, ShellMetrics metrics) in new[] { ("js-off", jsOff), ("pre-interactive", preInteractive), ("settled", settled) })
    {
      // Logged so the CI run shows the measured boxes next to the screenshots.
      Console.WriteLine($"[290 first paint] {name}: {metrics}");
      AssertShellLayout(name, metrics);
    }

    // First paint must not move when Blazor goes interactive. The search input is compared, not
    // its wrapper: once upgraded, fluent-field adds a 4px label row around the 32px input.
    foreach ((string name, ShellMetrics early) in new[] { ("js-off", jsOff), ("pre-interactive", preInteractive) })
    {
      early.AppBar.ShouldBe(settled.AppBar, $"{name}: app bar moved when the page went interactive");
      early.Logo.ShouldBe(settled.Logo, $"{name}: logo moved when the page went interactive");
      early.SearchInput.ShouldBe(settled.SearchInput, $"{name}: search input moved when the page went interactive");
      early.HeaderBackground.ShouldBe(settled.HeaderBackground, $"{name}: header frame color changed");
    }

    settled.RenderMode.ShouldNotBe("Static", "the settled pass never went interactive");
    settled.FluentBrand.ShouldNotBeNullOrWhiteSpace("Fluent brand tokens were not applied");
    settled.FluentBrand.ShouldNotBe(FluentWebBlue, "Fluent fell back to its default blue theme");
    // MainLayout no longer applies a theme, so a purple ramp here came from <body data-theme-color>
    // at Fluent's script start. Purple: red and blue both above green (Fluent blue fails this).
    int red = Convert.ToInt32(settled.FluentBrand[1..3], 16);
    int green = Convert.ToInt32(settled.FluentBrand[3..5], 16);
    int blue = Convert.ToInt32(settled.FluentBrand[5..7], 16);
    (red > green && blue > green).ShouldBeTrue($"--colorBrandBackground {settled.FluentBrand} is not a purple brand ramp");
  }

  private static void AssertShellLayout(string name, ShellMetrics metrics)
  {
    metrics.AppBarDisplay.ShouldBe("flex", $"{name}: .twe-appbar is not flex (shell CSS not applied)");
    metrics.AppBarBackground.ShouldBe(Paper, $"{name}: .twe-appbar background");
    metrics.HeaderBackground.ShouldBe(BrandPurple, $"{name}: header frame is not brand purple");
    ((double)metrics.AppBar.Height).ShouldBe(AppBarHeight, 1, $"{name}: app bar height");
    metrics.Logo.Y.ShouldBeGreaterThanOrEqualTo(0, $"{name}: logo clipped above the viewport");
    ((double)metrics.Logo.Height).ShouldBe(48, 1, $"{name}: logo height");
    metrics.Search.Width.ShouldBeGreaterThanOrEqualTo(400, $"{name}: search box is not full width");
    metrics.SearchEndLines.ShouldBe(1, $"{name}: \"Ctrl-K\" wrapped");
  }

  private static void AssertStyleElementsAreNotEncoded(string html)
  {
    MatchCollection styles = StyleElement().Matches(html);
    foreach (Match style in styles)
    {
      string text = style.Groups[1].Value;
      text.ShouldNotContain("&#x", Case.Insensitive, "prerendered <style> is HTML-encoded: " + Excerpt(text));
      text.ShouldNotContain("&quot;", Case.Insensitive, "prerendered <style> is HTML-encoded: " + Excerpt(text));
      text.ShouldNotContain("&amp;", Case.Insensitive, "prerendered <style> is HTML-encoded: " + Excerpt(text));
    }
  }

  [GeneratedRegex(@"@\*.*?\*@", RegexOptions.Singleline)]
  private static partial Regex RazorComment();

  [GeneratedRegex(@"<style[\s>]", RegexOptions.IgnoreCase)]
  private static partial Regex StyleOpenTag();

  [GeneratedRegex("<style[^>]*>(.*?)</style>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
  private static partial Regex StyleElement();

  private static string Excerpt(string text) => text.Length <= 160 ? text : text[..160];

  private static async Task<string> FetchPrerenderedHtmlAsync(IPlaywright playwright)
  {
    await using IAPIRequestContext request = await playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      ExtraHTTPHeaders = new Dictionary<string, string> { ["X-TimeWarp-Mock-Principal-Id"] = MockPrincipalId },
    });
    IAPIResponse response = await request.GetAsync(InProcTestPorts.WebHostUrl);
    response.Ok.ShouldBeTrue($"GET / returned {response.Status}");
    string html = await response.TextAsync();
    html.ShouldContain("twe-appbar");
    return html;
  }

  private static async Task<ShellMetrics> MeasureAsync(IBrowser browser, bool javaScriptEnabled, bool blockBlazor, string screenshot)
  {
    await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      JavaScriptEnabled = javaScriptEnabled,
      Locale = "en-US",
      ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
      ExtraHTTPHeaders = new Dictionary<string, string> { ["X-TimeWarp-Mock-Principal-Id"] = MockPrincipalId },
    });
    if (blockBlazor)
    {
      await context.RouteAsync("**/_framework/blazor.web*.js", route => route.AbortAsync());
    }

    IPage page = await context.NewPageAsync();
    await page.GotoAsync(InProcTestPorts.WebHostUrl, new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
    ILocator mode = page.Locator(".twe-footer__mode");
    if (javaScriptEnabled && !blockBlazor)
    {
      await page.WaitForFunctionAsync(
        "() => { const e = document.querySelector('.twe-footer__mode'); return e && e.textContent.trim() !== 'Static'; }",
        null,
        new PageWaitForFunctionOptions { Timeout = 120_000 });
      // Let Fluent's web components upgrade and the theme settle before measuring.
      await page.WaitForTimeoutAsync(1_000);
    }

    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath(screenshot) });
    System.Text.Json.JsonElement raw = await page.EvaluateAsync<System.Text.Json.JsonElement>(
      """
      () => {
        const box = s => { const r = document.querySelector(s).getBoundingClientRect(); return { x: Math.round(r.x), y: Math.round(r.y), width: Math.round(r.width), height: Math.round(r.height) }; };
        const css = (s, p) => getComputedStyle(document.querySelector(s))[p];
        return {
          appBar: box('.twe-appbar'),
          logo: box('.twe-brand__logo'),
          search: box('.twe-appbar__search'),
          searchInput: box('.twe-appbar__search fluent-text-input'),
          appBarDisplay: css('.twe-appbar', 'display'),
          appBarBackground: css('.twe-appbar', 'backgroundColor'),
          headerBackground: css('.fluent-layout-item[area=header]', 'backgroundColor'),
          searchEndLines: document.querySelector('.search-end').getClientRects().length,
          renderMode: document.querySelector('.twe-footer__mode').textContent.trim(),
          fluentBrand: getComputedStyle(document.documentElement).getPropertyValue('--colorBrandBackground').trim(),
        };
      }
      """);
    ShellMetrics? metrics = raw.Deserialize<ShellMetrics>(JsonOptions);
    metrics.ShouldNotBeNull();
    (await mode.CountAsync()).ShouldBe(1);
    return metrics;
  }

  private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

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

  private static bool IsBuildOutput(string path)
  {
    string separator = Path.DirectorySeparatorChar.ToString();
    return path.Contains($"{separator}bin{separator}", StringComparison.Ordinal)
      || path.Contains($"{separator}obj{separator}", StringComparison.Ordinal);
  }

  private static string RepoRoot()
  {
    string? directory = AppContext.BaseDirectory;
    while (directory is not null && !File.Exists(Path.Combine(directory, "timewarp-architecture.slnx")))
    {
      directory = Path.GetDirectoryName(directory);
    }

    directory.ShouldNotBeNull();
    return directory;
  }

  private static string ScreenshotPath(string fileName)
  {
    string folder = Path.Combine(RepoRoot(), "artifacts", "playwright", "290");
    Directory.CreateDirectory(folder);
    return Path.Combine(folder, fileName);
  }
}

public sealed record Box(int X, int Y, int Width, int Height);

public sealed record ShellMetrics(
  Box AppBar,
  Box Logo,
  Box Search,
  Box SearchInput,
  string AppBarDisplay,
  string AppBarBackground,
  string HeaderBackground,
  int SearchEndLines,
  string RenderMode,
  string FluentBrand);
