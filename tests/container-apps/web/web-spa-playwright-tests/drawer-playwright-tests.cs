#region Purpose
// Browser proof that the style-guide drawer moves focus in, keeps Tab inside, and returns
// focus to the opener on Escape. The same run checks the inline edges under both writing
// directions, a native dialog close, a module close that must not ask the host, and a show
// that resolves only after Close.
#endregion

#region Design
// Same host shape as AskFocus_Given_Wasm: in-process Web.Server, shared Chromium, en-US locale.
// The style guide requires developer.access. The SPA does not use mock auth, so the test signs
// in with a virtual passkey the way AskSignIn_Given_Wasm does. That first account is
// Administrator, which does not include developer.access, so the test adds the Developer role
// on the host store and loads /StyleGuide with a full navigation. Focus checks retry because
// showModal runs after the dialog's script binds.
// End sits on the inline-end edge and Start on the inline-start edge. The test flips the
// document direction and the side class, then reads the settled box and the inner border.
// A form with method dialog closes the dialog without the module's Close, and the title
// leaves once the caller's flag drops. A direct Bind, with fluent-button still undefined,
// proves a late show does not call showModal after Close. The module Close check waits for
// the queued close event before it reads host calls. The native close check clears that
// list first, then counts RequestClose, so an earlier module close cannot satisfy it.
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
    await AssertInlineEdgesAsync(page);

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

    await AssertLateShowStaysClosedAsync(page);
  }

  private static async Task AssertInlineEdgesAsync(IPage page)
  {
    await WaitUntilFlushAsync(page, flushRight: true);
    await AssertInnerBorderAsync(page, onInlineStart: true);

    await page.EvaluateAsync("() => { document.documentElement.dir = 'rtl'; }");
    await WaitUntilFlushAsync(page, flushRight: false);
    await AssertInnerBorderAsync(page, onInlineStart: true);

    await page.EvaluateAsync(
      """
      () => {
        const dialog = document.querySelector("dialog.twe-drawer[open]");
        dialog.classList.remove("twe-drawer--end");
        dialog.classList.add("twe-drawer--start");
      }
      """);
    await WaitUntilFlushAsync(page, flushRight: true);
    await AssertInnerBorderAsync(page, onInlineStart: false);

    await page.EvaluateAsync(
      """
      () => {
        document.documentElement.dir = "ltr";
        const dialog = document.querySelector("dialog.twe-drawer[open]");
        dialog.classList.remove("twe-drawer--start");
        dialog.classList.add("twe-drawer--end");
      }
      """);
    await WaitUntilFlushAsync(page, flushRight: true);
    await AssertInnerBorderAsync(page, onInlineStart: true);
  }

  private static async Task AssertInnerBorderAsync(IPage page, bool onInlineStart)
  {
    double[] widths = await page.EvaluateAsync<double[]>(
      """
      () => {
        const dialog = document.querySelector("dialog.twe-drawer[open]");
        const style = getComputedStyle(dialog);
        return [
          Number.parseFloat(style.borderInlineStartWidth),
          Number.parseFloat(style.borderInlineEndWidth),
        ];
      }
      """);
    if (onInlineStart)
    {
      widths[0].ShouldBe(1, 0.2);
      widths[1].ShouldBe(0, 0.2);
      return;
    }

    widths[0].ShouldBe(0, 0.2);
    widths[1].ShouldBe(1, 0.2);
  }

  private static Task WaitUntilFlushAsync(IPage page, bool flushRight) =>
    page.WaitForFunctionAsync(
      """
      (flushRight) => {
        const dialog = document.querySelector("dialog.twe-drawer[open]");
        if (!dialog) {
          return false;
        }

        const rect = dialog.getBoundingClientRect();
        const width = document.documentElement.clientWidth;
        const transform = getComputedStyle(dialog).transform;
        const idle = transform === "none" || transform === "matrix(1, 0, 0, 1, 0, 0)";
        if (!idle) {
          return false;
        }

        if (flushRight) {
          return Math.abs(rect.right - width) <= 1 && rect.left > 64;
        }

        return rect.left <= 1 && width - rect.right > 64;
      }
      """,
      flushRight,
      new PageWaitForFunctionOptions { Timeout = 5_000 });

  private static async Task AssertLateShowStaysClosedAsync(IPage page)
  {
    string result = await page.EvaluateAsync<string>(
      """
      async () => {
        const realGet = customElements.get.bind(customElements);
        const realWhenDefined = customElements.whenDefined.bind(customElements);
        const frames = () => new Promise((resolve) => {
          requestAnimationFrame(() => requestAnimationFrame(resolve));
        });
        let release = () => undefined;
        const holdButton = () => new Promise((resolve) => {
          release = resolve;
        });
        customElements.get = (name) => name === "fluent-button" ? undefined : realGet(name);
        customElements.whenDefined = (name) => name === "fluent-button" ? holdButton() : realWhenDefined(name);

        const dialog = document.createElement("dialog");
        dialog.className = "twe-drawer twe-drawer--end";
        dialog.append(document.createElement("button"));
        document.body.append(dialog);
        const calls = [];
        const host = { invokeMethodAsync: async (name) => { calls.push(name); } };
        const requestCloseCount = () => calls.filter((name) => name === "RequestClose").length;
        const waitForQueuedClose = () => {
          if (!dialog.open) {
            return Promise.resolve();
          }

          return new Promise((resolve) => {
            dialog.addEventListener("close", () => resolve(), { once: true });
          });
        };
        try {
          const module = await import("/js/features/drawer.js");
          const handle = module.Bind(dialog, host);

          handle.Show();
          const stayedClosedUntilDefined = dialog.open === false;
          release();
          await frames();
          const openedAfterDefine = dialog.open === true;
          const moduleClose = waitForQueuedClose();
          handle.Close();
          await moduleClose;
          const moduleCloseRequestCount = requestCloseCount();

          handle.Show();
          const stayedClosedOnSecondShow = dialog.open === false;
          handle.Close();
          release();
          await frames();
          const stayedClosedAfterLateShow = dialog.open === false;

          handle.Show();
          release();
          await frames();
          calls.length = 0;
          const nativeClose = waitForQueuedClose();
          dialog.close();
          await nativeClose;
          const nativeCloseRequestCount = requestCloseCount();
          const stayedClosedAfterNativeClose = dialog.open === false;
          handle.Dispose();

          return JSON.stringify({
            stayedClosedUntilDefined,
            openedAfterDefine,
            moduleCloseRequestCount,
            stayedClosedOnSecondShow,
            stayedClosedAfterLateShow,
            nativeCloseRequestCount,
            stayedClosedAfterNativeClose,
          });
        } finally {
          customElements.get = realGet;
          customElements.whenDefined = realWhenDefined;
          dialog.remove();
        }
      }
      """);

    using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(result);
    System.Text.Json.JsonElement root = document.RootElement;
    root.GetProperty("stayedClosedUntilDefined").GetBoolean().ShouldBeTrue();
    root.GetProperty("openedAfterDefine").GetBoolean().ShouldBeTrue();
    root.GetProperty("moduleCloseRequestCount").GetInt32().ShouldBe(0);
    root.GetProperty("stayedClosedOnSecondShow").GetBoolean().ShouldBeTrue();
    root.GetProperty("stayedClosedAfterLateShow").GetBoolean().ShouldBeTrue();
    root.GetProperty("nativeCloseRequestCount").GetInt32().ShouldBe(1);
    root.GetProperty("stayedClosedAfterNativeClose").GetBoolean().ShouldBeTrue();
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
