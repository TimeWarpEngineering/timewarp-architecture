#region Purpose
// Opens an ingress URL with the same launcher `dev open` and `dev deploy` share.
#endregion

#region Design
// The process half of DeployOperate.ChooseBrowserOpen. Both verbs call OpenAsync so the
// opener is not copied: WSL tries wslview, then explorer.exe, then xdg-open; the first
// success wins. explorer.exe exits 1 after it opens a URL, which still counts.
// --no-browser, a non-interactive terminal, or no launcher on PATH prints the URL and
// returns. None of those is a failure, and none starts a process.
#endregion

namespace DevCli.Services;

/// <summary>Starts a browser for an ingress URL, or prints the URL when nothing can.</summary>
internal static class DeployBrowser
{
  /// <summary>
  /// Prints <paramref name="url"/>, then opens it unless <paramref name="noBrowser"/> is set,
  /// the terminal is not interactive, or no launcher is on PATH.
  /// </summary>
  internal static async Task OpenAsync(ITerminal terminal, string url, bool noBrowser, CancellationToken cancellationToken)
  {
    terminal.WriteLine($"App: {url}".Green());

    bool hasTerminal = terminal.IsInteractive;
    BrowserLauncher[] candidates = [];
    if (!noBrowser && hasTerminal)
    {
      string? kernel = File.Exists("/proc/version")
        ? await File.ReadAllTextAsync("/proc/version", cancellationToken)
        : null;
      bool wsl = DeployOperate.IsWsl(Environment.GetEnvironmentVariable("WSL_DISTRO_NAME"), kernel);
      candidates = DeployOperate.BrowserLaunchers(OperatingSystem.IsWindows(), OperatingSystem.IsMacOS(), wsl);
    }

    BrowserOpenChoice choice = DeployOperate.ChooseBrowserOpen(
      noBrowser,
      hasTerminal,
      candidates,
      executable => PathResolver.ResolveExecutable(executable) is not null,
      url);
    if (choice.Fallback is not null)
    {
      terminal.WriteLine(choice.Fallback.Yellow());
      return;
    }

    foreach (BrowserLauncher launcher in choice.Launchers)
    {
      CommandOutput opened = await Shell.Builder(launcher.Executable)
        .WithArguments([.. launcher.Arguments, url])
        .WithTimeout(TimeSpan.FromSeconds(15))
        .WithNoValidation()
        .CaptureAsync(cancellationToken);

      if (DeployOperate.BrowserLaunchSucceeded(launcher.Executable, opened.Success, opened.TimedOut))
      {
        return;
      }
    }

    if (choice.Launchers.Count > 0)
    {
      terminal.WriteLine(DeployOperate.NoBrowserMessage(url).Yellow());
    }
  }
}
