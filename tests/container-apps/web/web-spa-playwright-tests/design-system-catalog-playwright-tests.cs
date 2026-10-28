#region Purpose
// Browser proof that the design-system catalog deep-links, filters, and rejects an unknown slug.
#endregion

#region Design
// Same host shape as the drawer proof: in-process Web.Server, shared Chromium, en-US locale.
// The design system requires developer.access. The first passkey account is Administrator, so the
// test adds the Developer role and then fully navigates. Each case is a document load or a
// real click, not a static render. The filter targets the shadow input of the catalog text field.
// Group names are headings inside the catalog, and each nested list names its heading.
// Expected part titles are Part.Title in DesignSystemParts.PageOrder.
// Expected group headings are the Group names in declaration order, for groups that have a part.
// The catalog writes that name with @group.
#endregion

namespace DesignSystemCatalogPlaywright_;

using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.DesignSystem;
using TimeWarp.Identity;

public class DesignSystemCatalog_Given_Wasm
{
  private static readonly string[] PartTitles =
    [.. DesignSystemParts.PageOrder.Select(static part => part.Title)];

  private static readonly string[] GroupNames = GroupHeadings();

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<DesignSystemCatalog_Given_Wasm>();

  public static async Task Catalog_Should_DeepLink_Filter_And_Reject_An_Unknown_Slug()
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

    await GotoAsync(page, "/DesignSystem/drawers");
    ILocator catalog = page.Locator("[data-qa=DesignSystemCatalog]");
    ILocator stage = page.Locator("[data-qa=DesignSystemStage]");
    ILocator titles = stage.Locator("h2.twe-card__title");
    await titles.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
    await Assertions.Expect(titles).ToHaveCountAsync(1);
    await Assertions.Expect(titles).ToHaveTextAsync("Drawers");
    await Assertions.Expect(page.Locator("[data-qa=DesignSystemUsage]")).ToBeVisibleAsync();
    await Assertions.Expect(page.Locator("[data-qa=DesignSystemPartLink][data-part=drawers]"))
      .ToHaveAttributeAsync("aria-current", "page");
    await Assertions.Expect(page.Locator("[data-qa=DesignSystemAll]")).Not.ToHaveAttributeAsync("aria-current", "page");
    await ExpectGroupHeadingsAsync(catalog, GroupNames);
    await ExpectDesignSystemNavOpenAsync(page);

    await GotoAsync(page, "/DesignSystem/brand");
    await ExpectDesignSystemNavOpenAsync(page);
    await Assertions.Expect(titles).ToHaveTextAsync("Brand");

    await GotoAsync(page, "/DesignSystem/page-header");
    await Assertions.Expect(page.Locator("h1")).ToHaveCountAsync(1);
    await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Design System");
    await Assertions.Expect(page.Locator("[data-qa=PageHeaderExample] h1")).ToHaveCountAsync(0);
    await Assertions.Expect(page.Locator("[data-qa=PageHeaderSync]")).ToContainTextAsync("Sync");
    await Assertions.Expect(page.Locator("[data-qa=PageHeaderAdd]")).ToContainTextAsync("Add item");
    await Assertions.Expect(page.Locator("[data-qa=PageHeaderRule]")).ToContainTextAsync("FormActions");
    await ExpectDesignSystemNavOpenAsync(page);

    await GotoAsync(page, "/DesignSystem");
    await Assertions.Expect(page.Locator("[data-qa=DesignSystemAll]")).ToHaveAttributeAsync("aria-current", "page");
    await Assertions.Expect(page.Locator("[data-qa=DesignSystemUsage]")).ToHaveCountAsync(0);
    await Assertions.Expect(titles).ToHaveCountAsync(PartTitles.Length);
    IReadOnlyList<string> shown = await titles.AllInnerTextsAsync();
    shown.ShouldBe(PartTitles);
    double rowHeight = await page.EvaluateAsync<double>(
      "() => document.querySelector('[data-qa=DesignSystemPartLink]').getBoundingClientRect().height");
    rowHeight.ShouldBeInRange(26, 32);
    await ExpectGroupHeadingsAsync(catalog, GroupNames);
    await Assertions.Expect(catalog.Locator("ul.ds-catalog__parts")).ToHaveCountAsync(GroupNames.Length);
    await Assertions.Expect(catalog.Locator("ul.ds-catalog__parts[aria-labelledby='ds-catalog-layout']"))
      .ToHaveAttributeAsync("aria-labelledby", "ds-catalog-layout");

