#region Purpose
// Browser proof for task 293: Ask never reports a 401 as "AI not configured", and it re-reads its
// readiness when the user signs in inside the SPA.
#endregion

#region Design
// Real InteractiveWebAssembly against Web.Server with the real identity-session cookie. No mock
// principal header is sent, so the configuration endpoint authenticates exactly the way it does on
// a developer machine. Sign-in stays in the SPA. The first test signs in from Ask's own "Sign in"
// button, which closes the panel and routes to /Login. The second uses the Home "Sign in" button,
// the path Steven took on 2026-10-09. Both then Create account with a passkey.
// A Chrome DevTools virtual authenticator answers the WebAuthn ceremony. The app then navigates to
// /Settings without a page load, which is why a once-per-app probe never ran again.
// The signed-out and signed-in checks are collected and asserted together so a failing run on the
// old code reports both bugs: the masked 401 and the stale readiness after sign-in.
// Every GET api/agent-chat/configuration status is recorded so the test proves the probe re-ran and
// what the server answered. After sign-in the test waits for that re-probe's response before
// opening Ask, so it does not read the store while it still holds the signed-out answer. The configured pass uses the fake upstream (no request leaves the
// process); the no-key pass has no XAI:ApiKey. Screenshots go to artifacts/playwright/293, which
// CI uploads.
#endregion

namespace AskSignInPlaywright_;

public class AskSignIn_Given_Wasm
{
  private const string ConfigurationPath = "/api/agent-chat/configuration";

  private const string AnyAskState =
    "[data-qa=AgentAskNotConfigured], [data-qa=AgentAskSignIn], [data-qa=AgentAskError], .sc-ai-input__textarea";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskSignIn_Given_Wasm>();

  public static async Task SignIn_Should_ReprobeAsk_And_NeverShowA401AsNotConfigured()
  {
    InstallChromium();
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
      services.PostConfigure<XaiChatOptions>(options =>
      {
        options.UseFakeUpstream = true;
        options.ApiKey = "playwright-placeholder-key";
      }));

    await using Session session = await Session.StartAsync();
    List<string> failures = [];

