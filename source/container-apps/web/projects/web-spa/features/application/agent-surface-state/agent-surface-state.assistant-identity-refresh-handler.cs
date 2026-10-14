#region Purpose
// Reloads the assistant's name and avatar after an action that can change or wipe them.
#endregion

#region Design
// Task 361. Action handlers never dispatch actions (TWS0002), so the follow-up lives here, in the
// post-pipeline notification shape (IncrementCountNotificationHandler): it runs after the action's
// pipeline finishes, without coupling to that handler.
// UpdateSiteSettings: a successful save may rename the assistant. The action is agent-visible (the
// in-app model and WebMCP invoke it) as well as the Admin Authentication page's submit, so no page
// is there to sequence the refresh; reloading here means a rename shows in every label at once,
// whoever saved. The avatar is server-made, so it is fetched, not derived from the response.
// ResetStore: Store.Reset reinitializes AgentSurfaceState to the default name with no avatar, and
// nothing else reloads the identity before a full page load.
// A failed save (SaveFailed) skips the reload. LoadAssistantIdentity's own PostPipelineNotification
// matches neither filter, so there is no loop.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using TimeWarp.Architecture.Features.Settings;

partial class AgentSurfaceState
{
  [CrossSliceReference(typeof(SiteSettingsState), "A site-settings save can rename the assistant, so the shell reloads its identity.")]
  internal sealed class AssistantIdentityRefreshHandler(IStore store) : INotificationHandler<PostPipelineNotification>
  {
    public async Task Handle(PostPipelineNotification postPipelineNotification, CancellationToken cancellationToken)
    {
      bool refresh = postPipelineNotification.Request switch
      {
        SiteSettingsState.UpdateSiteSettingsActionSet.Action => !store.GetState<SiteSettingsState>().SaveFailed,
        ApplicationState.ResetStoreActionSet.Action => true,
        _ => false,
      };

      if (refresh)
      {
        await store.GetState<AgentSurfaceState>().LoadAssistantIdentity(cancellationToken);
      }
    }
  }
}
