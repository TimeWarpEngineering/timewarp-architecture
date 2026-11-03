#region Purpose
// Browser proof for task 381: focus handling when Ask opens from the Ctrl-K row or the search field,
// and when the search-field palette closes after Ask.
#endregion

#region Design
// Task 381: Ask opened from the Ctrl-K row leaves the caret in the composer (the palette's own focus
// return does not land on top of it), so the question is typed with the keyboard and no click. While
// the navigate approval card waits, the composer does not take focus; the check waits a second
// first so a late focus move would be seen. Ask opened from the palette that the search field
// opened returns focus to the search field on close without reopening the palette.
#endregion

namespace AskSignInPlaywright_;

using static AskSignInPlaywright_.AskSignInTestFixture;

public class AskSignInSearchFocus_Given_Wasm
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskSignInSearchFocus_Given_Wasm>();

  public static async Task Close_Should_Not_Reopen_Palette_When_Ask_Came_From_Search_Field()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
      services.PostConfigure<XaiChatOptions>(options =>
      {
        options.UseFakeUpstream = true;
        options.ApiKey = "playwright-placeholder-key";
      }));

    await using Session session = await Session.StartAsync();
    IPage page = session.Page;
    await session.GoHomeAsync();
    await session.WaitForConfigurationProbeAsync(0);
    int signedOutProbes = session.ConfigurationStatuses.Count;
    await session.SignInWithNewPasskeyAsync();
    await session.WaitForConfigurationProbeAsync(signedOutProbes);
    await session.GoHomeAsync();

    // Open the palette from the appbar search field (its trigger), not Ctrl-K.
    ILocator search = page.Locator(".twe-appbar__search");
    ILocator paletteInput = page.Locator("[data-qa=CommandPaletteInput]");
    bool opened = false;
    for (int attempt = 0; attempt < 20 && !opened; attempt++)
    {
      await search.ClickAsync();
      try
      {
        await paletteInput.WaitForAsync(new LocatorWaitForOptions { Timeout = 2_000, State = WaitForSelectorState.Visible });
        opened = true;
      }
      catch (TimeoutException)
      {
        await page.Keyboard.PressAsync("Escape");
      }
    }

    opened.ShouldBeTrue("The search field did not open the palette");
    await paletteInput.FillAsync("ask");
    ILocator active = page.Locator(".twe-palette__row--active");
    await Assertions.Expect(active.Locator(".twe-palette__kind"))
      .ToHaveTextAsync("Command", new LocatorAssertionsToHaveTextOptions { Timeout = 10_000 });
    await Assertions.Expect(active.Locator(".twe-palette__name"))
      .ToContainTextAsync("ask", new LocatorAssertionsToContainTextOptions { IgnoreCase = true, Timeout = 10_000 });
    await paletteInput.PressAsync("Enter");
    await page.Locator("[data-qa=AgentAsk]").WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
    await Assertions.Expect(page.Locator(".sc-ai-input__textarea"))
      .ToBeFocusedAsync(new LocatorAssertionsToBeFocusedOptions { Timeout = 30_000 });

    // Close hands focus back to the search field without reopening the palette.
    await page.Locator("[data-qa=AgentAskClose]").ClickAsync();
    await page.Locator("[data-qa=AgentAsk]").WaitForAsync(new LocatorWaitForOptions
    {
      State = WaitForSelectorState.Detached,
      Timeout = 30_000,
    });
    await page.WaitForTimeoutAsync(1_000);
    (await page.Locator("[data-qa=CommandPalette]").IsVisibleAsync()).ShouldBeFalse("Closing Ask reopened the command palette");
    (await page.EvaluateAsync<bool>("() => document.activeElement?.closest('.twe-appbar__search') != null"))
      .ShouldBeTrue("Focus did not return to the search field");
    session.AssertNoWasmFailures();
  }
}
