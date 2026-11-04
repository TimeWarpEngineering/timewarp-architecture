#region Purpose
// Browser proof that the design-system drawer moves focus in, keeps Tab inside, and returns
// focus to the opener on Escape. The same run checks the dialog stays in the render tree,
// matches :modal, paints above the app bar, and refuses focus on the opener. It also checks
// the inline edges under both writing directions, a native dialog close, a module close that
// must not ask the host, Dispose closing an open dialog without asking the host, a modal
// of only native buttons letting Tab leave, and leaving the page while the drawer is open.
#endregion

#region Design
// Same host shape as AskFocus_Given_Wasm: in-process Web.Server, shared Chromium, en-US locale.
// The design system requires developer.access. The SPA does not use mock auth, so the test signs
// in with a virtual passkey the way AskSignInReprobe_Given_Wasm does. That first account is
// Administrator, which does not include developer.access, so the test adds the Developer role
// on the host store and loads /DesignSystem/drawers with a full navigation. Focus checks retry because
// showModal runs after the dialog's script binds.
// The end drawer slides with translateX(100%) for 180ms. The paint sample is 24px inside the dialog
// border box and in the top 16px, where the app bar sits. That box follows the transform, so early
// in the slide the sample is past the viewport edge and elementFromPoint misses, while the scrim
// point still hits ::backdrop. The probe finishes the dialog animations before it measures, and it
// counts a hit only when that sample lies inside the settled box and the viewport.
// End sits on the inline-end edge and Start on the inline-start edge. The test flips the
// document direction and the side class, then reads the settled box and the inner border.
// A form with method dialog closes the dialog without the module's Close, and the title
// leaves once the caller's flag drops. A direct Bind proves Show opens on the same turn,
// Close does not call RequestClose, and a native close calls it once. The module Close
// check waits for the queued close event before it reads host calls. The native close
// check clears that list first, then counts RequestClose, so an earlier module close
// cannot satisfy it. Dispose while open removes the listeners and then closes, so the
// queued close does not call RequestClose, and a later Show stays shut. Before the drawer
// opens, a display:contents host holds a modal dialog of two native buttons. Tab from the
// last button leaves that dialog for both display:block and display:flex. That is why the
// drawer wraps Tab. The inner panel is the flex column and the sheet hit target. Leaving the
// page uses Blazor.navigateTo so the open dialog is disposed in place.
#endregion

namespace DesignSystemDrawerPlaywright_;

using static DesignSystemDrawerPlaywright_.DrawerPageAsserts;
using static DesignSystemDrawerPlaywright_.DrawerCloseAsserts;

public class DesignSystemDrawer_Given_Wasm
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<DesignSystemDrawer_Given_Wasm>();

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
    await page.GotoAsync(InProcTestPorts.WebHostUrl.TrimEnd('/') + "/DesignSystem/drawers", new PageGotoOptions
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
      throw new TimeoutException($"Design system drawer opener was not shown at {page.Url}. Page:\n{text}", exception);
    }

    await AssertNativeModalLetsTabLeaveAsync(page);

    await opener.ClickAsync();
    ILocator openDialog = page.Locator("dialog.twe-drawer[open]");
    await Assertions.Expect(openDialog).ToHaveCountAsync(1, new LocatorAssertionsToHaveCountOptions { Timeout = 30_000 });

    ILocator headerClose = page.Locator("[data-qa=DrawerClose]");
    await Assertions.Expect(headerClose).ToBeFocusedAsync(new LocatorAssertionsToBeFocusedOptions { Timeout = 30_000 });
    await AssertModalInTheShellAsync(page);
    await AssertInlineEdgesAsync(page);

    string withHandler = await RecordTabStopsAsync(page);
    withHandler.StartsWith("wrapped:", StringComparison.Ordinal)
      .ShouldBeTrue($"Tab left the drawer or never wrapped: {withHandler}");
    await page.Keyboard.PressAsync("Shift+Tab");
    string backward = await FocusedStopAsync(page);
    backward.EndsWith("|inside", StringComparison.Ordinal)
      .ShouldBeTrue($"Shift+Tab left the drawer: {backward}");
    backward.Contains("|DrawerFooterClose|", StringComparison.Ordinal)
      .ShouldBeTrue($"Shift+Tab did not wrap to the footer close button: {backward}");
    await Assertions.Expect(opener).Not.ToBeFocusedAsync();

    await page.Keyboard.PressAsync("Escape");
    await Assertions.Expect(openDialog).ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = 10_000 });
    await Assertions.Expect(opener).ToBeFocusedAsync(new LocatorAssertionsToBeFocusedOptions { Timeout = 10_000 });

    await opener.ClickAsync();
    await Assertions.Expect(openDialog).ToHaveCountAsync(1, new LocatorAssertionsToHaveCountOptions { Timeout = 30_000 });
    bool inserted = await page.EvaluateAsync<bool>(
      """
      () => {
        const body = document.querySelector("dialog.twe-drawer[open] .twe-drawer__body");
        if (!body) {
          return false;
        }

        const form = document.createElement("form");
        form.method = "dialog";
        const button = document.createElement("button");
        button.type = "submit";
        button.setAttribute("data-qa", "DrawerNativeClose");
        button.textContent = "Native close";
        form.append(button);
        body.append(form);
        return true;
      }
      """);
    inserted.ShouldBeTrue();
    await page.Locator("[data-qa=DrawerNativeClose]").ClickAsync();
    await Assertions.Expect(page.Locator("dialog.twe-drawer")).Not.ToContainTextAsync(
      "Sample drawer",
      new LocatorAssertionsToContainTextOptions { Timeout = 10_000 });
    await Assertions.Expect(openDialog).ToHaveCountAsync(0);

    await AssertCloseAndNativeCloseAsync(page);

    await opener.ClickAsync();
    await Assertions.Expect(openDialog).ToHaveCountAsync(1, new LocatorAssertionsToHaveCountOptions { Timeout = 30_000 });
    await AssertLeavingThePageRemovesTheOpenDrawerAsync(page);
  }
}
