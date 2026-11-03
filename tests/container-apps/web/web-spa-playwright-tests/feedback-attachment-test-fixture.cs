#region Purpose
// Shared helpers for the FeedbackAttachment Playwright test classes: grid layout and form assertions,
// feedback filing, edge-box measurement and passkey sign-in.
#endregion

namespace FeedbackAttachmentPlaywright_;

internal static class FeedbackAttachmentTestFixture
{
  internal static async Task OpenAdminGridAsync(IPage page, ConcurrentQueue<string> console, string path, string rowQa)
  {
    await page.GotoAsync(InProcTestPorts.WebHostUrl.TrimEnd('/') + path, new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    try
    {
      await page.Locator($"[data-qa={rowQa}]").First.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
    }
    catch (TimeoutException exception)
    {
      await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath($"admin-grid-{rowQa}-failed.png"), FullPage = true });
      string text = await page.Locator("body").InnerTextAsync();
      string log = string.Join('\n', console);
      throw new TimeoutException($"{path} showed no {rowQa}. Page:\n{text}\nConsole:\n{log}", exception);
    }
  }

  // The Ask button is interactive after the shell's first WebAssembly render. Retry until Ask opens.
  internal static async Task OpenAskPanelAsync(IPage page)
  {
    ILocator button = page.Locator("[data-qa=AskAiButton]");
    ILocator panel = page.Locator("[data-qa=AgentAsk]");
    for (int attempt = 0; attempt < 15; attempt++)
    {
      await button.ClickAsync();
      try
      {
        await panel.WaitForAsync(new LocatorWaitForOptions { Timeout = 4_000 });
        return;
      }
      catch (TimeoutException)
      {
      }
    }

    throw new TimeoutException("The top-bar Ask button did not open the Ask panel.");
  }

  // Header cells have width, never overlap, and clip anything wider than the cell. The primary
  // column's cells keep a usable width (task 354: Title collapsed to zero and read "Kitld").
  internal static async Task AssertGridLayoutAsync(IPage page, string gridQa, string rowQa, string shot)
  {
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath($"admin-grid-{shot}.png"), FullPage = true });
    string[] problems = await page.EvaluateAsync<string[]>(
      """
      ([gridQa, rowQa]) => {
        const problems = [];
        const grid = document.querySelector(`[data-qa=${gridQa}]`);
        if (!grid) { return [`no grid ${gridQa}`]; }
        const headers = [...grid.querySelectorAll('th[cell-type=columnheader]')];
        if (headers.length < 2) { problems.push(`only ${headers.length} header cells`); }
        headers.forEach((th, index) => {
          const box = th.getBoundingClientRect();
          const label = th.innerText.trim() || `#${index}`;
          if (box.width < 40) { problems.push(`header ${label} is ${box.width}px wide`); }
          const next = headers[index + 1];
          if (next && box.right > next.getBoundingClientRect().left + 1) {
            problems.push(`header ${label} (right ${box.right}) overlaps the next (left ${next.getBoundingClientRect().left})`);
          }
          const clips = ['hidden', 'clip'].includes(getComputedStyle(th).overflowX);
          for (const child of th.querySelectorAll('[col-sort-button], [col-title]')) {
            const childBox = child.getBoundingClientRect();
            if (!clips && childBox.right > box.right + 1) {
              problems.push(`header ${label} content (right ${childBox.right}) spills past its cell (right ${box.right})`);
            }
          }
        });
        const links = [...grid.querySelectorAll(`[data-qa=${rowQa}]`)];
        if (links.length === 0) { problems.push(`no ${rowQa} links`); }
        for (const link of links) {
          const cell = link.closest('td');
          const cellBox = cell.getBoundingClientRect();
          const linkBox = link.getBoundingClientRect();
          if (cellBox.width < 200) { problems.push(`primary cell is ${cellBox.width}px wide`); }
          if (linkBox.width <= 0 || linkBox.height <= 0) { problems.push(`${rowQa} link has no box`); }
        }
        return problems;
      }
      """,
      new[] { gridQa, rowQa });
    problems.ShouldBeEmpty($"{gridQa} at {shot}: {string.Join("; ", problems)}");
  }

  // Below the grid's minimum total width the frame around it scrolls; the page does not.
  internal static async Task AssertGridScrollsAsync(IPage page, string gridQa)
  {
    int[] widths = await page.EvaluateAsync<int[]>(
      """
      (gridQa) => {
        const frame = document.querySelector(`[data-qa=${gridQa}]`).closest('.twe-data-grid-frame');
        if (!frame) { return []; }
        const before = frame.scrollLeft;
        frame.scrollLeft = 10000;
        const scrolled = frame.scrollLeft;
        frame.scrollLeft = before;
        const root = document.documentElement;
        return [frame.clientWidth, frame.scrollWidth, Math.round(scrolled), root.clientWidth, root.scrollWidth];
      }
      """,
      gridQa);
    widths.Length.ShouldBe(5, $"{gridQa} is not inside DataGridFrame");
    string report = $"{gridQa} frame={widths[0]}/{widths[1]} scrolled={widths[2]} page={widths[3]}/{widths[4]}";
    Console.WriteLine(report);
    widths[1].ShouldBeGreaterThan(widths[0], report);
    widths[2].ShouldBeGreaterThan(0, report);
    widths[4].ShouldBeLessThanOrEqualTo(widths[3] + 1, report);
  }

  // The submit button is Disabled while any action is active, and blurring Details dispatches the
  // composer-context action, so a click straight after typing can land on a disabled button and
  // do nothing. Wait until the button has stayed enabled for 500ms, then click exactly once: a
  // re-click could file a duplicate item and would hide a submit that really did nothing.
  internal static async Task<ILocator> SubmitFeedbackAsync(IPage page)
  {
    await page.WaitForFunctionAsync(
      """
      () => {
        const button = document.querySelector('[data-qa=FeedbackSubmit]');
        const enabled = button && !button.hasAttribute('disabled');
        if (!enabled) { window.feedbackSubmitEnabledSince = undefined; return false; }
        window.feedbackSubmitEnabledSince ??= performance.now();
        return performance.now() - window.feedbackSubmitEnabledSince >= 500;
      }
      """,
      null,
      new PageWaitForFunctionOptions { Timeout = 30_000, PollingInterval = 50 });
    await page.Locator("[data-qa=FeedbackSubmit]").ClickAsync();

    ILocator receipt = page.Locator("[data-qa=FeedbackReceiptId]");
    try
    {
      await receipt.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    }
    catch (TimeoutException exception)
    {
      await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("feedback-reply-submit-failed.png"), FullPage = true });
      throw new TimeoutException("Feedback submit showed no receipt after one click on an enabled button.", exception);
    }

    return receipt;
  }

  internal static async Task AssertBoxesFillTheirFieldAsync(IPage page, string surface, string[] qas)
  {
    foreach (string qa in qas)
    {
      await AssertFillsFieldAsync(page, qa, $"{surface}-1280");
    }

    await page.ScreenshotAsync(new PageScreenshotOptions
    {
      Path = ScreenshotPath($"feedback-{surface}-textareas-1280.png"),
      FullPage = true,
    });

    foreach (string qa in qas)
    {
      await AssertGripResizesAsync(page, qa);
    }

    await page.SetViewportSizeAsync(600, 900);
    await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
    foreach (string qa in qas)
    {
      await AssertFillsFieldAsync(page, qa, $"{surface}-600");
    }

    await page.ScreenshotAsync(new PageScreenshotOptions
    {
      Path = ScreenshotPath($"feedback-{surface}-textareas-600.png"),
      FullPage = true,
    });
  }

  // The field is the element holding the shared TextArea wrapper; its content box is the width
  // the visible part=root must fill. A narrow box (18rem default) fails the right-edge check.
  internal static async Task AssertFillsFieldAsync(IPage page, string qa, string viewport)
  {
    const string fieldScript = """
      (qa) => {
        const host = document.querySelector(`[data-qa=${qa}]`);
        const wrapper = host && host.closest('.twe-text-area');
        const field = wrapper && wrapper.parentElement;
        if (!field) {
          return null;
        }
        const rect = field.getBoundingClientRect();
        const style = getComputedStyle(field);
        const left = rect.left + parseFloat(style.paddingLeft) + parseFloat(style.borderLeftWidth);
        const right = rect.right - parseFloat(style.paddingRight) - parseFloat(style.borderRightWidth);
        return [left, right];
      }
      """;
    await page.Locator($"[data-qa={qa}]").ScrollIntoViewIfNeededAsync();
    float[] field = (await page.EvaluateAsync<float[]?>(fieldScript, qa)).ShouldNotBeNull($"{qa} has no TextArea field");
    EdgeBox fieldBox = new(field[0], 0f, field[1] - field[0], 0f);
    EdgeBox root = await ShadowBoxAsync(page, qa, "root");
    EdgeBox control = await ShadowBoxAsync(page, qa, "control");
    string report = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"viewport={viewport} qa={qa} field={fieldBox.X:0.#}..{fieldBox.Right:0.#} {fieldBox.Width:0.#} root={root.X:0.#}..{root.Right:0.#} {root.Width:0.#}x{root.Height:0.#} control={control.Width:0.#}x{control.Height:0.#}");
    Console.WriteLine(report);
    SameEdges(fieldBox, root, report);
    ((double)root.Height).ShouldBeGreaterThanOrEqualTo(140d, report);
    ((double)control.Height).ShouldBeGreaterThan(100d, report);
  }

  internal static async Task AssertGripResizesAsync(IPage page, string qa)
  {
    const float dragDistance = 80f;
    ILocator rootLocator = page.Locator($"[data-qa={qa}]").Locator("[part=root]");
    await rootLocator.ScrollIntoViewIfNeededAsync();
    EdgeBox before = await ShadowBoxAsync(page, qa, "root");
    float gripX = before.Right - 8f;
    float gripY = before.Y + before.Height - 4f;
    await page.Mouse.MoveAsync(gripX, gripY);
    await page.Mouse.DownAsync();
    await page.Mouse.MoveAsync(gripX, gripY + dragDistance, new MouseMoveOptions { Steps = 12 });
    await page.Mouse.UpAsync();
    EdgeBox after = await ShadowBoxAsync(page, qa, "root");
    double grown = after.Height - before.Height;
    string dragReport = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"qa={qa} drag={dragDistance:0.#} before={before.Height:0.#} after={after.Height:0.#} grown={grown:0.#}");
    Console.WriteLine(dragReport);
    grown.ShouldBeGreaterThan(dragDistance - 20d, dragReport);
    grown.ShouldBeLessThan(dragDistance + 20d, dragReport);
  }

  internal static async Task FileFeedbackAsync(IPage page, ConcurrentQueue<string> console, string title, string? kindLabel)
  {
    await OpenFeedbackAsync(page, console);
    if (kindLabel is not null)
    {
      await ChooseOptionAsync(page, "FeedbackKind", kindLabel);
    }

    await page.Locator("[data-qa=FeedbackTitle]").Locator("input").FillAsync(title);
    await page.Locator("[data-qa=FeedbackBody]").Locator("textarea").FillAsync("Filter me.");
    await page.Locator("[data-qa=FeedbackBody]").Locator("textarea").PressAsync("Tab");
    await SubmitFeedbackAsync(page);
  }

  internal static async Task ChooseOptionAsync(IPage page, string dataQa, string optionText)
  {
    ILocator host = page.Locator($"[data-qa={dataQa}]");
    await host.ClickAsync();
    ILocator option = page.Locator("fluent-option").Filter(new LocatorFilterOptions { HasText = optionText });
    await option.Last.ClickAsync(new LocatorClickOptions { Timeout = 10_000 });
  }

  internal static async Task OpenFeedbackAsync(IPage page, ConcurrentQueue<string> console)
  {
    string feedbackUrl = InProcTestPorts.WebHostUrl.TrimEnd('/') + "/Feedback";
    await page.GotoAsync(feedbackUrl, new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    try
    {
      await page.Locator(".twe-appbar").WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
      await page.Locator("[data-qa=FeedbackBody]").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    }
    catch (TimeoutException exception)
    {
      string startupLog = string.Join('\n', console);
      throw new TimeoutException(
        $"Feedback form did not render at {page.Url}. Console:\n{startupLog}",
        exception);
    }
  }

  internal static async Task AssertDetailsMatchesTitleAsync(IPage page, string viewport)
  {
    EdgeBox host = await HostBoxAsync(page, "FeedbackBody");
    EdgeBox titleRoot = await ShadowBoxAsync(page, "FeedbackTitle", "root");
    EdgeBox detailsRoot = await ShadowBoxAsync(page, "FeedbackBody", "root");
    EdgeBox titleControl = await ShadowBoxAsync(page, "FeedbackTitle", "control");
    EdgeBox detailsControl = await ShadowBoxAsync(page, "FeedbackBody", "control");
    string report = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"viewport={viewport} host={host.Width:0.#}x{host.Height:0.#} detailsRoot={detailsRoot.X:0.#}..{detailsRoot.Right:0.#} {detailsRoot.Width:0.#}x{detailsRoot.Height:0.#} titleRoot={titleRoot.X:0.#}..{titleRoot.Right:0.#} {titleRoot.Width:0.#}x{titleRoot.Height:0.#} detailsControl={detailsControl.X:0.#}..{detailsControl.Right:0.#} {detailsControl.Width:0.#}x{detailsControl.Height:0.#} titleControl={titleControl.X:0.#}..{titleControl.Right:0.#} {titleControl.Width:0.#}x{titleControl.Height:0.#}");
    Console.WriteLine(report);
    SameEdges(titleRoot, detailsRoot, report);
    SameEdges(titleControl, detailsControl, report);
    ((double)detailsRoot.Height).ShouldBeGreaterThanOrEqualTo(140d, report);
    ((double)detailsControl.Height).ShouldBeGreaterThan(100d, report);
  }

  internal static void SameEdges(EdgeBox expected, EdgeBox actual, string report)
  {
    ((double)Math.Abs(expected.X - actual.X)).ShouldBeLessThan(4d, report);
    ((double)Math.Abs(expected.Right - actual.Right)).ShouldBeLessThan(4d, report);
  }

  internal static async Task<EdgeBox> HostBoxAsync(IPage page, string qa)
  {
    LocatorBoundingBoxResult box = (await page.Locator($"[data-qa={qa}]").BoundingBoxAsync()).ShouldNotBeNull();
    return new EdgeBox(box.X, box.Y, box.Width, box.Height);
  }

  internal static async Task<EdgeBox> ShadowBoxAsync(IPage page, string qa, string part)
  {
    ILocator locator = page.Locator($"[data-qa={qa}]").Locator($"[part={part}]");
    LocatorBoundingBoxResult box = (await locator.BoundingBoxAsync()).ShouldNotBeNull($"{qa} part={part}");
    return new EdgeBox(box.X, box.Y, box.Width, box.Height);
  }

  internal readonly record struct EdgeBox(float X, float Y, float Width, float Height)
  {
    public float Right => X + Width;
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

  internal static string ScreenshotPath(string fileName) =>
    PlaywrightScreenshot.PathFor<FeedbackAttachment_Given_Wasm>(fileName);
}
