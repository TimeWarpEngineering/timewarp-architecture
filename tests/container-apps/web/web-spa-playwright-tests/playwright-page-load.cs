#region Purpose
// One full-page navigation for a signed-in test: load the document, then wait until the page has
// finished authorizing and rendered its shell before the test asserts anything.
#endregion

#region Design
// The suite runs with prerender off. After a document load the DOM is empty, then the router shows
// its Authorizing... placeholder, then the page. DOMContentLoaded fires before any of that, and a
// default expect only waits 5 seconds. On a loaded machine (other browser tests running) WASM
// start plus authorization took longer than 5 seconds (task 448).
// The landmark is the app bar. TimeWarpPage renders it on every page, and the whole page sits
// inside AuthorizeRouteView, so it appears only after authorization finishes. 30 seconds matches
// the suite's other in-test waits. A first load in a fresh browser context downloads WASM and
// keeps its own 120-second wait (the sign-in helpers).
#endregion

namespace WebSpaPlaywright_;

/// <summary>Full-page navigations that return once the page has rendered after authorization.</summary>
public static class PlaywrightPageLoad
{
  public const int ShellTimeout = 30_000;

  /// <summary>Loads <paramref name="path"/> on the in-process web host and waits for the shell.</summary>
  public static async Task GotoAsync(IPage page, string path)
  {
    await page.GotoAsync(InProcTestPorts.WebHostUrl.TrimEnd('/') + path, new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    await WaitForShellAsync(page);
  }

  /// <summary>Reloads the current document and waits for the shell.</summary>
  public static async Task ReloadAsync(IPage page)
  {
    await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
    await WaitForShellAsync(page);
  }

  private static async Task WaitForShellAsync(IPage page)
  {
    try
    {
      await page.Locator(".twe-appbar").WaitForAsync(new LocatorWaitForOptions { Timeout = ShellTimeout });
    }
    catch (TimeoutException exception)
    {
      string text = await page.Locator("body").InnerTextAsync();
      throw new TimeoutException($"The page did not finish authorizing at {page.Url}. Page:\n{text}", exception);
    }
  }
}
