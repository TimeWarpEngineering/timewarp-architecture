#region Purpose
// Browser proof that the shell breadcrumb is the page's location, and that the trail styles apply.
#endregion

#region Design
// Same host as the design-system catalog: in-process Web.Server, shared Chromium, en-US.
// The design system needs developer.access, so the first passkey account (Administrator) also
// receives Developer before any navigation. Ctrl-K lists each design-system part, so one case
// opens Color Tokens from the palette. Another opens Design System, then clicks Color Tokens.
// The trail must not keep Counter. A part-to-part click keeps one part crumb. The role editor
// crumb is the role name once the list has loaded. That case requires the Roles item to exist
// in the menu (not the Roles crumb, which uses the same href) and not be active, and the Admin
// group to be expanded. Computed color and margin read the isolated
// rules. The first crumb's focus ring must sit inside the trail list, which still ellipsizes.
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

    await PlaywrightPageLoad.GotoAsync(page, "/DesignSystem/color-tokens");
    await ExpectTrailAsync(page, "Color Tokens", "Home", "Design System");
    await PlaywrightPageLoad.ReloadAsync(page);
    await ExpectTrailAsync(page, "Color Tokens", "Home", "Design System");
    await ExpectTrailStylesAsync(page);
    await ExpectFocusRingInsideTheTrailAsync(page);

    await page.Locator("[data-qa=DesignSystemPartLink][data-part=drawers]").ClickAsync();
    await page.WaitForURLAsync(
      static url => url.EndsWith("/DesignSystem/drawers", StringComparison.Ordinal),
      new PageWaitForURLOptions { Timeout = 30_000 });
    await ExpectTrailAsync(page, "Drawers", "Home", "Design System");

    await page.Locator("[data-qa=DesignSystemPartLink][data-part=color-tokens]").ClickAsync();
    await page.WaitForURLAsync(
      static url => url.EndsWith("/DesignSystem/color-tokens", StringComparison.Ordinal),
      new PageWaitForURLOptions { Timeout = 30_000 });
    await ExpectTrailAsync(page, "Color Tokens", "Home", "Design System");

    await PlaywrightPageLoad.GotoAsync(page, "/Counter");
    await OpenDesignSystemFromPaletteAsync(page);
    await ExpectTrailAsync(page, "Design System", "Home");
    await PlaywrightPageLoad.GotoAsync(page, "/Counter");
    await OpenPartFromPaletteAsync(page);
    await ExpectTrailAsync(page, "Color Tokens", "Home", "Design System");
    await page.Locator("[data-qa=DesignSystemPartLink][data-part=color-tokens]").ClickAsync();
    await page.WaitForURLAsync(
      static url => url.EndsWith("/DesignSystem/color-tokens", StringComparison.Ordinal),
      new PageWaitForURLOptions { Timeout = 30_000 });
    await ExpectTrailAsync(page, "Color Tokens", "Home", "Design System");
    (await page.Locator("[data-qa=Breadcrumb]").InnerTextAsync()).ShouldNotContain("Counter");

    await PlaywrightPageLoad.GotoAsync(page, "/Admin/Roles");
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
    ILocator rolesLink = page.Locator(".twe-nav-menu a[href='/Admin/Roles']");
    await Assertions.Expect(rolesLink).ToHaveCountAsync(1);
    string rolesClass = await rolesLink.GetAttributeAsync("class") ?? "";
    rolesClass.Split(' ', StringSplitOptions.RemoveEmptyEntries).ShouldNotContain("active");
    ILocator adminGroup = page.Locator("button[aria-expanded='true']")
      .GetByText("Admin", new LocatorGetByTextOptions { Exact = true });
    await Assertions.Expect(adminGroup).ToHaveCountAsync(1);
    await Assertions.Expect(adminGroup).ToBeVisibleAsync();
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

  private static async Task ExpectFocusRingInsideTheTrailAsync(IPage page)
  {
    string within = await page.EvaluateAsync<string>(
      """
      () => {
        const link = document.querySelector('[data-qa=BreadcrumbLink]');
        link.focus({ focusVisible: true });
        const linkRect = link.getBoundingClientRect();
        const style = getComputedStyle(link);
        const width = parseFloat(style.outlineWidth) || 0;
        const offset = Math.max(parseFloat(style.outlineOffset) || 0, 0);
        const expand = width + offset;
        const list = link.closest('ol');
        const clip = list.getBoundingClientRect();
        const listStyle = getComputedStyle(list);
        const inside = linkRect.left - expand >= clip.left - 0.5
          && linkRect.top - expand >= clip.top - 0.5
          && linkRect.right + expand <= clip.right + 0.5
          && linkRect.bottom + expand <= clip.bottom + 0.5;
        return JSON.stringify({
          inside,
          overflow: listStyle.overflowX,
          textOverflow: listStyle.textOverflow,
          whiteSpace: listStyle.whiteSpace,
          outlineWidth: style.outlineWidth,
          outlineOffset: style.outlineOffset,
        });
      }
      """);
    using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(within);
    document.RootElement.GetProperty("inside").GetBoolean().ShouldBeTrue(within);
    document.RootElement.GetProperty("overflow").GetString().ShouldBe("hidden");
    document.RootElement.GetProperty("textOverflow").GetString().ShouldBe("ellipsis");
    document.RootElement.GetProperty("whiteSpace").GetString().ShouldBe("nowrap");
    document.RootElement.GetProperty("outlineWidth").GetString().ShouldBe("2px");
    document.RootElement.GetProperty("outlineOffset").GetString().ShouldBe("2px");
  }

  private static async Task OpenDesignSystemFromPaletteAsync(IPage page)
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
    await input.FillAsync("design system");
    ILocator active = page.Locator(".twe-palette__row--active");
    await Assertions.Expect(active.Locator(".twe-palette__kind")).ToHaveTextAsync
    (
      "Page",
      new LocatorAssertionsToHaveTextOptions { Timeout = 10_000 }
    );
    await Assertions.Expect(active.Locator(".twe-palette__name")).ToHaveTextAsync
    (
      "Design System",
      new LocatorAssertionsToHaveTextOptions { Timeout = 10_000 }
    );
    await input.PressAsync("Enter");
    await page.WaitForURLAsync(
      static url => url.EndsWith("/DesignSystem", StringComparison.Ordinal),
      new PageWaitForURLOptions { Timeout = 30_000 });
  }

  private static async Task OpenPartFromPaletteAsync(IPage page)
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
    await input.FillAsync("color tokens");
    ILocator active = page.Locator(".twe-palette__row--active");
    await Assertions.Expect(active.Locator(".twe-palette__name")).ToHaveTextAsync(
      "Design System: Color Tokens",
      new LocatorAssertionsToHaveTextOptions { Timeout = 10_000 });
    await input.PressAsync("Enter");
    await page.WaitForURLAsync(
      static url => url.EndsWith("/DesignSystem/color-tokens", StringComparison.Ordinal),
      new PageWaitForURLOptions { Timeout = 30_000 });
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
