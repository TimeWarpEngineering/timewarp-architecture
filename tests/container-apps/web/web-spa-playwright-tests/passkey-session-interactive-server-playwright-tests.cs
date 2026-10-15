#region Purpose
// Browser proof for task 350: passkey Create account and Sign in under InteractiveServer leave the
// browser holding the identity-session cookie, and an [Authorize] page then loads.
#endregion

#region Design
// The host is forced to InteractiveServer with prerender off, which is what a clean browser gets on
// a deployment before WASM is cached (InteractiveAuto's first visit). Prerender off means the Login
// buttons only exist once the circuit is up, so a click always reaches the action handler.
// A Chrome DevTools virtual authenticator answers both WebAuthn ceremonies. The old code completed
// the ceremony on web-server's own HttpClient (the self-call), so Set-Cookie never reached the
// browser: the cookie assertion is the regression check. The fresh GotoAsync of /Settings is the
// "signed in on the next request" check: an anonymous load redirects to /Login.
// Sign-in reuses the account the first ceremony made: the resident credential stays in the virtual
// authenticator, and the context's cookies are cleared before Sign in so only that ceremony can
// issue the session.
// Authentication:UseMock is turned off for this host (the project appsettings turn it on): with it
// on, the circuit's auth state is the fixed mock principal, which the server permission store does
// not know, so /Settings renders 403 whatever the cookie says.
// Both tests need the same forced InteractiveServer host and neither depends on a fresh one (no
// Administrator claim, no list counts), so the class boots it once in SetupOnce and disposes it in
// CleanUpOnce (C-create per class). Each test still gets its own browser context, and with it its
// own virtual authenticator, from the suite's shared browser (PlaywrightBrowserSession).
#endregion

namespace PasskeySessionInteractiveServerPlaywright_;

using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Configuration;

public class PasskeySession_Given_InteractiveServer
{
  private const string SessionCookieName = ".timewarp.identity.session";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<PasskeySession_Given_InteractiveServer>();

  private static HostGraph? Graph;

  public static async Task SetupOnce() => Graph = await CreateInteractiveServerHostAsync();

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  public static async Task CreateAccount_Should_SignInTheBrowser()
  {
    await using Session session = await Session.StartAsync();

    await session.CreateAccountAsync();

    await session.AssertSignedInAsync("create-account");
  }

  public static async Task SignIn_Should_SignInTheBrowser()
  {
    await using Session session = await Session.StartAsync();

    await session.CreateAccountAsync();
    await session.Context.ClearCookiesAsync();

    await session.GoToLoginAsync();
    await session.Page.Locator("[data-qa=ContinueWithPasskey]").ClickAsync();
    await session.Page.WaitForURLAsync(url => !url.Contains("/Login", StringComparison.OrdinalIgnoreCase), new PageWaitForURLOptions
    {
      Timeout = 60_000,
    });

    await session.AssertSignedInAsync("sign-in");
  }

  private static Task<HostGraph> CreateInteractiveServerHostAsync() =>
    HostGraphFactory.CreateWebAsync(
      services =>
        services.AddSingleton<IOptions<BlazorSettings>>(Options.Create(new BlazorSettings
        {
          RenderMode = RenderMode.InteractiveServer,
          Prerender = false,
        })),
      // The endpoints dictionary is the host's last configuration source. UseMock off so the
      // circuit runs the real identity-session auth state provider, not the fixed mock principal.
      new Dictionary<string, string?>(WebTestServerApplication.HttpsServiceEndpoints)
      {
        ["Authentication:UseMock"] = "false",
      });

  private sealed class Session : IAsyncDisposable
  {
    private readonly ConcurrentQueue<string> Console = new();

    private Session(IBrowserContext context, IPage page)
    {
      Context = context;
      Page = page;
    }

    public IBrowserContext Context { get; }

    public IPage Page { get; }

    public static async Task<Session> StartAsync()
    {
      IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
      IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
      {
        IgnoreHTTPSErrors = true,
        Locale = "en-US",
      });
      IPage page = await context.NewPageAsync();
      Session session = new(context, page);
      page.Console += (_, message) => session.Console.Enqueue($"{message.Type}: {message.Text}");
      page.PageError += (_, error) => session.Console.Enqueue(error);

      // A platform authenticator that always verifies the user, so both ceremonies complete headless.
      ICDPSession cdp = await context.NewCDPSessionAsync(page);
      await cdp.SendAsync("WebAuthn.enable");
      await cdp.SendAsync("WebAuthn.addVirtualAuthenticator", new Dictionary<string, object>
      {
        ["options"] = new Dictionary<string, object>
        {
          ["protocol"] = "ctap2",
          ["transport"] = "internal",
          ["hasResidentKey"] = true,
          ["hasUserVerification"] = true,
          ["isUserVerified"] = true,
          ["automaticPresenceSimulation"] = true,
        },
      });
      return session;
    }

    public async Task GoToLoginAsync()
    {
      await Page.GotoAsync(InProcTestPorts.WebHostUrl.TrimEnd('/') + "/Login", new PageGotoOptions
      {
        WaitUntil = WaitUntilState.DOMContentLoaded,
        Timeout = 120_000,
      });
      await Page.Locator("[data-qa=CreatePasskey]").WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
    }

    public async Task CreateAccountAsync()
    {
      await GoToLoginAsync();
      await Page.Locator("[data-qa=CreatePasskey]").ClickAsync();
      await Page.WaitForURLAsync("**/Settings", new PageWaitForURLOptions { Timeout = 60_000 });
    }

    public async Task AssertSignedInAsync(string flow)
    {
      IReadOnlyList<BrowserContextCookiesResult> cookies = await Context.CookiesAsync();
      cookies.Select(cookie => cookie.Name).ShouldContain(
        SessionCookieName,
        $"{flow}: the browser has no {SessionCookieName} cookie. Console: {string.Join(" | ", Console)}");

      // A new request (and a new circuit) must be signed in: an anonymous load redirects to /Login.
      await Page.GotoAsync(InProcTestPorts.WebHostUrl.TrimEnd('/') + "/Settings", new PageGotoOptions
      {
        WaitUntil = WaitUntilState.DOMContentLoaded,
        Timeout = 120_000,
      });
      ILocator settled = Page.Locator("[data-qa=SignedInAccount], [data-qa=ContinueWithPasskey]").First;
      try
      {
        await settled.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
      }
      finally
      {
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
          Path = PlaywrightScreenshot.PathFor<PasskeySession_Given_InteractiveServer>($"{flow}-settings.png"),
        });
      }

      new Uri(Page.Url).AbsolutePath.ShouldBe("/Settings", $"{flow}: /Settings did not load signed in");
      string log = string.Join('\n', Console);
      log.ShouldNotContain("Unhandled exception rendering component");
    }

    // The browser is session-owned (PlaywrightBrowserSession); only this test's context is closed.
    public ValueTask DisposeAsync() => Context.DisposeAsync();
  }
}
