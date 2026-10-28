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
// in with a virtual passkey the way AskSignIn_Given_Wasm does. That first account is
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

using TimeWarp.Architecture.Features;
using TimeWarp.Identity;

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

  private static async Task AssertModalInTheShellAsync(IPage page)
  {
    string probe = await page.EvaluateAsync<string>(
      """
      () => {
        const dialog = document.querySelector("dialog.twe-drawer[open]");
        const shell = document.querySelector(".twe-shell");
        const opener = document.querySelector("[data-qa=DrawerOpen]");
        const closeButton = dialog && dialog.querySelector("[data-qa=DrawerClose]");
        if (!dialog || !shell || !opener || !closeButton) {
          return JSON.stringify({ error: "missing drawer, shell, opener, or close button" });
        }

        // The open slide is translateX(100%) on the dialog. Finish it before reading the box so
        // the sample is not taken while the panel still sits past the viewport edge.
        for (const animation of dialog.getAnimations()) {
          const state = animation.playState;
          if (state === "running" || state === "pending" || state === "paused") {
            animation.finish();
          }
        }

        const rect = dialog.getBoundingClientRect();
        const sampleX = rect.left + 24;
        const sampleY = Math.min(rect.top + 12, 16);
        const sampleInside = sampleX >= rect.left && sampleX < rect.right
          && sampleY >= rect.top && sampleY < rect.bottom
          && sampleX >= 0 && sampleY >= 0
          && sampleX < window.innerWidth && sampleY < window.innerHeight;
        const hitsDialog = (x, y) => {
          let node = document.elementFromPoint(x, y);
          while (node) {
            if (node === dialog) {
              return true;
            }

            const root = node.getRootNode();
            node = root instanceof ShadowRoot ? root.host : node.parentElement;
          }

          return false;
        };
        const insideDialog = (node) => {
          while (node) {
            if (node === dialog) {
              return true;
            }

            const root = node.getRootNode();
            node = root instanceof ShadowRoot ? root.host : node.parentElement;
          }

          return false;
        };

        closeButton.focus();
        const focusBeforeOpener = insideDialog(document.activeElement);
        opener.focus();
        const openerTookFocus = document.activeElement === opener || opener.contains(document.activeElement);
        const focusAfterOpenerAttempt = insideDialog(document.activeElement);
        closeButton.focus();
        return JSON.stringify({
          parentIsBody: dialog.parentElement === document.body,
          shellDisplay: getComputedStyle(shell).display,
          matchesModal: dialog.matches(":modal"),
          focusBeforeOpener,
          openerTookFocus,
          focusAfterOpenerAttempt,
          paintHitsDialog: sampleInside && hitsDialog(sampleX, sampleY),
          scrimHitsDialog: hitsDialog(24, Math.round(window.innerHeight / 2)),
          sampleInside,
          sampleX,
          sampleY,
          rectLeft: rect.left,
          rectTop: rect.top,
          rectRight: rect.right,
          rectBottom: rect.bottom
        });
      }
      """);

    using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(probe);
    System.Text.Json.JsonElement root = document.RootElement;
    root.TryGetProperty("error", out _).ShouldBeFalse(probe);
    root.GetProperty("parentIsBody").GetBoolean().ShouldBeFalse(probe);
    root.GetProperty("shellDisplay").GetString().ShouldBe("contents", probe);
    root.GetProperty("matchesModal").GetBoolean().ShouldBeTrue(probe);
    root.GetProperty("focusBeforeOpener").GetBoolean().ShouldBeTrue(probe);
    root.GetProperty("openerTookFocus").GetBoolean().ShouldBeFalse(probe);
    root.GetProperty("focusAfterOpenerAttempt").GetBoolean().ShouldBeTrue(probe);
    root.GetProperty("paintHitsDialog").GetBoolean().ShouldBeTrue(probe);
    root.GetProperty("scrimHitsDialog").GetBoolean().ShouldBeTrue(probe);
  }

  private static async Task AssertLeavingThePageRemovesTheOpenDrawerAsync(IPage page)
  {
    System.Collections.Concurrent.ConcurrentQueue<string> errors = new();
    void OnConsole(object? _, IConsoleMessage message)
    {
      if (message.Type == "error")
      {
        errors.Enqueue(message.Text);
      }
    }

    void OnPageError(object? _, string error) => errors.Enqueue(error);

    page.Console += OnConsole;
    page.PageError += OnPageError;
    try
    {
      await page.EvaluateAsync("() => Blazor.navigateTo('/')");
      await page.GetByText("Welcome to TimeWarp.Architecture").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
      (await page.Locator("dialog.twe-drawer").CountAsync()).ShouldBe(0);
      string[] found = errors.ToArray();
      found.Length.ShouldBe(0, string.Join("\n", found));
    }
    finally
    {
      page.Console -= OnConsole;
      page.PageError -= OnPageError;
    }
  }

  private static async Task<string> RecordTabStopsAsync(IPage page)
  {
    List<string> stops = [];
    bool sawFooter = false;
    bool returnedToHeader = false;
    for (int step = 0; step < 6 && !returnedToHeader; step++)
    {
      await page.Keyboard.PressAsync("Tab");
      string where = await FocusedStopAsync(page);
      stops.Add(where);
      if (!where.EndsWith("|inside", StringComparison.Ordinal))
      {
        return "left:" + string.Join(" -> ", stops);
      }

      if (where.Contains("|DrawerFooterClose|", StringComparison.Ordinal))
      {
        sawFooter = true;
      }

      if (sawFooter && where.Contains("|DrawerClose|", StringComparison.Ordinal))
      {
        returnedToHeader = true;
      }
    }

    string joined = string.Join(" -> ", stops);
    if (!sawFooter || !returnedToHeader)
    {
      return "incomplete:" + joined;
    }

    return "wrapped:" + joined;
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

  private static async Task AssertCloseAndNativeCloseAsync(IPage page)
  {
    string result = await page.EvaluateAsync<string>(
      """
      async () => {
        const dialog = document.createElement("dialog");
        dialog.className = "twe-drawer twe-drawer--end";
        dialog.append(document.createElement("button"));
        document.body.append(dialog);
        const calls = [];
        const host = { invokeMethodAsync: async (name) => { calls.push(name); } };
        const requestCloseCount = () => calls.filter((name) => name === "RequestClose").length;
        const waitForQueuedClose = () => new Promise((resolve) => {
          dialog.addEventListener("close", () => resolve(), { once: true });
        });
        try {
          const module = await import("/js/features/drawer.js");
          const handle = module.Bind(dialog, host);

          handle.Show();
          const openedImmediately = dialog.open === true;
          const moduleClose = waitForQueuedClose();
          handle.Close();
          await moduleClose;
          const moduleCloseRequestCount = requestCloseCount();
          const closed = dialog.open === false;

          handle.Show();
          const reopened = dialog.open === true;
          calls.length = 0;
          const nativeClose = waitForQueuedClose();
          dialog.close();
          await nativeClose;
          const nativeCloseRequestCount = requestCloseCount();
          const stayedClosedAfterNativeClose = dialog.open === false;

          handle.Show();
          const openedForDispose = dialog.open === true;
          calls.length = 0;
          const disposeClosed = new Promise((resolve) => {
            dialog.addEventListener("close", () => resolve(), { once: true });
          });
          handle.Dispose();
          await disposeClosed;
          const disposeRequestCount = requestCloseCount();
          const closedAfterDispose = dialog.open === false;
          handle.Show();
          const stayedClosedAfterDispose = dialog.open === false;

          return JSON.stringify({
            openedImmediately,
            moduleCloseRequestCount,
            closed,
            reopened,
            nativeCloseRequestCount,
            stayedClosedAfterNativeClose,
            openedForDispose,
            disposeRequestCount,
            closedAfterDispose,
            stayedClosedAfterDispose,
          });
        } finally {
          dialog.remove();
        }
      }
      """);

    using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(result);
    System.Text.Json.JsonElement root = document.RootElement;
    root.GetProperty("openedImmediately").GetBoolean().ShouldBeTrue();
    root.GetProperty("moduleCloseRequestCount").GetInt32().ShouldBe(0);
    root.GetProperty("closed").GetBoolean().ShouldBeTrue();
    root.GetProperty("reopened").GetBoolean().ShouldBeTrue();
    root.GetProperty("nativeCloseRequestCount").GetInt32().ShouldBe(1);
    root.GetProperty("stayedClosedAfterNativeClose").GetBoolean().ShouldBeTrue();
    root.GetProperty("openedForDispose").GetBoolean().ShouldBeTrue();
    root.GetProperty("disposeRequestCount").GetInt32().ShouldBe(0);
    root.GetProperty("closedAfterDispose").GetBoolean().ShouldBeTrue();
    root.GetProperty("stayedClosedAfterDispose").GetBoolean().ShouldBeTrue();
  }

  private static async Task AssertNativeModalLetsTabLeaveAsync(IPage page)
  {
    await AssertNativeModalDisplayLetsTabLeaveAsync(page, "block");
    await AssertNativeModalDisplayLetsTabLeaveAsync(page, "flex");
  }

  private static async Task AssertNativeModalDisplayLetsTabLeaveAsync(IPage page, string display)
  {
    await page.EvaluateAsync(
      """
      (display) => {
        const host = document.createElement("div");
        host.id = "native-modal-probe";
        host.style.display = "contents";
        const dialog = document.createElement("dialog");
        dialog.id = "native-modal-probe-dialog";
        dialog.style.display = display;
        const a = document.createElement("button");
        a.id = "native-modal-probe-a";
        a.type = "button";
        a.textContent = "a";
        const b = document.createElement("button");
        b.id = "native-modal-probe-b";
        b.type = "button";
        b.textContent = "b";
        dialog.append(a, b);
        host.append(dialog);
        document.body.append(host);
        dialog.showModal();
        b.focus();
      }
      """,
      display);

    try
    {
      List<string> stops = [];
      for (int step = 0; step < 4; step++)
      {
        await page.Keyboard.PressAsync("Tab");
        stops.Add(await page.EvaluateAsync<string>(
          """
          () => {
            const dialog = document.getElementById("native-modal-probe-dialog");
            const active = document.activeElement;
            const inside = !!dialog && !!active && (active === dialog || dialog.contains(active));
            if (!active || active === document.body) {
              return "outside:BODY";
            }

            const id = "id" in active && active.id ? active.id : active.tagName;
            return `${inside ? "inside" : "outside"}:${id}`;
          }
          """));
      }

      string joined = string.Join(" -> ", stops);
      stops[0].StartsWith("outside:", StringComparison.Ordinal)
        .ShouldBeTrue($"display:{display} kept Tab inside a native-button dialog: {joined}");
    }
    finally
    {
      await page.EvaluateAsync(
        """
        () => {
          const dialog = document.getElementById("native-modal-probe-dialog");
          if (dialog instanceof HTMLDialogElement && dialog.open) {
            dialog.close();
          }

          document.getElementById("native-modal-probe")?.remove();
        }
        """);
    }
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
