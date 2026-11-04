#region Purpose
// Browser proof for tasks 418 and 300: the admin renames the assistant and every Ask entry point
// follows; Ask answers a passkey question through site_map and navigates after approval.
#endregion

#region Design
// Task 418: signed in as the first (Administrator) account, /Admin/AI renames the
// assistant to Zawadi. Without a reload the top-bar button reads "Ask Zawadi", the derived Ctrl-K
// row reads "Ask Zawadi", and the panel it opens has the header Zawadi with its avatar and the
// greeting "Hi, I'm Zawadi"; no "Akili" is left in the panel (assistant-rename-*.png).
// Task 300: signed in, back on Home, Ask answers "Where can I add a passkey?" through site_map:
// the fake upstream links the first site map page whose title or description names a passkey,
// which is Settings, and the request carried site_map, navigate and a site map naming /Settings
// in its instructions. "Take me there" calls navigate; in Ask before editing the approval card
// shows navigate and /Settings and the page stays on Home until Approve, then the app lands on
// /Settings without a reload (ask-home-where-passkey*.png, ask-navigate-approval.png,
// ask-navigate-landed-settings.png).
// Task 381: Ask opened from the Ctrl-K row leaves the caret in the composer (the palette's own focus
// return does not land on top of it), so the question is typed with the keyboard and no click. While
// the navigate approval card waits, the composer does not take focus; the check waits a second
// first so a late focus move would be seen.
#endregion

namespace AskSignInPlaywright_;

using TimeWarp.Architecture.Features.Applications;
using static AskSignInPlaywright_.AskSignInTestFixture;

