#region Purpose
// Which browser launchers `dev open` and `dev deploy` try, and whether a launch counts as
// success. The process that starts the launcher is services/deploy-browser.cs.
#endregion

#region Design
// Browser (task 396): ChooseBrowserOpen is the decision `dev open` and `dev deploy` share.
// --no-browser, no interactive terminal, or no launcher on PATH prints the URL and is not a failure.
// NoBrowserMessage is the no-launcher and every-launcher-failed line. It does not name --no-browser.
// The process that starts the launcher is services/deploy-browser.cs.
#endregion

namespace DevCli.Services;

/// <summary>Which browser launcher <c>dev open</c> and <c>dev deploy</c> try, and whether a launch counts as success.</summary>
internal static class DeployBrowserChoice
{
  /// <summary>
  /// Browser openers to try in order: Windows <c>explorer.exe</c>; macOS <c>open</c>; WSL <c>wslview</c> then
  /// <c>explorer.exe</c> (the Windows browser), then <c>xdg-open</c>; other Linux <c>xdg-open</c>. Those on PATH are
  /// tried until one succeeds; none on PATH means print the URL.
  /// </summary>
  internal static BrowserLauncher[] BrowserLaunchers(bool isWindows, bool isMacOs, bool isWsl) =>
    isWindows ? [new BrowserLauncher("explorer.exe", [])]
    : isMacOs ? [new BrowserLauncher("open", [])]
    : isWsl ? [new BrowserLauncher("wslview", []), new BrowserLauncher("explorer.exe", []), new BrowserLauncher("xdg-open", [])]
    : [new BrowserLauncher("xdg-open", [])];

  /// <summary>WSL when WSL_DISTRO_NAME is set or the kernel version names Microsoft.</summary>
  internal static bool IsWsl(string? distroName, string? kernelVersion) =>
    !string.IsNullOrWhiteSpace(distroName)
    || (kernelVersion?.Contains("microsoft", StringComparison.OrdinalIgnoreCase) ?? false);

  internal static string NoBrowserMessage(string url) => $"No browser opener worked; open {url}";

  /// <summary>
  /// Which launchers to start. <paramref name="noBrowser"/> and a missing terminal print the URL only.
  /// No launcher on PATH returns <see cref="NoBrowserMessage"/>. Neither case is a failure.
  /// </summary>
  internal static BrowserOpenChoice ChooseBrowserOpen(
    bool noBrowser,
    bool hasTerminal,
    IReadOnlyList<BrowserLauncher> candidates,
    Func<string, bool> isOnPath,
    string url)
  {
    if (noBrowser || !hasTerminal)
    {
      return new([], null);
    }

    BrowserLauncher[] onPath = [.. candidates.Where(launcher => isOnPath(launcher.Executable))];
    return onPath.Length == 0
      ? new([], NoBrowserMessage(url))
      : new(onPath, null);
  }

  /// <summary>
  /// <c>explorer.exe</c> exits 1 after it opens a URL. A timeout is still a miss.
  /// Any other launcher must exit 0.
  /// </summary>
  internal static bool BrowserLaunchSucceeded(string executable, bool processSuccess, bool timedOut) =>
    processSuccess || (executable == "explorer.exe" && !timedOut);
}
