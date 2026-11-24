#region Purpose
// Browser proof that New Role is create-only and that the role edit form is on the role detail page.
#endregion

#region Design
// In-process Web.Server, shared Chromium, en-US, a virtual passkey. The first account is
// Administrator, so it holds admin.roles.manage. /Admin/Roles/New shows RoleForm (RoleName,
// RoleSave) and no RoleEditForm (no RoleEditName, no RoleEditEmpty). The Administrator detail
// page /Admin/Roles/{RoleIds.Administrator} shows RoleEditForm bound to the stored role and
// no create form; Cancel copies the stored name back over an unsaved edit. Each page is shot
// to test output (PlaywrightScreenshot); task 507 copied those shots into its evidence folder.
// Task 512: the detail page renders permissions and agent scopes through PermissionTree. The
// Administrator admin.* rows are locked (disabled, checked) and so is their parent. A child row
// is at most 32px tall and a parent row at most 36px. Clicking a child flips it and makes its
// parent mixed; ArrowDown and ArrowUp move focus between rows; Space toggles the focused row.
// Nothing is saved. RowBoxScript outlines each row and prints its height for the evidence shots,
// taken light, then dark after an OS switch (nothing stored, so Fluent follows it). The
// design-system shot needs developer.access, so the test grants it before that load.
// Task 506: the walk starts on /Admin/Roles. New Role is a FluentButton that dispatches
// RoleState.OpenNewRole (no Href, TWA0050); the list is shot, then the click must land on
// /Admin/Roles/New.
#endregion

namespace RolePagesPlaywright_;

using DesignSystemDrawerPlaywright_;
using TimeWarp.Architecture.Features;

public class RolePages_Given_Wasm
{
  private const string RowBoxScript =
    """
    () => {
      for (const row of document.querySelectorAll('.twe-perm-tree__parent, .twe-perm-tree__item')) {
        row.style.outline = '1px dashed #d33';
        row.style.position = 'relative';
        const h = Math.round(row.getBoundingClientRect().height);
        const tag = document.createElement('span');
        tag.className = 't512-row-height';
        tag.textContent = h + 'px';
        tag.style.cssText = 'position:absolute;right:0;top:0;font:11px monospace;color:#d33;background:transparent';
        row.appendChild(tag);
      }
    }
    """;

  private const string RowHeightsScript =
    """
    () => [
      Math.max(...[...document.querySelectorAll('.twe-perm-tree__parent')].map(r => r.getBoundingClientRect().height)),
      Math.max(...[...document.querySelectorAll('.twe-perm-tree__item')].map(r => r.getBoundingClientRect().height)),
    ]
    """;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RolePages_Given_Wasm>();

