#region Purpose
// Server-side handler for UpdateXaiByokMode: optimistic concurrency, then the key mode only.
#endregion

#region Design
// GetAsync through the registered SeedOnReadSiteSettingsStore seeds an empty store, so a write
// after the row was deleted applies against the freshly seeded row; a null read means the table
// is not migrated yet (Unavailable 503). Compare Command.Version to stored Version, 409 on
// mismatch. On match, SetXaiByokMode then UpdateAsync. The handler does not touch Entra policy,
// the passkey prompt, accepted tenants, or the assistant name. Concurrent Update throws
// ConcurrencyConflictException → the same 409. Does not reference Identity.Application (TWA0009).
#endregion

namespace TimeWarp.Architecture.Features.Settings.Application;

using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Settings.UpdateXaiByokMode;

public sealed class UpdateXaiByokMode
{
  public sealed class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ISiteSettingsStore SiteSettingsStore;

    public Handler(ISiteSettingsStore siteSettingsStore)
    {
      SiteSettingsStore = siteSettingsStore;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Command command,
      CancellationToken cancellationToken)
    {
      SiteSettings? settings = await SiteSettingsStore.GetAsync(cancellationToken)
        .ConfigureAwait(false);
      if (settings is null)
      {
        return SiteSettingsProblems.Unavailable();
      }

      if (command.Version != settings.Version)
      {
        return SiteSettingsProblems.ConcurrencyConflict();
      }

      settings.SetXaiByokMode(command.XaiByokMode);

      try
      {
        await SiteSettingsStore.UpdateAsync(settings, cancellationToken).ConfigureAwait(false);
      }
      catch (ConcurrencyConflictException)
      {
        return SiteSettingsProblems.ConcurrencyConflict();
      }

      SiteSettings? stored = await SiteSettingsStore.GetAsync(cancellationToken).ConfigureAwait(false);
      SiteSettings current = stored ?? settings;
      return new Response(current.XaiByokMode, current.Version);
    }
  }
}
