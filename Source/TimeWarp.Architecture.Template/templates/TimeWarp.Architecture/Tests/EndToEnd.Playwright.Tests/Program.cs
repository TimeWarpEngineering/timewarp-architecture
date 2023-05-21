namespace EndToEnd.Playwright_.Tests;

class Program
{
  public static async Task Main(string[] args)
  {
    using IPlaywright playwright = await Playwright.CreateAsync();
    await using IBrowser browser = await playwright.Chromium.LaunchAsync
    (
      new BrowserTypeLaunchOptions { Headless = false, SlowMo = 50, }
    );
    IPage page = await browser.NewPageAsync();
    await page.GotoAsync("https://playwright.dev/dotnet");
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = "screenshot.png" });
  }
}
