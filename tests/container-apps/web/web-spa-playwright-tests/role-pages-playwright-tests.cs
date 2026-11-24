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
#endregion

namespace RolePagesPlaywright_;

using DesignSystemDrawerPlaywright_;
using TimeWarp.Architecture.Features;

public class RolePages_Given_Wasm
{
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

    await PlaywrightPageLoad.GotoAsync(page, "/Admin/Roles/New");
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
}
