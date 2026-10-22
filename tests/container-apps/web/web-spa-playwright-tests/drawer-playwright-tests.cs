#region Purpose
// Browser proof that the style-guide drawer moves focus in, keeps Tab inside, and returns
// focus to the opener on Escape.
#endregion

#region Design
// Same host shape as AskFocus_Given_Wasm: in-process Web.Server, shared Chromium, en-US locale.
// The style guide requires developer.access. The SPA does not use mock auth, so the test signs
// in with a virtual passkey the way AskSignIn_Given_Wasm does. That first account is
// Administrator, which does not include developer.access, so the test adds the Developer role
// on the host store and loads /StyleGuide with a full navigation. Focus checks retry because
// showModal runs after the dialog's script binds.
#endregion

namespace StyleGuideDrawerPlaywright_;

using TimeWarp.Architecture.Features;
using TimeWarp.Identity;

public class StyleGuideDrawer_Given_Wasm
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<StyleGuideDrawer_Given_Wasm>();

  public static async Task Open_Should_MoveFocusIn_TrapTab_And_ReturnFocusOnEscape()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync();
    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
    await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
      // The host locale can be en-us@posix, which .NET WASM rejects and never renders the shell.
      Locale = "en-US",
    });
    IPage page = await context.NewPageAsync();

    await SignInWithNewPasskeyAsync(page, context);
    await GrantDeveloperAsync(graph);
    await page.GotoAsync(InProcTestPorts.WebHostUrl.TrimEnd('/') + "/StyleGuide", new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });

    ILocator opener = page.Locator("[data-qa=DrawerOpen]");
    try
    {
      await opener.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
    }
    catch (TimeoutException exception)
    {
      string text = await page.Locator("body").InnerTextAsync();
      throw new TimeoutException($"Style guide drawer opener was not shown at {page.Url}. Page:\n{text}", exception);
    }

    await opener.ClickAsync();
    ILocator openDialog = page.Locator("dialog.twe-drawer[open]");
    await Assertions.Expect(openDialog).ToHaveCountAsync(1, new LocatorAssertionsToHaveCountOptions { Timeout = 30_000 });

    ILocator headerClose = page.Locator("[data-qa=DrawerClose]");
    ILocator footerClose = page.Locator("[data-qa=DrawerFooterClose]");
    await Assertions.Expect(headerClose).ToBeFocusedAsync(new LocatorAssertionsToBeFocusedOptions { Timeout = 30_000 });

    List<string> stops = [];
    bool sawFooter = false;
    bool returnedToHeader = false;
    for (int step = 0; step < 6 && !returnedToHeader; step++)
    {
      await page.Keyboard.PressAsync("Tab");
      string where = await FocusedStopAsync(page);
      stops.Add(where);
      where.EndsWith("|inside", StringComparison.Ordinal)
        .ShouldBeTrue($"Tab left the drawer: {string.Join(" -> ", stops)}");
      if (where.Contains("|DrawerFooterClose|", StringComparison.Ordinal))
      {
        sawFooter = true;
      }

      if (sawFooter && where.Contains("|DrawerClose|", StringComparison.Ordinal))
      {
        returnedToHeader = true;
      }
    }

    sawFooter.ShouldBeTrue($"Tab never reached the footer close button: {string.Join(" -> ", stops)}");
    returnedToHeader.ShouldBeTrue($"Tab never wrapped back to the header close button: {string.Join(" -> ", stops)}");
    await Assertions.Expect(opener).Not.ToBeFocusedAsync();

    await page.Keyboard.PressAsync("Escape");
    await Assertions.Expect(openDialog).ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = 10_000 });
    await Assertions.Expect(opener).ToBeFocusedAsync(new LocatorAssertionsToBeFocusedOptions { Timeout = 10_000 });
  }

  private static Task<string> FocusedStopAsync(IPage page) =>
    page.EvaluateAsync<string>(
      """
      () => {
        const dialog = document.querySelector("dialog.twe-drawer");
        let node = document.activeElement;
        let inside = false;
        while (node) {
          if (node === dialog) {
            inside = true;
            break;
          }

          const root = node.getRootNode();
          if (root instanceof ShadowRoot) {
            node = root.host;
          } else {
            node = node.parentElement;
          }
        }

        const active = document.activeElement;
        const qa = active && "getAttribute" in active ? active.getAttribute("data-qa") ?? "" : "";
        const tag = active ? active.tagName : "none";
        return `${tag}|${qa}|${inside ? "inside" : "outside"}`;
      }
      """);

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
}
