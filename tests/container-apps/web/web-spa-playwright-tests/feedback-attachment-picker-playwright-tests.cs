#region Purpose
// Browser proof for task 511: the Feedback Attachments field opens the file picker from a FluentUI
// "Choose files" button (click, Enter and Space), never shows the browser's own file widget, and
// spaces its action row and chips with the design-system tokens, in light and dark.
#endregion

#region Design
// Same host and passkey sign-in as FeedbackAttachment_Given_Wasm. Each theme gets its own browser
// context (a fresh account), and dark mode is the stored Fluent theme setting the theme button
// leaves behind (DarkTheme_Given_Shell). FluentInputFile wires the button in OnAfterRenderAsync by
// setting fluentuiBlazorFileInputHandlerAttached on it, so the test waits for that attribute
// before the first click. Playwright's file chooser event proves the picker opened; the chooser
// then sets two files, which upload through OnFilesAsync like any pick. Spacing is measured from
// bounding boxes: buttons in the row sit --twe-space-3 (12px) apart and the chips list starts
// --twe-space-2 (8px) under the row. Element shots of the Attachments field (empty, two files,
// a captured page) go to test output; the task copies them into its evidence folder by hand.
#endregion

namespace FeedbackAttachmentPlaywright_;

using static FeedbackAttachmentPlaywright_.FeedbackAttachmentTestFixture;

public class FeedbackAttachmentPicker_Given_Wasm
{
  private const string StoreDarkScript =
    "localStorage.setItem('fluentui-blazor:theme-settings', JSON.stringify({ mode: 'dark' }));";

  private const string ChooseFiles = "[data-qa=FeedbackChooseFiles]";

  private const string PngBase64 =
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FeedbackAttachmentPicker_Given_Wasm>();

  public static async Task ChooseFiles_Should_OpenThePicker_And_SpaceTheActions()
  {
    await using HostGraph graph = await PostgresWebHost.CreateWebAsync();
    IBrowser browser = (await PlaywrightBrowserSession.GetAsync()).Inner;
    await ProveThemeAsync(browser, dark: false);
    await ProveThemeAsync(browser, dark: true);
  }