    ILocator filter = page.Locator("[data-qa=DesignSystemFilter] input");
    await filter.ClickAsync();
    await filter.PressSequentiallyAsync("Drawers");
    await Assertions.Expect(page.Locator("[data-qa=DesignSystemPartLink]")).ToHaveCountAsync(1);
    await Assertions.Expect(page.Locator("[data-qa=DesignSystemPartLink]")).ToHaveAttributeAsync("data-part", "drawers");
    await Assertions.Expect(page.Locator("[data-qa=DesignSystemAll]")).ToHaveCountAsync(0);
    await ExpectGroupHeadingsAsync(catalog, ["Layout"]);
    await Assertions.Expect(catalog.Locator("ul.ds-catalog__parts"))
      .ToHaveAttributeAsync("aria-labelledby", "ds-catalog-layout");

    await filter.FillAsync("");
    await Assertions.Expect(page.Locator("[data-qa=DesignSystemAll]")).ToHaveCountAsync(1);
    await Assertions.Expect(page.Locator("[data-qa=DesignSystemPartLink]")).ToHaveCountAsync(PartTitles.Length);
    await ExpectGroupHeadingsAsync(catalog, GroupNames);

    await page.Locator("[data-qa=DesignSystemPartLink][data-part=color-tokens]").ClickAsync();
    await page.WaitForURLAsync(
      static url => url.EndsWith("/DesignSystem/color-tokens", StringComparison.Ordinal),
      new PageWaitForURLOptions { Timeout = 30_000 });
    await Assertions.Expect(titles).ToHaveTextAsync("Color Tokens");
    await Assertions.Expect(page.Locator("[data-qa=DesignSystemPartLink][data-part=color-tokens]"))
      .ToHaveAttributeAsync("aria-current", "page");

    await page.GoBackAsync(new PageGoBackOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
    await page.WaitForURLAsync(
      static url => url.EndsWith("/DesignSystem", StringComparison.Ordinal),
      new PageWaitForURLOptions { Timeout = 30_000 });
    await Assertions.Expect(page.Locator("[data-qa=DesignSystemAll]")).ToHaveAttributeAsync("aria-current", "page");
    await Assertions.Expect(titles).ToHaveCountAsync(PartTitles.Length);

    await GotoAsync(page, "/DesignSystem/no-such-part");
    await Assertions.Expect(page.Locator("[data-qa=DesignSystemUnknown]")).ToContainTextAsync("No such part.");
    await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Design System");
    await Assertions.Expect(page.Locator("[data-qa=DesignSystemUsage]")).ToHaveCountAsync(0);
    await Assertions.Expect(titles).ToHaveCountAsync(0);
    await page.Locator("[data-qa=DesignSystemUnknownAll]").ClickAsync();
    await page.WaitForURLAsync(
      static url => url.EndsWith("/DesignSystem", StringComparison.Ordinal),
      new PageWaitForURLOptions { Timeout = 30_000 });
    await Assertions.Expect(page.Locator("[data-qa=DesignSystemAll]")).ToHaveAttributeAsync("aria-current", "page");
  }

  private static string[] GroupHeadings()
  {
    List<string> names = [];
    foreach (DesignSystemParts.Group group in Enum.GetValues<DesignSystemParts.Group>())
    {
      if (Array.Exists(DesignSystemParts.PageOrder, part => part.Group == group))
      {
        names.Add(group.ToString());
      }
    }

    return [.. names];
  }

  private static async Task ExpectGroupHeadingsAsync(ILocator catalog, IReadOnlyList<string> names)
  {
    await Assertions.Expect(catalog.GetByRole(AriaRole.Heading)).ToHaveCountAsync(names.Count);
    foreach (string name in names)
    {
      await Assertions.Expect(catalog.GetByRole(AriaRole.Heading, new() { Name = name, Exact = true }))
        .ToHaveCountAsync(1);
    }
  }

  private static async Task ExpectDesignSystemNavOpenAsync(IPage page)
  {
    const string probe =
      """
      () => {
        const item = document.querySelector('a[href="/DesignSystem"]');
        const group = item && item.closest('.fluent-navsubitemgroup');
        const active = item !== null && item.classList.contains('active');
        const expanded = group !== null && group.classList.contains('expanded');
        return JSON.stringify({
          open: active && expanded,
          active,
          expanded,
          group: group ? group.className : null,
        });
      }
      """;
    try
    {
      await page.WaitForFunctionAsync(
        """
        () => {
          const item = document.querySelector('a[href="/DesignSystem"]');
          const group = item && item.closest('.fluent-navsubitemgroup');
          return item !== null && item.classList.contains('active')
            && group !== null && group.classList.contains('expanded');
        }
        """,
        null,
        new PageWaitForFunctionOptions { Timeout = 30_000 });
    }
    catch (TimeoutException exception)
    {
      string state = await page.EvaluateAsync<string>(probe);
      throw new TimeoutException(
        "the Design System nav item stays active and its group stays open: " + state,
        exception);
    }
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
