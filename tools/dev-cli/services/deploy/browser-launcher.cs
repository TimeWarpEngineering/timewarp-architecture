#region Purpose
// A browser opener candidate: executable plus the arguments before the URL.
#endregion

#region Design
// Chosen by DeployBrowserChoice; started by services/deploy-browser.cs.
#endregion

namespace DevCli.Services;

/// <summary>A browser opener candidate: executable plus the arguments before the URL.</summary>
internal sealed record BrowserLauncher(string Executable, IReadOnlyList<string> Arguments);
