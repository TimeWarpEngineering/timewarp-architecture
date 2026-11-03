#region Purpose
// The launchers to try, in order.
#endregion

#region Design
// Returned by DeployBrowserChoice.ChooseBrowserOpen.
#endregion

namespace DevCli.Services;

/// <summary>
/// Launchers to try, in order. An empty list and a null <see cref="BrowserOpenChoice.Fallback"/>
/// means print the URL and start nothing. A fallback line means no launcher was on PATH.
/// </summary>
internal sealed record BrowserOpenChoice(IReadOnlyList<BrowserLauncher> Launchers, string? Fallback);
