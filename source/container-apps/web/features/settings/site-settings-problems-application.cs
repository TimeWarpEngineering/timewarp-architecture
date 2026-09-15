#region Purpose
// SharedProblemDetails factories for the settings slice handlers.
#endregion

#region Design
// Slice-local so other product slices do not take a TWA0009 dependency on Settings.Application.
// Concurrency conflict is 409 on stale Version. Missing row after seed is 500-class only if
// GetOrSeed failed — handlers call the seeder first.
#endregion

namespace TimeWarp.Architecture.Features.Settings.Application;

internal static class SiteSettingsProblems
{
  public static SharedProblemDetails ConcurrencyConflict() => new()
  {
    Title = "Concurrency conflict",
    Status = 409,
    Detail = "Site settings were updated by someone else. Reload and retry."
  };
}