public class AskSignInRenameNavigate_Given_Wasm
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskSignInRenameNavigate_Given_Wasm>();

  private static string ScreenshotPath(string fileName) =>
    PlaywrightScreenshot.PathFor<AskSignInRenameNavigate_Given_Wasm>(fileName);

  public static async Task AdminRename_Should_Relabel_Every_Ask_Entry_Point()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
      services.PostConfigure<XaiChatOptions>(options =>
      {
        options.UseFakeUpstream = true;
        options.ApiKey = "playwright-placeholder-key";
      }));

    await using Session session = await Session.StartAsync<AskSignInRenameNavigate_Given_Wasm>();
    IPage page = session.Page;
    await session.GoHomeAsync();
    ILocator askButton = page.Locator("[data-qa=AskAiButton]");
    await Assertions.Expect(askButton).ToHaveTextAsync("Ask Akili", new LocatorAssertionsToHaveTextOptions { Timeout = 30_000 });
    await session.SignInWithNewPasskeyAsync();

    // Rename through the Admin AI form (UpdateAssistantName and its Version token).
    await page.GotoAsync(InProcTestPorts.WebHostUrl.TrimEnd('/') + "/Admin/AI", new PageGotoOptions
    {
      WaitUntil = WaitUntilState.DOMContentLoaded,
      Timeout = 120_000,
    });
    ILocator nameInput = page.Locator("[data-qa=AssistantName]").Locator("input");
    await nameInput.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
    await Assertions.Expect(nameInput).ToHaveValueAsync("Akili", new LocatorAssertionsToHaveValueOptions { Timeout = 30_000 });
    await nameInput.FillAsync("Zawadi");
    await nameInput.PressAsync("Tab");
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("assistant-rename-admin.png") });
    await page.Locator("[data-qa=AssistantNameSave]").ClickAsync();

    // Top bar: the page reloads the public identity after a successful save, so no page load.
    await Assertions.Expect(askButton).ToHaveTextAsync("Ask Zawadi", new LocatorAssertionsToHaveTextOptions { Timeout = 30_000 });

    // Ctrl-K: the derived AgentSurface.OpenAskPanel row carries the runtime override.
    ILocator paletteInput = page.Locator("[data-qa=CommandPaletteInput]");
    bool opened = false;
    for (int attempt = 0; attempt < 20 && !opened; attempt++)
    {
      await page.Keyboard.PressAsync("Control+k");
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

    opened.ShouldBeTrue("Ctrl-K did not open the palette");
    await paletteInput.FillAsync("zawadi");
    ILocator active = page.Locator(".twe-palette__row--active");
    await Assertions.Expect(active.Locator(".twe-palette__name"))
      .ToHaveTextAsync("Ask Zawadi", new LocatorAssertionsToHaveTextOptions { Timeout = 10_000 });
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("assistant-rename-ctrl-k.png") });
    await paletteInput.PressAsync("Enter");

    // Panel: header name and avatar, and the empty-state greeting once the agent is built.
    ILocator panel = page.Locator("[data-qa=AgentAsk]");
    await panel.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
    await Assertions.Expect(page.Locator("[data-qa=AskTitle]"))
      .ToHaveTextAsync("Zawadi", new LocatorAssertionsToHaveTextOptions { Timeout = 30_000 });
    await Assertions.Expect(page.Locator("[data-qa=AskAvatar]"))
      .ToHaveAttributeAsync("alt", "Zawadi", new LocatorAssertionsToHaveAttributeOptions { Timeout = 30_000 });
    await Assertions.Expect(page.Locator("[data-qa=AskGreeting]"))
      .ToContainTextAsync("Hi, I'm Zawadi", new LocatorAssertionsToContainTextOptions { Timeout = 60_000 });
    (await page.Locator("[data-qa=AskEmptyAvatar]").GetAttributeAsync("alt")).ShouldBe("Zawadi");
    await panel.ScreenshotAsync(new LocatorScreenshotOptions { Path = ScreenshotPath("assistant-rename-panel.png") });
    (await panel.InnerTextAsync()).ShouldNotContain("Akili");
    session.AssertNoWasmFailures();
  }

  public static async Task Home_Should_Point_Passkeys_To_Settings_And_Navigate_After_Approval()
  {
    await using HostGraph graph = await HostGraphFactory.CreateWebAsync(services =>
      services.PostConfigure<XaiChatOptions>(options =>
      {
        options.UseFakeUpstream = true;
        options.ApiKey = "playwright-placeholder-key";
      }));

    await using Session session = await Session.StartAsync<AskSignInRenameNavigate_Given_Wasm>();
    IPage page = session.Page;
    await session.GoHomeAsync();
    await session.WaitForConfigurationProbeAsync(0);
    int signedOutProbes = session.ConfigurationStatuses.Count;
    await session.SignInWithNewPasskeyAsync();
    await session.WaitForConfigurationProbeAsync(signedOutProbes);

    // Back to Home: the page Steven asked from, with no passkey controls of its own.
    await session.GoHomeAsync();
    new Uri(page.Url).AbsolutePath.ShouldBe("/");
    string state = await session.OpenAskAsync(signedIn: true);
    state.ShouldBe("Configured", $"Ask text: {await session.AskTextAsync()}");
    await Assertions.Expect(page.Locator("[data-qa=AskEditMode]"))
      .ToHaveAttributeAsync("data-mode", nameof(AgentEditMode.AskBeforeEditing), new LocatorAssertionsToHaveAttributeOptions { Timeout = 30_000 });

    // The fake upstream calls site_map for a "where" question and links the matching page.
    int completionsBefore = session.Completions.Count;
    ILocator input = page.Locator(".sc-ai-input__textarea");
    await Assertions.Expect(input).ToBeFocusedAsync(new LocatorAssertionsToBeFocusedOptions { Timeout = 30_000 });
    await page.Keyboard.TypeAsync("Where can I add a passkey?");
    await Assertions.Expect(input).ToHaveValueAsync("Where can I add a passkey?");
    await page.Locator(".sc-ai-input__send").ClickAsync();
    ILocator answer = page.Locator(".sc-ai-root").GetByText("You can do that on the");
    await answer.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
    string answered = await page.Locator(".sc-ai-root").InnerTextAsync();
    answered.ShouldContain("Settings");
    answered.ShouldNotContain("doesn't expose");
    session.Completions.Count.ShouldBeGreaterThan(completionsBefore);
    string firstRequest = session.Completions[completionsBefore];
    firstRequest.ShouldContain("\"site_map\"");
    firstRequest.ShouldContain("\"navigate\"");
    InstructionsOf(firstRequest).ShouldContain("/Settings");
    InstructionsOf(firstRequest).ShouldNotContain("this page only");
    // The link is the page's url, whether the answer renders markdown or shows it as text.
    bool linked = await page.Locator(".sc-ai-root a[href$='/Settings']").CountAsync() > 0;
    (linked || answered.Contains("](/Settings)", StringComparison.Ordinal)).ShouldBeTrue($"Answer: {answered}");
    await page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-home-where-passkey.png"),
    });
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("ask-home-where-passkey-page.png") });

    // "take me there": navigate asks first in Ask before editing, and nothing moves until approved.
    await input.FillAsync("Take me there");
    await page.Locator(".sc-ai-input__send").ClickAsync();
    ILocator approval = page.Locator("[data-qa=AgentApproval]").Last;
    await approval.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
    (await approval.InnerTextAsync()).ShouldContain("navigate");
    (await page.Locator("[data-qa=AgentApprovalArguments]").Last.InnerTextAsync()).ShouldContain("/Settings");
    new Uri(page.Url).AbsolutePath.ShouldBe("/");
    await page.WaitForTimeoutAsync(1_000);
    await Assertions.Expect(input).Not.ToBeFocusedAsync();
    await page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-navigate-approval.png"),
    });

    await approval.Locator("[data-qa=AgentApprove]").ClickAsync();
    await page.WaitForURLAsync("**/Settings", new PageWaitForURLOptions { Timeout = 60_000 });
    await page.Locator("[data-qa=CreatePasskey], .twe-appbar").First.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("ask-navigate-landed-settings.png") });
    session.AssertNoWasmFailures();
  }
}
