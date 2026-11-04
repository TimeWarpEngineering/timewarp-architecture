#region Purpose
// Gates the `dev open` browser launch behavior.
#endregion

// ReSharper disable InconsistentNaming
namespace DeployOperate_;

public class Browser_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Browser_Given_>();

  public static Task Wsl_Should_PreferWslviewThenTheWindowsBrowser()
  {
    DeployBrowserChoice.BrowserLaunchers(isWindows: false, isMacOs: false, isWsl: true).Select(launcher => launcher.Executable)
      .ShouldBe(["wslview", "explorer.exe", "xdg-open"]);
    return Task.CompletedTask;
  }

  public static Task EachPlatform_Should_HaveAnOpener()
  {
    DeployBrowserChoice.BrowserLaunchers(true, false, false).Single().Executable.ShouldBe("explorer.exe");
    DeployBrowserChoice.BrowserLaunchers(false, true, false).Single().Executable.ShouldBe("open");
    DeployBrowserChoice.BrowserLaunchers(false, false, false).Single().Executable.ShouldBe("xdg-open");
    DeployBrowserChoice.NoBrowserMessage("http://localhost:36371").ShouldBe("No browser opener worked; open http://localhost:36371");
    return Task.CompletedTask;
  }

  public static Task WslDetection_Should_UseTheDistroOrKernel()
  {
    DeployBrowserChoice.IsWsl("Ubuntu", null).ShouldBeTrue();
    DeployBrowserChoice.IsWsl(null, "Linux version 6.18.40.1-microsoft-standard-WSL2").ShouldBeTrue();
    DeployBrowserChoice.IsWsl(null, "Linux version 6.8.0-generic").ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task NoOpener_Should_PrintTheUrlAndNotFail()
  {
    BrowserOpenChoice choice = DeployBrowserChoice.ChooseBrowserOpen(
      noBrowser: false,
      hasTerminal: true,
      DeployBrowserChoice.BrowserLaunchers(isWindows: false, isMacOs: false, isWsl: true),
      _ => false,
      "http://localhost:36371");
    choice.Launchers.ShouldBeEmpty();
    choice.Fallback.ShouldBe(DeployBrowserChoice.NoBrowserMessage("http://localhost:36371"));
    return Task.CompletedTask;
  }

  public static Task NoTerminal_Should_PrintTheUrlAndNotOpen()
  {
    BrowserOpenChoice choice = DeployBrowserChoice.ChooseBrowserOpen(
      noBrowser: false,
      hasTerminal: false,
      DeployBrowserChoice.BrowserLaunchers(isWindows: false, isMacOs: false, isWsl: true),
      _ => true,
      "http://localhost:36371");
    choice.Launchers.ShouldBeEmpty();
    choice.Fallback.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task NoBrowser_Should_OpenNothing()
  {
    BrowserOpenChoice choice = DeployBrowserChoice.ChooseBrowserOpen(
      noBrowser: true,
      hasTerminal: true,
      DeployBrowserChoice.BrowserLaunchers(isWindows: true, isMacOs: false, isWsl: false),
      _ => true,
      "http://localhost:36371");
    choice.Launchers.ShouldBeEmpty();
    choice.Fallback.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Wsl_Should_TryWslviewBeforeExplorerWhenBothAreOnPath()
  {
    BrowserOpenChoice choice = DeployBrowserChoice.ChooseBrowserOpen(
      noBrowser: false,
      hasTerminal: true,
      DeployBrowserChoice.BrowserLaunchers(isWindows: false, isMacOs: false, isWsl: true),
      executable => executable is "wslview" or "explorer.exe",
      "http://localhost:36371");
    choice.Fallback.ShouldBeNull();
    choice.Launchers.Select(launcher => launcher.Executable).ShouldBe(["wslview", "explorer.exe"]);
    DeployBrowserChoice.BrowserLaunchSucceeded("explorer.exe", processSuccess: false, timedOut: false).ShouldBeTrue();
    DeployBrowserChoice.BrowserLaunchSucceeded("explorer.exe", processSuccess: false, timedOut: true).ShouldBeFalse();
    DeployBrowserChoice.BrowserLaunchSucceeded("wslview", processSuccess: false, timedOut: false).ShouldBeFalse();
    return Task.CompletedTask;
  }
}
