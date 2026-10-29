#region Purpose
// Browser proof that Clear key asks, Cancel and Escape leave the stored key, Enter does not
// confirm, and the dialog's Clear key removes it. The design-system sample asks the same way.
// A host key shows the saved policy as having no effect.
#endregion

#region Design
// Same host shape as the design-system drawer: in-process Web.Server, shared Chromium, en-US,
// a virtual passkey. The first account is Administrator, which can open Admin → AI. The default
// host has no durable secret store, so the clear-key case registers a memory store and clears
// any host XAI:ApiKey. The page saves a key, then the real FluentUI dialog runs. Developer is
// granted after that so /DesignSystem can open the sample confirmation.
// HostKey_Should_Show_The_Policy_As_Having_No_Effect sets XAI:ApiKey. The select is absent.
// The saved policy and the host reason are shown.
#endregion

namespace ClearXaiApiKeyPlaywright_;

using Microsoft.Extensions.DependencyInjection.Extensions;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Secrets;
using TimeWarp.Identity;

public class ClearXaiApiKey_Given_Wasm
{
  private const string Secret = "xai-playwright-3799";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ClearXaiApiKey_Given_Wasm>();

  public static async Task ClearKey_Should_Ask_And_LeaveTheKey_Until_Confirmed()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
    {
      services.PostConfigure<XaiChatOptions>(options => options.ApiKey = "");
      services.RemoveAll<IProtectedSecretStore>();
      services.AddSingleton<IProtectedSecretStore, MemoryProtectedSecretStore>();
    });

    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
    await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
      Locale = "en-US",
    });
    IPage page = await context.NewPageAsync();

    await SignInWithNewPasskeyAsync(page, context);
    await page.GotoAsync(InProcTestPorts.WebHostUrl.TrimEnd('/') + "/Admin/AI", new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });

    ILocator input = page.Locator("[data-qa=XaiApiKeyInput]");
    try
    {
      await input.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
    }
    catch (TimeoutException exception)
    {
      string text = await page.Locator("body").InnerTextAsync();
      throw new TimeoutException($"Admin AI key field was not shown at {page.Url}. Page:\n{text}", exception);
    }

    await Assertions.Expect(page.Locator("[data-qa=XaiByokMode]")).ToBeVisibleAsync();
    await PlaywrightPageLoad.GotoAsync(page, "/Admin/Authentication");
    await Assertions.Expect(page.Locator("[data-qa=AuthenticationSave]")).ToBeVisibleAsync();
    await Assertions.Expect(page.Locator("[data-qa=XaiByokModeSettings]")).ToHaveCountAsync(0);
    await page.GotoAsync(InProcTestPorts.WebHostUrl.TrimEnd('/') + "/Admin/AI", new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    await input.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });

    await input.Locator("input").FillAsync(Secret);
    await page.Locator("[data-qa=XaiApiKeySave]").ClickAsync();
    ILocator status = page.Locator("[data-qa=XaiApiKeyStatus]");
    await Assertions.Expect(status).ToContainTextAsync("3799", new LocatorAssertionsToContainTextOptions { Timeout = 30_000 });

    await page.Locator("[data-qa=XaiApiKeyClear]").ClickAsync();
    ILocator dialog = page.Locator("fluent-dialog");
    await Assertions.Expect(dialog.GetByText("Clear the xAI API key?")).ToBeVisibleAsync(
      new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
    await Assertions.Expect(dialog.GetByText("Ask stops working until a new key is saved.")).ToBeVisibleAsync();

    await page.Keyboard.PressAsync("Enter");
    await page.WaitForTimeoutAsync(800);
    await Assertions.Expect(dialog.GetByText("Clear the xAI API key?")).ToBeVisibleAsync();
    await Assertions.Expect(status).ToContainTextAsync("3799");

    await dialog.GetByText("Cancel", new LocatorGetByTextOptions { Exact = true }).ClickAsync();
    await Assertions.Expect(dialog).ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = 10_000 });
    await Assertions.Expect(status).ToContainTextAsync("3799");

    await page.Locator("[data-qa=XaiApiKeyClear]").ClickAsync();
    await Assertions.Expect(dialog.GetByText("Clear the xAI API key?")).ToBeVisibleAsync(
      new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
    await page.Keyboard.PressAsync("Escape");
    await Assertions.Expect(dialog).ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = 10_000 });
    await Assertions.Expect(status).ToContainTextAsync("3799");

    await page.Locator("[data-qa=XaiApiKeyClear]").ClickAsync();
    await Assertions.Expect(dialog.GetByText("Clear the xAI API key?")).ToBeVisibleAsync(
      new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
    await dialog.GetByText("Clear key", new LocatorGetByTextOptions { Exact = true }).ClickAsync();
    await Assertions.Expect(dialog).ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = 10_000 });
    await Assertions.Expect(status).ToContainTextAsync("No key stored.", new LocatorAssertionsToContainTextOptions { Timeout = 30_000 });
    await Assertions.Expect(page.Locator("[data-qa=XaiApiKeyClear]")).ToHaveCountAsync(0);
    await Assertions.Expect(page.Locator("[data-qa=MessageBars]")).ToContainTextAsync("xAI API key cleared.");

    await GrantDeveloperAsync(graph);
    await page.GotoAsync(InProcTestPorts.WebHostUrl.TrimEnd('/') + "/DesignSystem/confirmations", new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    ILocator sample = page.Locator("[data-qa=ConfirmSample]");
    await sample.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
    await sample.ClickAsync();
    await Assertions.Expect(dialog.GetByText("Confirm this sample?")).ToBeVisibleAsync(
      new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
    await page.Keyboard.PressAsync("Escape");
    await Assertions.Expect(dialog).ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = 10_000 });
    await Assertions.Expect(page.GetByText("Sample confirmed.")).ToHaveCountAsync(0);

    await sample.ClickAsync();
    await Assertions.Expect(dialog.GetByText("Confirm this sample?")).ToBeVisibleAsync(
      new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
    await dialog.GetByText("Confirm", new LocatorGetByTextOptions { Exact = true }).ClickAsync();
    await Assertions.Expect(page.Locator("[data-qa=MessageBars]")).ToContainTextAsync(
      "Sample confirmed.",
      new LocatorAssertionsToContainTextOptions { Timeout = 10_000 });
  }

  public static async Task HostKey_Should_Show_The_Policy_As_Having_No_Effect()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
      services.PostConfigure<XaiChatOptions>(options => options.ApiKey = "playwright-host-key"));
    graph.Web.ShouldNotBeNull();

    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
    await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
      Locale = "en-US",
    });
    IPage page = await context.NewPageAsync();
    await SignInWithNewPasskeyAsync(page, context);
    await page.GotoAsync(InProcTestPorts.WebHostUrl.TrimEnd('/') + "/Admin/AI", new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    await Assertions.Expect(page.Locator("[data-qa=XaiApiKeyManagedByHost]")).ToBeVisibleAsync(
      new LocatorAssertionsToBeVisibleOptions { Timeout = 120_000 });
    await Assertions.Expect(page.Locator("[data-qa=XaiByokMode]")).ToHaveCountAsync(0);
    await Assertions.Expect(page.Locator("[data-qa=XaiByokModeReadOnly]")).ToBeVisibleAsync();
    await Assertions.Expect(page.Locator("[data-qa=XaiByokModeHostReason]")).ToContainTextAsync(
      "this policy has no effect");
  }

  private static async Task GrantDeveloperAsync(HostGraph graph)
  {
    await using AsyncServiceScope scope = graph.Web!.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalStore principals = scope.ServiceProvider.GetRequiredService<IPrincipalStore>();
    IPrincipalRoleStore roles = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    IReadOnlyList<Principal> all = await principals.ListPrincipalsAsync();
    Principal? administrator = null;
    foreach (Principal principal in all)
    {
      IReadOnlyList<Guid> current = await roles.GetRoleIdsAsync(principal.Id);
      if (!current.Contains(RoleIds.Administrator))
      {
        continue;
      }

      administrator = principal;
      if (!current.Contains(RoleIds.Developer))
      {
        await roles.SetRoleIdsAsync(principal.Id, [.. current, RoleIds.Developer]);
      }

      break;
    }

    administrator.ShouldNotBeNull("the first passkey account claims Administrator");
  }

  private static async Task SignInWithNewPasskeyAsync(IPage page, IBrowserContext context)
  {
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

    await page.GotoAsync(InProcTestPorts.WebHostUrl, new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    await page.Locator(".twe-appbar").WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
    await page.Locator("[data-qa=HomeSignIn]").ClickAsync();
    await page.WaitForURLAsync("**/Login**", new PageWaitForURLOptions { Timeout = 30_000 });
    await page.Locator("[data-qa=CreatePasskey]").ClickAsync();
    await page.WaitForURLAsync("**/Settings", new PageWaitForURLOptions { Timeout = 60_000 });
    await page.Locator(".twe-appbar").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
  }

  /// <summary>In-memory secret store so Admin → AI can save a key without Postgres.</summary>
  private sealed class MemoryProtectedSecretStore : IProtectedSecretStore
  {
    private readonly Dictionary<string, (string Plaintext, ProtectedSecretRecord Record)> Rows = [];

    public bool StorageAvailable => true;

    public Task<ProtectedSecretRecord?> FindAsync(string name, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      lock (Rows)
      {
        return Task.FromResult(Rows.TryGetValue(name, out (string Plaintext, ProtectedSecretRecord Record) row)
          ? row.Record
          : null);
      }
    }

    public Task<ProtectedSecretWrite> SetAsync(
      string name,
      string purpose,
      string plaintext,
      Guid setByPrincipalId,
      DateTimeOffset setAt,
      CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      lock (Rows)
      {
        bool inserted = !Rows.ContainsKey(name);
        Rows[name] = (plaintext, new ProtectedSecretRecord(
          name,
          purpose,
          ProtectedSecretSuffix.From(plaintext),
          setAt,
          setByPrincipalId));
        return Task.FromResult(inserted ? ProtectedSecretWrite.Inserted : ProtectedSecretWrite.Replaced);
      }
    }

    public Task<string?> UnprotectAsync(string name, string purpose, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      lock (Rows)
      {
        if (!Rows.TryGetValue(name, out (string Plaintext, ProtectedSecretRecord Record) row)
          || !string.Equals(row.Record.Purpose, purpose, StringComparison.Ordinal))
        {
          return Task.FromResult<string?>(null);
        }

        return Task.FromResult<string?>(row.Plaintext);
      }
    }

    public Task<bool> ClearAsync(string name, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      lock (Rows)
      {
        return Task.FromResult(Rows.Remove(name));
      }
    }
  }
}
