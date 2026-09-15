#region Purpose
// SharedProblemDetails factories for the settings slice handlers.
#endregion

#region Design
// Slice-local so other product slices do not take a TWA0009 dependency on Settings.Application.
// NotInitialized is 503 when the singleton row is missing — only SiteSettingsSeeder inserts it.
// Concurrency conflict is 409 on stale Version.
#endregion

namespace TimeWarp.Architecture.Features.Settings.Application;

internal static class SiteSettingsProblems
{
  public static SharedProblemDetails NotInitialized() => new()
  {
    Title = "Site settings not initialized",
    Status = 503,
    Detail = "Site settings have not been seeded yet. Retry after the host finishes starting."
  };

  public static SharedProblemDetails ConcurrencyConflict() => new()
  {
    Title = "Concurrency conflict",
    Status = 409,
    Detail = "Site settings were updated by someone else. Reload and retry."
  };
}
