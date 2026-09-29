#region Purpose
// SharedProblemDetails factories for the settings slice handlers.
#endregion

#region Design
// Slice-local so other product slices do not take a TWA0009 dependency on Settings.Application.
// Unavailable is 503 when the store read returns null — since task 254 the read seeds an empty
// store, so null only means the schema is not migrated yet (Postgres 42P01; see
// SeedOnReadSiteSettingsStore), a transient state a retry clears, never a 500.
// Concurrency conflict is 409 on stale Version.
#endregion

namespace TimeWarp.Architecture.Features.Settings.Application;

internal static class SiteSettingsProblems
{
  public static SharedProblemDetails Unavailable() => new()
  {
    Title = "Site settings unavailable",
    Status = 503,
    Detail = "The site settings table is not migrated yet. Retry after database migrations finish."
  };

  public static SharedProblemDetails ConcurrencyConflict() => new()
  {
    Title = "Concurrency conflict",
    Status = 409,
    Detail = "Site settings were updated by someone else. Reload and retry."
  };
}
