#region Purpose
// Browser proof for the admin feedback grids: sort, filter, owner link and narrow-width layout.
#endregion

#region Design
// Same host as the other WASM proofs: appsettings UseMock on the server, wwwroot UseMock left
// false so the page calls the real in-memory API. /Feedback is [Authorize] for
// feedback.file.self. That gate reads the SPA authentication state, which comes from the
// identity-session cookie. X-TimeWarp-Mock-Principal-Id authenticates server API calls only,
// so a direct visit while signed out renders RedirectToLogin (no app bar). The test creates
// an account with a virtual passkey, the same ceremony as AskSignInReprobe_Given_Wasm, then opens
// /Feedback. A new account's role includes feedback.file.self. Shots go to test output
// (PlaywrightScreenshot). Each test boots its own host (the admin proofs need a fresh one) and
// opens its own context on the suite's shared browser (PlaywrightBrowserSession).
// The admin list sort proof (task 327) files two items, opens /Admin/Feedback, clicks the
// Title header's sort button, and checks the rows re-order (server-side sort) and aria-sort
// toggles ascending then descending. The owner proof (task 336) files one item as that
// Administrator, opens /Admin/Feedback, and checks the Owner cell links to
// /Admin/Principals/{id} and shows the display name IProfileDirectory holds for that id, not the
// raw id; following the link lands on the principal detail page. The permalink proof (task 353)
// checks the pasted image renders once, inline in the safe-markdown body, and that attachments
// the body references are not listed again under it. The narrow-grid proof (task 354) opens
// /Admin/Feedback at 1280 with the Ask panel docked, then both admin grids at 640: header cells
// never overlap, the primary column keeps its width and its link opens the item, and below the
// grid's minimum the DataGridFrame scrolls while the page does not. The filter proof
// (task 379-004) files a bug and a feature request, then uses the admin Kind select to
// leave only the feature row, and the search box to leave only the bug row.
#endregion

namespace FeedbackAttachmentPlaywright_;

using static FeedbackAttachmentPlaywright_.FeedbackAttachmentTestFixture;

