namespace EndToEnd.Playwright_.Tests;

class Program
{
  public static async Task Main(string[] args)
  {
    string projectName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "EndToEnd.Playwright.Tests";
    string runStamp = Environment.GetEnvironmentVariable("CI_RUN_ID") ?? DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
    const string testName = "main";

    string solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    string screenshotPath = Path.Combine(solutionRoot, "artifacts", projectName, runStamp, $"{testName}-home.png");
    Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!); // ensure artifacts tree exists before writing

    using IPlaywright playwright = await Playwright.CreateAsync();
    await using IBrowser browser = await playwright.Chromium.LaunchAsync
    (
      new BrowserTypeLaunchOptions { Headless = false, SlowMo = 50, }
    );
    IPage page = await browser.NewPageAsync();
    await page.GotoAsync("https://playwright.dev/dotnet");
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });
  }
}
