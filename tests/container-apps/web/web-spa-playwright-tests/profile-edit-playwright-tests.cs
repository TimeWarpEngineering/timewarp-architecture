#region Purpose
// Browser proof that Profile loads the server profile, Cancel restores it, and Save keeps the new name.
#endregion

#region Design
// In-process Web.Server, shared Chromium, en-US, a virtual passkey. The first account is
// Administrator and can open /Profile. Create-if-missing answers "Member" with en-US / US / system,
// so the form shows those values without the page inventing them. Cancel copies the snapshot
// back. Save writes the display name, the shell card updates, and a reload still shows it.
#endregion

namespace ProfileEditPlaywright_;

using DesignSystemDrawerPlaywright_;

public class ProfileEdit_Given_Wasm
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ProfileEdit_Given_Wasm>();

  public static async Task SaveAndCancel_Should_KeepTheServerSnapshot()
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
    await PlaywrightPageLoad.GotoAsync(page, "/Profile");

    ILocator displayName = page.Locator("[data-qa=ProfileDisplayName] input");
    await Assertions.Expect(displayName).ToHaveValueAsync(
      "Member",
      new LocatorAssertionsToHaveValueOptions { Timeout = 30_000 });
    await Assertions.Expect(page.Locator("[data-qa=ProfileEmail] input")).ToHaveValueAsync("");

    string language = await ControlTextAsync(page.Locator("[data-qa=ProfileLanguage]"));
    (language.Contains("en-US", StringComparison.Ordinal) ||
      language.Contains("English (United States)", StringComparison.Ordinal))
      .ShouldBeTrue(language);
    string region = await ControlTextAsync(page.Locator("[data-qa=ProfileRegion]"));
    (region.Contains("United States", StringComparison.Ordinal) ||
      region.Contains("US", StringComparison.Ordinal))
      .ShouldBeTrue(region);
    string theme = await ControlTextAsync(page.Locator("[data-qa=ProfileTheme]"));
    theme.ShouldContain("System");

    await displayName.FillAsync("Temporary");
    await page.Locator("[data-qa=ProfileCancel]").ClickAsync();
    await Assertions.Expect(displayName).ToHaveValueAsync(
      "Member",
      new LocatorAssertionsToHaveValueOptions { Timeout = 10_000 });

    await displayName.FillAsync("Ada Lovelace");
    await page.Locator("[data-qa=ProfileSave]").ClickAsync();
    await Assertions.Expect(page.Locator("[data-qa=ShellUserCard]")).ToContainTextAsync(
      "Ada Lovelace",
      new LocatorAssertionsToContainTextOptions { Timeout = 30_000 });

    await PlaywrightPageLoad.ReloadAsync(page);
    await Assertions.Expect(displayName).ToHaveValueAsync(
      "Ada Lovelace",
      new LocatorAssertionsToHaveValueOptions { Timeout = 30_000 });
    await Assertions.Expect(page.Locator("[data-qa=ShellUserCard]")).ToContainTextAsync("Ada Lovelace");
  }

  private static async Task<string> ControlTextAsync(ILocator root)
  {
    await root.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    ILocator input = root.Locator("input");
    string value = await input.CountAsync() > 0 ? await input.First.InputValueAsync() : "";
    string text = await root.InnerTextAsync();
    return value + "\n" + text;
  }
}