  public static async Task NewRole_Should_BeCreateOnly_And_DetailPage_Should_EditTheRole()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync();
    graph.Web.ShouldNotBeNull();

    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
    await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
      Locale = "en-US",
    });
    IPage page = await context.NewPageAsync();

    await DrawerCloseAsserts.SignInWithNewPasskeyAsync(page, context);

    await PlaywrightPageLoad.GotoAsync(page, "/Admin/Roles");
    ILocator newRole = page.Locator("[data-qa=NewRole]");
    await Assertions.Expect(newRole).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
    await Assertions.Expect(page.Locator("[data-qa=RoleDetail]").First).ToBeVisibleAsync(
      new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
    (await newRole.GetAttributeAsync("href")).ShouldBeNull();
    await page.ScreenshotAsync(new PageScreenshotOptions
    {
      Path = PlaywrightScreenshot.PathFor<RolePages_Given_Wasm>("admin-roles-list.png"),
      FullPage = true,
    });
    await newRole.ClickAsync();
    await page.WaitForURLAsync("**/Admin/Roles/New");
    await Assertions.Expect(page.Locator("[data-qa=RoleName] input")).ToBeVisibleAsync(
      new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
    await Assertions.Expect(page.Locator("[data-qa=RoleSave]")).ToBeVisibleAsync();
    await Assertions.Expect(page.GetByText("Create a custom role.", new PageGetByTextOptions { Exact = true }))
      .ToBeVisibleAsync();
    await Assertions.Expect(page.Locator("[data-qa=RoleEditName]")).ToHaveCountAsync(0);
    await Assertions.Expect(page.Locator("[data-qa=RoleEditEmpty]")).ToHaveCountAsync(0);
    await page.ScreenshotAsync(new PageScreenshotOptions
    {
      Path = PlaywrightScreenshot.PathFor<RolePages_Given_Wasm>("admin-roles-new.png"),
      FullPage = true,
    });

    await PlaywrightPageLoad.GotoAsync(page, $"/Admin/Roles/{RoleIds.Administrator}");
    ILocator name = page.Locator("[data-qa=RoleEditName] input");
    await Assertions.Expect(name).ToHaveValueAsync(
      "Administrator",
      new LocatorAssertionsToHaveValueOptions { Timeout = 30_000 });
    await Assertions.Expect(page.Locator("[data-qa=RoleName]")).ToHaveCountAsync(0);

    await name.FillAsync("Unsaved name");
    await page.Locator("[data-qa=RoleEditCancel]").ClickAsync();
    await Assertions.Expect(name).ToHaveValueAsync(
      "Administrator",
      new LocatorAssertionsToHaveValueOptions { Timeout = 10_000 });
    await page.ScreenshotAsync(new PageScreenshotOptions
    {
      Path = PlaywrightScreenshot.PathFor<RolePages_Given_Wasm>("admin-roles-administrator.png"),
      FullPage = true,
    });
  }

  public static async Task DetailPage_Should_Render_Dense_Permission_And_Scope_Trees()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync();
    graph.Web.ShouldNotBeNull();

    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
    await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
      Locale = "en-US",
      ColorScheme = ColorScheme.Light,
    });
    IPage page = await context.NewPageAsync();

    await DrawerCloseAsserts.SignInWithNewPasskeyAsync(page, context);
    await PlaywrightPageLoad.GotoAsync(page, $"/Admin/Roles/{RoleIds.Administrator}");

    ILocator permissions = page.Locator("[data-qa=RolePermissionEditor]");
    ILocator scopes = page.Locator("[data-qa=RoleAgentScopeEditor]");
    await Assertions.Expect(permissions).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
    await Assertions.Expect(scopes).ToBeVisibleAsync();
    await Assertions.Expect(page.Locator("[data-qa='AgentScope-identity:read']")).ToBeVisibleAsync();

    ILocator locked = page.Locator("[data-qa='Permission-admin.roles.manage']");
    await Assertions.Expect(locked).ToHaveAttributeAsync("disabled", "");
    (await locked.EvaluateAsync<bool>("el => el.checked")).ShouldBeTrue();
    await Assertions.Expect(ParentCheckbox(page, "PermissionGroup-admin")).ToHaveAttributeAsync("disabled", "");

    double[] heights = await page.EvaluateAsync<double[]>(RowHeightsScript);
    heights[0].ShouldBeLessThanOrEqualTo(36, "parent row height");
    heights[1].ShouldBeLessThanOrEqualTo(32, "child row height");

    ILocator access = page.Locator("[data-qa='Permission-developer.access']");
    ILocator claims = page.Locator("[data-qa='Permission-developer.claims.read']");
    bool accessBefore = await access.EvaluateAsync<bool>("el => el.checked");
    await access.ClickAsync();
    await WaitCheckedAsync(access, !accessBefore);
    (await ParentCheckbox(page, "PermissionGroup-developer").EvaluateAsync<bool>("el => el.indeterminate"))
      .ShouldBe(accessBefore == await claims.EvaluateAsync<bool>("el => el.checked"));

    await access.FocusAsync();
    await page.Keyboard.PressAsync("ArrowDown");
    await WaitFocusedAsync(page, "Permission-developer.claims.read");
    await page.Keyboard.PressAsync("ArrowUp");
    await WaitFocusedAsync(page, "Permission-developer.access");
    await page.Keyboard.PressAsync("Space");
    await WaitCheckedAsync(access, accessBefore);

    await page.SetViewportSizeAsync(1280, 2400);
    await page.EvaluateAsync(RowBoxScript);
    await ShootAsync(permissions, "role-administrator-permissions-light.png");
    await ShootAsync(scopes, "role-administrator-agent-scopes-light.png");

    await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
    await Assertions.Expect(page.Locator("body")).ToHaveAttributeAsync(
      "data-theme",
      "dark",
      new LocatorAssertionsToHaveAttributeOptions { Timeout = 10_000 });
    await ShootAsync(permissions, "role-administrator-permissions-dark.png");
    await ShootAsync(scopes, "role-administrator-agent-scopes-dark.png");

    await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Light });
    await page.SetViewportSizeAsync(1280, 800);
    await DrawerCloseAsserts.GrantDeveloperAsync(graph);
    await PlaywrightPageLoad.GotoAsync(page, "/DesignSystem/permission-tree");
    ILocator sample = page.Locator("[data-qa=DesignSystemPermissionTree]");
    await Assertions.Expect(sample.Locator("[data-qa='Permission-people.manage']"))
      .ToHaveAttributeAsync("disabled", "", new LocatorAssertionsToHaveAttributeOptions { Timeout = 30_000 });
    await page.EvaluateAsync(RowBoxScript);
    await page.ScreenshotAsync(new PageScreenshotOptions
    {
      Path = PlaywrightScreenshot.PathFor<RolePages_Given_Wasm>("design-system-permission-tree.png"),
      FullPage = true,
    });
  }

  private static ILocator ParentCheckbox(IPage page, string groupQa) =>
    page.Locator($"[data-qa='{groupQa}'] .twe-perm-tree__parent fluent-checkbox");

  private static async Task WaitCheckedAsync(ILocator checkbox, bool expected)
  {
    for (int attempt = 0; attempt < 50; attempt++)
    {
      if (await checkbox.EvaluateAsync<bool>("el => el.checked") == expected)
      {
        return;
      }

      await Task.Delay(100);
    }

    (await checkbox.EvaluateAsync<bool>("el => el.checked")).ShouldBe(expected);
  }

  private static async Task WaitFocusedAsync(IPage page, string dataQa)
  {
    const string script = "() => document.activeElement?.closest('[data-qa]')?.getAttribute('data-qa')";
    for (int attempt = 0; attempt < 50; attempt++)
    {
      if (await page.EvaluateAsync<string?>(script) == dataQa)
      {
        return;
      }

      await Task.Delay(100);
    }

    (await page.EvaluateAsync<string?>(script)).ShouldBe(dataQa);
  }

  private static Task ShootAsync(ILocator tree, string fileName) =>
    tree.Locator("xpath=ancestor::section[1]").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = PlaywrightScreenshot.PathFor<RolePages_Given_Wasm>(fileName),
    });
}
