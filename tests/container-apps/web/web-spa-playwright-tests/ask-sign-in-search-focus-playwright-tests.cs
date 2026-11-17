#region Purpose
// Browser proof for task 381: when Ask opens from the palette that the app-bar search field opened,
// closing Ask returns focus to the search field and does not reopen the palette.
#endregion

#region Design
// Task 381: the palette is opened from the search field (its trigger), not Ctrl-K, and Ask is
// chosen from it. Closing Ask must hand focus back to the search field without the palette
// reopening. The Ctrl-K focus checks are in AskSignInRenameNavigate_Given_Wasm.
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

    await using Session session = await Session.StartAsync<AskSignInSearchFocus_Given_Wasm>();
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
