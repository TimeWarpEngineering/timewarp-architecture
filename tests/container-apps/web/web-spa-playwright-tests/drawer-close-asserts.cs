#region Purpose
// Close and native-modal assertions for the drawer Playwright proof, plus the developer grant and
// passkey sign-in the test needs.
#endregion

namespace DesignSystemDrawerPlaywright_;

using TimeWarp.Architecture.Features;
using TimeWarp.Identity;

internal static class DrawerCloseAsserts
{
  internal static async Task AssertCloseAndNativeCloseAsync(IPage page)
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

  internal static async Task AssertNativeModalLetsTabLeaveAsync(IPage page)
  {
    await AssertNativeModalDisplayLetsTabLeaveAsync(page, "block");
    await AssertNativeModalDisplayLetsTabLeaveAsync(page, "flex");
  }

  internal static async Task AssertNativeModalDisplayLetsTabLeaveAsync(IPage page, string display)
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

  internal static Task<string> FocusedStopAsync(IPage page) =>
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

  internal static async Task GrantDeveloperAsync(HostGraph graph)
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

  internal static async Task SignInWithNewPasskeyAsync(IPage page, IBrowserContext context)
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
