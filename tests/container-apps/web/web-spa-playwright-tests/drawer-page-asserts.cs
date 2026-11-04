#region Purpose
// Page-level assertions for the drawer Playwright proof: modal in the shell, leaving the page, tab
// stops, inline edges and borders.
#endregion

namespace DesignSystemDrawerPlaywright_;

using static DesignSystemDrawerPlaywright_.DrawerCloseAsserts;

internal static class DrawerPageAsserts
{
  internal static async Task AssertModalInTheShellAsync(IPage page)
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

  internal static async Task AssertLeavingThePageRemovesTheOpenDrawerAsync(IPage page)
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

  internal static async Task<string> RecordTabStopsAsync(IPage page)
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

  internal static async Task AssertInlineEdgesAsync(IPage page)
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

  internal static async Task AssertInnerBorderAsync(IPage page, bool onInlineStart)
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

  internal static Task WaitUntilFlushAsync(IPage page, bool flushRight) =>
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

}
