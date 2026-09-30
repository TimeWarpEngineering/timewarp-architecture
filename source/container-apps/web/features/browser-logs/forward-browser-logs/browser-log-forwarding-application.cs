#region Purpose
// Dev-only gate and shared constants for browser console log forwarding.
#endregion

#region Design
// Lives in the application layer because two hosts consume it: the forward handler (fail-closed 404)
// and web-server's App.razor (decides whether to emit the forwarder script). One predicate means the
// script and the endpoint can never disagree. Production never loads the script and answers 404.
// The in-proc test host runs as Development, so the endpoint is live under integration tests.
#endregion

namespace TimeWarp.Architecture.Features.BrowserLogs.Application;

using Microsoft.Extensions.Hosting;

public static class BrowserLogForwarding
{
  public const string CategoryName = "Web.Spa.Browser";

  public static bool IsEnabled(IHostEnvironment environment) =>
    environment.IsDevelopment() || environment.IsEnvironment("Testing");
}