    // Signed out. The server must answer 401 and Ask must say so, not "AI not configured".
    await session.GoHomeAsync();
    string signedOut = await session.OpenAskAsync();
    await session.Page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-signed-out.png"),
    });
    int signedOutProbes = session.ConfigurationStatuses.Count;
    if (!session.ConfigurationStatuses.Contains(401))
    {
      failures.Add($"Signed out: expected GET {ConfigurationPath} to return 401. Saw [{session.Statuses}].");
    }

    if (signedOut != "AgentAskSignIn")
    {
      failures.Add($"Signed out: the server answered 401 but Ask showed {signedOut} ('{await session.AskTextAsync()}').");
    }

    // Sign in inside the SPA: Ask "Sign in" closes the panel -> /Login -> Create account (virtual passkey) -> /Settings.
    if (signedOut == "AgentAskSignIn")
    {
      await session.SignInFromAskWithNewPasskeyAsync();
    }
    else
    {
      await session.SignInWithNewPasskeyAsync();
    }

    await session.WaitForConfigurationProbeAsync(signedOutProbes);
    string signedIn = await session.OpenAskAsync();
    await session.Page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath(signedIn == "Configured" ? "ask-signed-in-configured.png" : "ask-signed-in-stale.png"),
    });
    List<int> afterSignIn = [.. session.ConfigurationStatuses.Skip(signedOutProbes)];
    if (!afterSignIn.Contains(200))
    {
      failures.Add(
        $"Signed in: GET {ConfigurationPath} was not re-run with a 200 after sign-in. "
        + $"All statuses: [{session.Statuses}], after sign-in: [{string.Join(", ", afterSignIn)}].");
    }

    if (signedIn != "Configured")
    {
      failures.Add($"Signed in with XAI:ApiKey set: Ask showed {signedIn} ('{await session.AskTextAsync()}').");
    }
    else
    {
      ILocator input = session.Page.Locator(".sc-ai-input__textarea");
      await input.FillAsync("What is on this page?");
      await session.Page.Locator(".sc-ai-input__send").ClickAsync();
      ILocator answer = session.Page.Locator(".sc-ai-root").GetByText("page_context:");
      await answer.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
      await session.Page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
      {
        Path = ScreenshotPath("ask-signed-in-configured-answer.png"),
      });
    }

    Console.WriteLine($"task-293 configuration statuses: [{session.Statuses}]; signed out: {signedOut}; signed in: {signedIn}");
    failures.ShouldBeEmpty(string.Join(Environment.NewLine, failures));
    session.AssertNoWasmFailures();
  }

  public static async Task SignedIn_WithoutKey_Should_ShowTheSetupCommand()
  {
    InstallChromium();
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
      services.PostConfigure<XaiChatOptions>(options =>
      {
        options.UseFakeUpstream = false;
        options.ApiKey = null;
      }));

    await using Session session = await Session.StartAsync();
    await session.GoHomeAsync();
    await session.WaitForConfigurationProbeAsync(0);
    int signedOutProbes = session.ConfigurationStatuses.Count;
    await session.SignInWithNewPasskeyAsync();
    await session.WaitForConfigurationProbeAsync(signedOutProbes);
    string state = await session.OpenAskAsync();
    await session.Page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-signed-in-no-key.png"),
    });

    state.ShouldBe("AgentAskNotConfigured", $"Ask text: {await session.AskTextAsync()}; statuses [{session.Statuses}]");
    session.ConfigurationStatuses.ShouldContain(200);
    (await session.Page.Locator("[data-qa=AgentAskNotConfigured]").InnerTextAsync()).ShouldBe("AI not configured");
    (await session.Page.Locator("[data-qa=AgentAskSetupCommand]").InnerTextAsync()).ShouldBe(XaiChatDefaults.SetupCommand);
    session.AssertNoWasmFailures();
  }

  private sealed class Session : IAsyncDisposable
  {
    private readonly IPlaywright Playwright;
    private readonly IBrowser Browser;
    private readonly IBrowserContext Context;
    private readonly ConcurrentQueue<string> Console = new();
    private readonly ConcurrentQueue<int> Configuration = new();

    private Session(IPlaywright playwright, IBrowser browser, IBrowserContext context, IPage page)
    {
      Playwright = playwright;
      Browser = browser;
      Context = context;
      Page = page;
    }

    public IPage Page { get; }

    public List<int> ConfigurationStatuses => [.. Configuration];

    public string Statuses => string.Join(", ", Configuration);

    public static async Task<Session> StartAsync()
    {
      IPlaywright playwright = await Microsoft.Playwright.Playwright.CreateAsync();
      IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
      IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
      {
        IgnoreHTTPSErrors = true,
        // The host locale can be en-us@posix, which .NET WASM rejects and never renders the shell.
        Locale = "en-US",
      });
      IPage page = await context.NewPageAsync();
      Session session = new(playwright, browser, context, page);
      page.Console += (_, message) => session.Console.Enqueue($"{message.Type}: {message.Text}");
      page.PageError += (_, error) => session.Console.Enqueue(error);
      page.Response += (_, response) =>
      {
        if (new Uri(response.Url).AbsolutePath.Equals(ConfigurationPath, StringComparison.OrdinalIgnoreCase))
        {
          session.Configuration.Enqueue(response.Status);
        }
      };

      // A platform authenticator that always verifies the user, so the passkey ceremony completes headless.
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

    public async Task GoHomeAsync()
    {
      await Page.GotoAsync(InProcTestPorts.WebHostUrl, new PageGotoOptions
      {
        WaitUntil = WaitUntilState.DOMContentLoaded,
        Timeout = 120_000,
      });
      await Page.Locator(".twe-appbar").WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
    }

    public async Task<string> OpenAskAsync()
    {
      // The palette listener attaches on the shell's first interactive render. Retry Ctrl-K.
      ILocator ask = Page.Locator("[data-qa=CommandPaletteAsk]");
      bool opened = false;
      for (int attempt = 0; attempt < 20 && !opened; attempt++)
      {
        await Page.Keyboard.PressAsync("Control+k");
        try
        {
          await ask.WaitForAsync(new LocatorWaitForOptions { Timeout = 2_000, State = WaitForSelectorState.Visible });
          opened = true;
        }
        catch (TimeoutException)
        {
          await Page.Keyboard.PressAsync("Escape");
        }
      }

      opened.ShouldBeTrue("Ctrl-K did not show Ask");
      await ask.ClickAsync();
      ILocator state = Page.Locator(AnyAskState).First;
      await state.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000, State = WaitForSelectorState.Visible });
      foreach (string qa in new[] { "AgentAskNotConfigured", "AgentAskSignIn", "AgentAskError" })
      {
        if (await Page.Locator($"[data-qa={qa}]").IsVisibleAsync())
        {
          return qa;
        }
      }

      return "Configured";
    }

    public async Task<string> AskTextAsync() =>
      (await Page.Locator("[data-qa=AgentAsk]").InnerTextAsync()).ReplaceLineEndings(" | ");

    public async Task SignInFromAskWithNewPasskeyAsync()
    {
      await Page.Locator("[data-qa=AgentAskSignInButton]").ClickAsync();
      await Page.WaitForURLAsync("**/Login**", new PageWaitForURLOptions { Timeout = 30_000 });
      await Page.Locator("[data-qa=AgentAsk]").WaitForAsync(new LocatorWaitForOptions
      {
        State = WaitForSelectorState.Hidden,
        Timeout = 10_000,
      });
      await CreatePasskeyAsync();
    }

    public async Task SignInWithNewPasskeyAsync()
    {
      await Page.Locator("[data-qa=HomeSignIn]").ClickAsync();
      await Page.WaitForURLAsync("**/Login**", new PageWaitForURLOptions { Timeout = 30_000 });
      await CreatePasskeyAsync();
    }

    private async Task CreatePasskeyAsync()
    {
      await Page.Locator("[data-qa=CreatePasskey]").ClickAsync();
      await Page.WaitForURLAsync("**/Settings", new PageWaitForURLOptions { Timeout = 60_000 });
      await Page.Locator(".twe-appbar").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    }

    /// <summary>
    /// Waits until a configuration probe answers after <paramref name="priorCount"/> recorded probes,
    /// so Ask is not opened while the store still holds the signed-out answer. Returns without
    /// throwing on timeout: the old code never re-probes, and the caller's assertions report that.
    /// </summary>
    public async Task WaitForConfigurationProbeAsync(int priorCount)
    {
      DateTime deadline = DateTime.UtcNow.AddSeconds(30);
      while (Configuration.Count <= priorCount && DateTime.UtcNow < deadline)
      {
        await Page.WaitForTimeoutAsync(100);
      }

      // The response event fires before the handler stores the result; let the dispatch finish.
      await Page.WaitForTimeoutAsync(250);
    }

    public void AssertNoWasmFailures()
    {
      string log = string.Join('\n', Console);
      log.ShouldNotContain("PlatformNotSupportedException");
      log.ShouldNotContain("SemaphoreSlim.Wait");
      log.ShouldNotContain("Unhandled exception rendering component");
    }

    public async ValueTask DisposeAsync()
    {
      await Context.DisposeAsync();
      await Browser.DisposeAsync();
      Playwright.Dispose();
    }
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

  private static string ScreenshotPath(string fileName)
  {
    string? directory = AppContext.BaseDirectory;
    while (directory is not null && !File.Exists(Path.Combine(directory, "timewarp-architecture.slnx")))
    {
      directory = Path.GetDirectoryName(directory);
    }

    directory.ShouldNotBeNull();
    string folder = Path.Combine(directory, "artifacts", "playwright", "293");
    Directory.CreateDirectory(folder);
    return Path.Combine(folder, fileName);
  }
}