  private static async Task ProveThemeAsync(IBrowser browser, bool dark)
  {
    string theme = dark ? "dark" : "light";
    await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
      Locale = "en-US",
      ColorScheme = ColorScheme.Light,
    });
    if (dark)
    {
      await context.AddInitScriptAsync(StoreDarkScript);
    }

    IPage page = await context.NewPageAsync();
    ConcurrentQueue<string> console = new();
    page.Console += (_, message) => console.Enqueue($"{message.Type}: {message.Text}");
    page.PageError += (_, error) => console.Enqueue(error);

    await SignInWithNewPasskeyAsync(page, context);
    await OpenFeedbackAsync(page, console);
    await page.WaitForFunctionAsync(
      $"() => (document.body.getAttribute('data-theme') ?? 'light') === '{theme}'",
      null,
      new PageWaitForFunctionOptions { Timeout = 30_000 });

    ILocator field = page.Locator(".twe-form-field").Filter(new LocatorFilterOptions { Has = page.Locator(ChooseFiles) });
    ILocator input = page.Locator("[data-qa=FeedbackFile]");
    ILocator button = page.Locator(ChooseFiles);
    ILocator attachments = page.Locator("[data-qa=FeedbackAttachment]");

    // The native widget is hidden; the hidden input keeps its name and the picker settings.
    (await input.EvaluateAsync<string>("element => element.tagName")).ShouldBe("INPUT");
    (await input.GetAttributeAsync("type")).ShouldBe("file");
    (await input.GetAttributeAsync("aria-label")).ShouldBe("Attachments");
    (await input.GetAttributeAsync("multiple")).ShouldNotBeNull();
    (await input.IsVisibleAsync()).ShouldBeFalse($"{theme}: the native file widget is visible");
    (await field.InnerTextAsync()).ShouldNotContain("No file chosen");
    (await button.EvaluateAsync<string>("element => element.tagName")).ShouldBe("FLUENT-BUTTON");
    (await button.InnerTextAsync()).Trim().ShouldBe("Choose files");
    (await attachments.CountAsync()).ShouldBe(0);
    (await page.Locator(".feedback-attachments").CountAsync()).ShouldBe(0, "an empty chips list renders nothing");

    await AssertActionRowAsync(page, theme);
    await field.ScreenshotAsync(new LocatorScreenshotOptions { Path = ShotPath($"attachments-empty-{theme}.png") });

    await page.Locator($"{ChooseFiles}[fluentuiBlazorFileInputHandlerAttached]")
      .WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });

    // Keyboard: the button takes focus and Enter and Space each open the picker.
    foreach (string key in new[] { "Enter", " " })
    {
      await button.FocusAsync();
      (await button.EvaluateAsync<bool>("element => document.activeElement === element"))
        .ShouldBeTrue($"{theme}: Choose files did not take focus");
      IFileChooser keyed = await page.RunAndWaitForFileChooserAsync(
        () => page.Keyboard.PressAsync(key),
        new PageRunAndWaitForFileChooserOptions { Timeout = 10_000 });
      keyed.IsMultiple.ShouldBeTrue();
    }

    // Click opens the picker; the chosen files upload through OnFilesAsync.
    IFileChooser chooser = await page.RunAndWaitForFileChooserAsync(
      () => button.ClickAsync(),
      new PageRunAndWaitForFileChooserOptions { Timeout = 10_000 });
    chooser.IsMultiple.ShouldBeTrue();
    await chooser.SetFilesAsync(
    [
      new FilePayload { Name = "steps.txt", MimeType = "text/plain", Buffer = "steps to reproduce"u8.ToArray() },
      new FilePayload { Name = "pixel.png", MimeType = "image/png", Buffer = Convert.FromBase64String(PngBase64) },
    ]);
    await Assertions.Expect(attachments).ToHaveCountAsync(2, new LocatorAssertionsToHaveCountOptions { Timeout = 30_000 });
    await AssertChipsUnderActionsAsync(page, theme);
    await field.ScreenshotAsync(new LocatorScreenshotOptions { Path = ShotPath($"attachments-two-files-{theme}.png") });

    // A captured page is one chip with its preview.
    await attachments.First.Locator("[data-qa=FeedbackAttachmentRemove]").ClickAsync();
    await Assertions.Expect(attachments).ToHaveCountAsync(1, new LocatorAssertionsToHaveCountOptions { Timeout = 30_000 });
    await attachments.First.Locator("[data-qa=FeedbackAttachmentRemove]").ClickAsync();
    await Assertions.Expect(attachments).ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = 30_000 });
    await page.Locator("[data-qa=FeedbackCaptureScreenshot]").ClickAsync();
    await Assertions.Expect(attachments).ToHaveCountAsync(1, new LocatorAssertionsToHaveCountOptions { Timeout = 30_000 });
    await attachments.First.Locator("img").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    await AssertChipsUnderActionsAsync(page, theme);
    await field.ScreenshotAsync(new LocatorScreenshotOptions { Path = ShotPath($"attachments-captured-page-{theme}.png") });

    string log = string.Join('\n', console);
    log.ShouldNotContain("Unhandled exception rendering component");
  }

  // Every button in the action row sits --twe-space-3 (12px) after the one before it on the same line.
  private static async Task AssertActionRowAsync(IPage page, string theme)
  {
    double[] gaps = await page.EvaluateAsync<double[]>(
      """
      () => {
        const buttons = [...document.querySelectorAll('.feedback-attachment-actions > fluent-button')];
        const gaps = [];
        for (let index = 1; index < buttons.length; index++) {
          const before = buttons[index - 1].getBoundingClientRect();
          const after = buttons[index].getBoundingClientRect();
          if (Math.abs(before.top - after.top) < 1) { gaps.push(after.left - before.right); }
        }
        return gaps;
      }
      """);
    gaps.ShouldNotBeEmpty($"{theme}: the action row has fewer than two buttons on one line");
    foreach (double gap in gaps)
    {
      gap.ShouldBe(12, 0.5, $"{theme}: action row gap");
    }
  }

  // The chips list starts --twe-space-2 (8px) under the action row.
  private static async Task AssertChipsUnderActionsAsync(IPage page, string theme)
  {
    double gap = await page.EvaluateAsync<double>(
      """
      () => {
        const actions = document.querySelector('.feedback-attachment-actions').getBoundingClientRect();
        const chips = document.querySelector('.feedback-attachments').getBoundingClientRect();
        return chips.top - actions.bottom;
      }
      """);
    gap.ShouldBe(8, 0.5, $"{theme}: chips gap under the action row");
  }

  private static string ShotPath(string fileName) =>
    PlaywrightScreenshot.PathFor<FeedbackAttachmentPicker_Given_Wasm>(fileName);
}