public class FeedbackAttachmentAdmin_Given_Wasm
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FeedbackAttachmentAdmin_Given_Wasm>();

  private static string ScreenshotPath(string fileName) =>
    PlaywrightScreenshot.PathFor<FeedbackAttachmentAdmin_Given_Wasm>(fileName);

  public static async Task AdminListHeaderClick_Should_ReorderRows_And_ToggleDirection()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync();
    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
    await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
      Locale = "en-US",
    });
    IPage page = await context.NewPageAsync();
    ConcurrentQueue<string> console = new();
    page.Console += (_, message) => console.Enqueue($"{message.Type}: {message.Text}");
    page.PageError += (_, error) => console.Enqueue(error);

    await SignInWithNewPasskeyAsync(page, context);
    // Alpha is filed first, so the default newest-first order is Zulu, Alpha.
    foreach (string title in new[] { "Alpha sort", "Zulu sort" })
    {
      await OpenFeedbackAsync(page, console);
      await page.Locator("[data-qa=FeedbackTitle]").Locator("input").FillAsync(title);
      await page.Locator("[data-qa=FeedbackBody]").Locator("textarea").FillAsync("Sort me.");
      await page.Locator("[data-qa=FeedbackBody]").Locator("textarea").PressAsync("Tab");
      await SubmitFeedbackAsync<FeedbackAttachmentAdmin_Given_Wasm>(page);
    }

    await page.GotoAsync(InProcTestPorts.WebHostUrl.TrimEnd('/') + "/Admin/Feedback", new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    ILocator rows = page.Locator("[data-qa=AdminFeedbackRow]");
    try
    {
      await Assertions.Expect(rows).ToHaveTextAsync(["Zulu sort", "Alpha sort"], new LocatorAssertionsToHaveTextOptions { Timeout = 120_000 });
    }
    catch (PlaywrightException exception)
    {
      await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("admin-feedback-sort-failed.png"), FullPage = true });
      string text = await page.Locator("body").InnerTextAsync();
      string log = string.Join('\n', console);
      throw new TimeoutException($"Admin feedback list did not render newest first at {page.Url}. Page:\n{text}\nConsole:\n{log}", exception);
    }

    ILocator grid = page.Locator("[data-qa=AdminFeedbackTable]");
    ILocator titleHeader = grid.GetByRole(AriaRole.Columnheader).Filter(new LocatorFilterOptions { HasText = "Title" });
    ILocator filedHeader = grid.GetByRole(AriaRole.Columnheader).Filter(new LocatorFilterOptions { HasText = "Filed" });
    await Assertions.Expect(filedHeader).ToHaveAttributeAsync("aria-sort", "descending");

    await titleHeader.Locator("[col-sort-button]").ClickAsync();
    await Assertions.Expect(rows).ToHaveTextAsync(["Alpha sort", "Zulu sort"], new LocatorAssertionsToHaveTextOptions { Timeout = 30_000 });
    await Assertions.Expect(titleHeader).ToHaveAttributeAsync("aria-sort", "ascending");
    await Assertions.Expect(filedHeader).Not.ToHaveAttributeAsync("aria-sort", "descending");
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("admin-feedback-sort-title-ascending.png"), FullPage = true });

    await titleHeader.Locator("[col-sort-button]").ClickAsync();
    await Assertions.Expect(titleHeader).ToHaveAttributeAsync("aria-sort", "descending", new LocatorAssertionsToHaveAttributeOptions { Timeout = 30_000 });
    await Assertions.Expect(rows).ToHaveTextAsync(["Zulu sort", "Alpha sort"], new LocatorAssertionsToHaveTextOptions { Timeout = 30_000 });
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("admin-feedback-sort-title-descending.png"), FullPage = true });
  }

  public static async Task AdminListFilters_Should_NarrowTheRows()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync();
    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
    await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
      Locale = "en-US",
    });
    IPage page = await context.NewPageAsync();
    ConcurrentQueue<string> console = new();
    page.Console += (_, message) => console.Enqueue($"{message.Type}: {message.Text}");
    page.PageError += (_, error) => console.Enqueue(error);

    await SignInWithNewPasskeyAsync(page, context);
    await FileFeedbackAsync<FeedbackAttachmentAdmin_Given_Wasm>(page, console, "Filter bug", kindLabel: null);
    await FileFeedbackAsync<FeedbackAttachmentAdmin_Given_Wasm>(page, console, "Filter feature", kindLabel: "Feature request");

    await page.GotoAsync(InProcTestPorts.WebHostUrl.TrimEnd('/') + "/Admin/Feedback", new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    ILocator rows = page.Locator("[data-qa=AdminFeedbackRow]");
    try
    {
      await Assertions.Expect(rows).ToHaveTextAsync(["Filter feature", "Filter bug"], new LocatorAssertionsToHaveTextOptions { Timeout = 120_000 });
    }
    catch (PlaywrightException exception)
    {
      await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("admin-feedback-filter-failed.png"), FullPage = true });
      string text = await page.Locator("body").InnerTextAsync();
      string log = string.Join('\n', console);
      throw new TimeoutException($"Admin feedback list did not render both filings at {page.Url}. Page:\n{text}\nConsole:\n{log}", exception);
    }

    await ChooseOptionAsync(page, "AdminFeedbackKind", "FeatureRequest");
    await Assertions.Expect(rows).ToHaveTextAsync(["Filter feature"], new LocatorAssertionsToHaveTextOptions { Timeout = 30_000 });
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("admin-feedback-filter-kind.png"), FullPage = true });

    await ChooseOptionAsync(page, "AdminFeedbackKind", "All kinds");
    await Assertions.Expect(rows).ToHaveTextAsync(["Filter feature", "Filter bug"], new LocatorAssertionsToHaveTextOptions { Timeout = 30_000 });

    await page.Locator(".twe-filter-bar input").FillAsync("bug");
    await Assertions.Expect(rows).ToHaveTextAsync(["Filter bug"], new LocatorAssertionsToHaveTextOptions { Timeout = 30_000 });
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("admin-feedback-filter-search.png"), FullPage = true });
  }

  public static async Task AdminListOwnerCell_Should_ShowTheDisplayName_And_LinkToThePrincipal()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync();
    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
    await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
      Locale = "en-US",
    });
    IPage page = await context.NewPageAsync();
    ConcurrentQueue<string> console = new();
    page.Console += (_, message) => console.Enqueue($"{message.Type}: {message.Text}");
    page.PageError += (_, error) => console.Enqueue(error);

    await SignInWithNewPasskeyAsync(page, context);
    await OpenFeedbackAsync(page, console);
    await page.Locator("[data-qa=FeedbackTitle]").Locator("input").FillAsync("Owner name");
    await page.Locator("[data-qa=FeedbackBody]").Locator("textarea").FillAsync("Who filed me?");
    await page.Locator("[data-qa=FeedbackBody]").Locator("textarea").PressAsync("Tab");
    await SubmitFeedbackAsync<FeedbackAttachmentAdmin_Given_Wasm>(page);

    await page.GotoAsync(InProcTestPorts.WebHostUrl.TrimEnd('/') + "/Admin/Feedback", new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    ILocator owner = page.Locator("[data-qa=AdminFeedbackOwner]").First;
    try
    {
      await owner.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
    }
    catch (TimeoutException exception)
    {
      await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("admin-feedback-owner-failed.png"), FullPage = true });
      string text = await page.Locator("body").InnerTextAsync();
      string log = string.Join('\n', console);
      throw new TimeoutException($"Admin feedback list showed no owner cell at {page.Url}. Page:\n{text}\nConsole:\n{log}", exception);
    }

    string href = (await owner.GetAttributeAsync("href")).ShouldNotBeNull();
    href.ShouldStartWith("/Admin/Principals/");
    Guid ownerId = Guid.Parse(href["/Admin/Principals/".Length..]);

    await using AsyncServiceScope scope = graph.Web!.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IReadOnlyDictionary<Guid, TimeWarp.Architecture.Abstractions.ProfileDirectoryEntry> profiles = await scope.ServiceProvider
      .GetRequiredService<TimeWarp.Architecture.Abstractions.IProfileDirectory>()
      .FindManyAsync([ownerId]);
    profiles.ShouldContainKey(ownerId, "signing in opens Settings, which creates the profile");
    string displayName = profiles[ownerId].DisplayName;

    await Assertions.Expect(owner).ToHaveTextAsync(displayName, new LocatorAssertionsToHaveTextOptions { Timeout = 30_000 });
    (await owner.InnerTextAsync()).ShouldNotContain(ownerId.ToString());
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("admin-feedback-owner-name.png"), FullPage = true });

    await owner.ClickAsync();
    await page.WaitForURLAsync($"**/Admin/Principals/{ownerId}", new PageWaitForURLOptions { Timeout = 30_000 });
    await page.Locator("[data-qa=AdminPrincipalStatus]").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
  }

  public static async Task AdminGrids_Should_KeepThePrimaryColumn_WhenNarrow_Or_AskIsOpen()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync();
    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
    await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
      Locale = "en-US",
    });
    IPage page = await context.NewPageAsync();
    ConcurrentQueue<string> console = new();
    page.Console += (_, message) => console.Enqueue($"{message.Type}: {message.Text}");
    page.PageError += (_, error) => console.Enqueue(error);

    await SignInWithNewPasskeyAsync(page, context);
    await OpenFeedbackAsync(page, console);
    await page.Locator("[data-qa=FeedbackTitle]").Locator("input").FillAsync("A long feedback title that must stay visible beside the Ask panel");
    await page.Locator("[data-qa=FeedbackBody]").Locator("textarea").FillAsync("Narrow grid.");
    await page.Locator("[data-qa=FeedbackBody]").Locator("textarea").PressAsync("Tab");
    await SubmitFeedbackAsync<FeedbackAttachmentAdmin_Given_Wasm>(page);

    // The reported case: 1280 wide with the Ask panel docked beside the content.
    await OpenAdminGridAsync<FeedbackAttachmentAdmin_Given_Wasm>(page, console, "/Admin/Feedback", "AdminFeedbackRow");
    await OpenAskPanelAsync(page);
    await AssertGridLayoutAsync<FeedbackAttachmentAdmin_Given_Wasm>(page, "AdminFeedbackTable", "AdminFeedbackRow", "feedback-ask-open-1280");
    await page.Locator("[data-qa=AdminFeedbackRow]").First.ClickAsync();
    await page.WaitForURLAsync("**/Admin/Feedback/*", new PageWaitForURLOptions { Timeout = 30_000 });

    // Narrow viewports: the grid keeps its minimum and scrolls inside its card.
    await page.SetViewportSizeAsync(640, 900);
    await OpenAdminGridAsync<FeedbackAttachmentAdmin_Given_Wasm>(page, console, "/Admin/Feedback", "AdminFeedbackRow");
    await AssertGridLayoutAsync<FeedbackAttachmentAdmin_Given_Wasm>(page, "AdminFeedbackTable", "AdminFeedbackRow", "feedback-640");
    await AssertGridScrollsAsync(page, "AdminFeedbackTable");
    await page.Locator("[data-qa=AdminFeedbackRow]").First.ClickAsync();
    await page.WaitForURLAsync("**/Admin/Feedback/*", new PageWaitForURLOptions { Timeout = 30_000 });

    await OpenAdminGridAsync<FeedbackAttachmentAdmin_Given_Wasm>(page, console, "/Admin/Principals", "AdminPrincipalRow");
    await AssertGridLayoutAsync<FeedbackAttachmentAdmin_Given_Wasm>(page, "PrincipalsTable", "AdminPrincipalRow", "principals-640");
    await AssertGridScrollsAsync(page, "PrincipalsTable");
    await page.Locator("[data-qa=AdminPrincipalRow]").First.ClickAsync();
    await page.WaitForURLAsync("**/Admin/Principals/*", new PageWaitForURLOptions { Timeout = 30_000 });
  }
}
