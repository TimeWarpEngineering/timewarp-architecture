#region Purpose
// Session-scoped Chromium shared by every Playwright test class in this project (task 369), so the
// suite installs and launches the browser once instead of once per test.
#endregion

#region Design
// A Jaribu session fixture (SessionHostFixture<IBrowser>), registered once from this file's own
// ModuleInitializer, same shape as SpaSessionFixture in web-spa-integration-tests. The first
// GetAsync installs Chromium (with the ubuntu24.04 fallback for distros newer than Playwright's
// platform list), starts the Playwright driver and launches headless Chromium. The Jaribu session
// hook disposes it once when the test session ends: browser first, then the driver. Test classes
// never dispose it.
// Isolation stays per test: each test opens its own IBrowserContext (cookies, storage, HTTP cache,
// WebAuthn virtual authenticator) and disposes it. The browser holds no per-test state.
// Hosts are NOT shared here. HostGraphFactory binds fixed in-proc ports, so only one host can be up
// at a time, and nearly every test depends on a fresh host: the first passkey account on a host
// claims Administrator, the admin feedback list asserts exact rows, and the rename test changes
// site settings. Each class still owns its HostGraph (C-create).
#endregion

namespace WebSpaPlaywright_;

/// <summary>
/// Shared headless Chromium for the suite. Resolve with <see cref="GetAsync"/>; open a fresh
/// <see cref="IBrowserContext"/> per test.
/// </summary>
public sealed class PlaywrightBrowserSession : SessionHostFixture<IBrowser>
{
  private PlaywrightBrowserSession(IPlaywright playwright, IBrowser browser) : base(browser)
  {
    Playwright = playwright;
  }

  /// <summary>The Playwright driver that launched <see cref="SessionHostFixture{TInner}.Inner"/>.</summary>
  public IPlaywright Playwright { get; }

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterSessionFixture<PlaywrightBrowserSession>();

  /// <summary>The session's shared browser fixture (created on first call).</summary>
  public static Task<PlaywrightBrowserSession> GetAsync() => SessionFixture.GetAsync<PlaywrightBrowserSession>();

  public static async Task<PlaywrightBrowserSession> CreateAsync()
  {
    InstallChromium();
    IPlaywright playwright = await Microsoft.Playwright.Playwright.CreateAsync().ConfigureAwait(false);
    try
    {
      IBrowser browser = await playwright.Chromium
        .LaunchAsync(new BrowserTypeLaunchOptions { Headless = true })
        .ConfigureAwait(false);
      return new PlaywrightBrowserSession(playwright, browser);
    }
    catch
    {
      playwright.Dispose();
      throw;
    }
  }

  public override async ValueTask DisposeAsync()
  {
    await base.DisposeAsync().ConfigureAwait(false);
    Playwright.Dispose();
  }

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
}
