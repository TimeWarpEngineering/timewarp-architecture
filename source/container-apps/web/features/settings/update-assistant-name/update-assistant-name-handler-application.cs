#region Purpose
// Server-side handler for UpdateAssistantName: optimistic concurrency, then rename only.
#endregion

#region Design
// GetAsync through the registered SeedOnReadSiteSettingsStore seeds an empty store, so a rename
// after the row was deleted applies against the freshly seeded row; a null read means the table
// is not migrated yet (Unavailable 503). Compare Command.Version to stored Version, 409 on
// mismatch. On match, RenameAssistant then UpdateAsync. The handler does not touch Entra policy,
// the passkey prompt, accepted tenants, or the xAI key mode. Concurrent Update throws
// ConcurrencyConflictException → the same 409. Does not reference Identity.Application (TWA0009).
#endregion

namespace TimeWarp.Architecture.Features.Settings.Application;

using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Settings.UpdateAssistantName;

public sealed class UpdateAssistantName
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

      settings.RenameAssistant(command.AssistantName);

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
      return new Response(current.AssistantName, current.Version);
    }
  }
}
