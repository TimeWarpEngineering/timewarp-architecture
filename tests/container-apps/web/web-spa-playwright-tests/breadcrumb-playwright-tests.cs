#region Purpose
// Browser proof that the shell breadcrumb is the page's location, and that the trail styles apply.
#endregion

#region Design
// Same host as the style-guide catalog: in-process Web.Server, shared Chromium, en-US.
// The style guide needs developer.access, so the first passkey account (Administrator) also
// receives Developer before any navigation. Ctrl-K has no style-guide part rows, so the
// palette case opens Style Guide from Counter and then clicks Color Tokens. The trail must
// not keep Counter. A part-to-part click keeps one part crumb. The role editor crumb is the
// role name once the list has loaded. Computed color and margin read the isolated rules.
#endregion

namespace BreadcrumbPlaywright_;

using TimeWarp.Architecture.Features;
using TimeWarp.Identity;

public class Breadcrumb_Given_Wasm
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Breadcrumb_Given_Wasm>();

  public static async Task Location_Trail_Should_Stay_The_Same_For_Every_Route_To_A_Page()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync();
    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
    await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
      Locale = "en-US",
    });
    IPage page = await context.NewPageAsync();

    await SignInWithNewPasskeyAsync(page, context);
    await GrantDeveloperAsync(graph);

    await GotoAsync(page, "/StyleGuide/color-tokens");
    await ExpectTrailAsync(page, "Color Tokens", "Home", "Style Guide");
    await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
    await ExpectTrailAsync(page, "Color Tokens", "Home", "Style Guide");
    await ExpectTrailStylesAsync(page);

    await page.Locator("[data-qa=StyleGuidePartLink][data-part=drawers]").ClickAsync();
    await page.WaitForURLAsync(
      static url => url.EndsWith("/StyleGuide/drawers", StringComparison.Ordinal),
      new PageWaitForURLOptions { Timeout = 30_000 });
    await ExpectTrailAsync(page, "Drawers", "Home", "Style Guide");

    await page.Locator("[data-qa=StyleGuidePartLink][data-part=color-tokens]").ClickAsync();
    await page.WaitForURLAsync(
      static url => url.EndsWith("/StyleGuide/color-tokens", StringComparison.Ordinal),
      new PageWaitForURLOptions { Timeout = 30_000 });
    await ExpectTrailAsync(page, "Color Tokens", "Home", "Style Guide");

    await GotoAsync(page, "/Counter");
    await OpenStyleGuideFromPaletteAsync(page);
    await ExpectTrailAsync(page, "Style Guide", "Home");
    await page.Locator("[data-qa=StyleGuidePartLink][data-part=color-tokens]").ClickAsync();
    await page.WaitForURLAsync(
      static url => url.EndsWith("/StyleGuide/color-tokens", StringComparison.Ordinal),
      new PageWaitForURLOptions { Timeout = 30_000 });
    await ExpectTrailAsync(page, "Color Tokens", "Home", "Style Guide");
    (await page.Locator("[data-qa=Breadcrumb]").InnerTextAsync()).ShouldNotContain("Counter");

    await GotoAsync(page, "/Admin/Roles");
    ILocator administrator = page.Locator("[data-qa=RoleDetail]").Filter(new LocatorFilterOptions { HasText = "Administrator" });
    await administrator.ClickAsync();
    await page.WaitForURLAsync(
      static url => url.Contains("/Admin/Roles/", StringComparison.OrdinalIgnoreCase),
      new PageWaitForURLOptions { Timeout = 30_000 });
    await Assertions.Expect(page.Locator("[data-qa=BreadcrumbCurrent]")).ToHaveTextAsync
    (
      "Administrator",
      new LocatorAssertionsToHaveTextOptions { Timeout = 30_000 }
    );
    await ExpectTrailAsync(page, "Administrator", "Home", "Roles");
  }

  private static async Task ExpectTrailAsync(IPage page, string current, params string[] links)
  {
    ILocator nav = page.Locator("[data-qa=Breadcrumb]");
    await Assertions.Expect(nav).ToHaveAttributeAsync("aria-label", "Breadcrumb");
    ILocator linkLocators = nav.Locator("[data-qa=BreadcrumbLink]");
    await Assertions.Expect(linkLocators).ToHaveCountAsync(links.Length);
    for (int index = 0; index < links.Length; index++)
    {
      await Assertions.Expect(linkLocators.Nth(index)).ToHaveTextAsync(links[index]);
    }

    ILocator currentLocator = nav.Locator("[data-qa=BreadcrumbCurrent]");
    await Assertions.Expect(currentLocator).ToHaveCountAsync(1);
    await Assertions.Expect(currentLocator).ToHaveTextAsync(current);
    await Assertions.Expect(currentLocator).ToHaveAttributeAsync("aria-current", "page");
  }

  private static async Task ExpectTrailStylesAsync(IPage page)
  {
    string linkColor = await page.EvaluateAsync<string>
    (
      "() => getComputedStyle(document.querySelector('[data-qa=Breadcrumb] a')).color"
    );
    linkColor.ShouldBe("rgb(0, 133, 178)");

    string currentColor = await page.EvaluateAsync<string>
    (
      "() => getComputedStyle(document.querySelector('[data-qa=BreadcrumbCurrent]')).color"
    );
    currentColor.ShouldBe("rgb(59, 63, 77)");

    string marginBottom = await page.EvaluateAsync<string>
    (
      "() => getComputedStyle(document.querySelector('[data-qa=Breadcrumb]')).marginBottom"
    );
    marginBottom.ShouldBe("12px");
  }

  private static async Task OpenStyleGuideFromPaletteAsync(IPage page)
  {
    ILocator input = page.Locator("[data-qa=CommandPaletteInput]");
    bool opened = false;
    for (int attempt = 0; attempt < 20 && !opened; attempt++)
    {
      await page.Keyboard.PressAsync("Control+k");
      try
      {
        await input.WaitForAsync(new LocatorWaitForOptions { Timeout = 2_000, State = WaitForSelectorState.Visible });
        opened = true;
      }
      catch (TimeoutException)
      {
        await page.Keyboard.PressAsync("Escape");
      }
    }

    opened.ShouldBeTrue("Ctrl-K did not open the palette");
    await input.FillAsync("style guide");
    ILocator active = page.Locator(".twe-palette__row--active");
    await Assertions.Expect(active.Locator(".twe-palette__kind")).ToHaveTextAsync
    (
      "Page",
      new LocatorAssertionsToHaveTextOptions { Timeout = 10_000 }
    );
    await Assertions.Expect(active.Locator(".twe-palette__name")).ToHaveTextAsync
    (
      "Style Guide",
      new LocatorAssertionsToHaveTextOptions { Timeout = 10_000 }
    );
    await input.PressAsync("Enter");
    await page.WaitForURLAsync(
      static url => url.EndsWith("/StyleGuide", StringComparison.Ordinal),
      new PageWaitForURLOptions { Timeout = 30_000 });
  }

  private static async Task GotoAsync(IPage page, string path)
  {
    await page.GotoAsync(InProcTestPorts.WebHostUrl.TrimEnd('/') + path, new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
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
}
