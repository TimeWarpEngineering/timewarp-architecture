#region Purpose
// Reloads Ask readiness after an admin sets or clears the site xAI API key.
#endregion

#region Design
// Task 398, review M5. Action handlers never dispatch actions (TWS0002), so the follow-up lives
// here, in the post-pipeline notification shape (AssistantIdentityRefreshHandler): it runs after
// the action's pipeline finishes. A successful set or clear changes whether Ask has a key. An
// open tab would otherwise keep the readiness it loaded at sign-in. A failed save (SaveFailed)
// skips the reload. A declined clear (ClearDeclined) skips it too: the key did not change.
// LoadChatConfiguration's own notification matches neither filter, so there is no loop.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using TimeWarp.Architecture.Features.Admin.Ai;

partial class AgentSurfaceState
{
  [CrossSliceReference(typeof(XaiApiKeyState), "An admin set or clear changes whether Ask has a key, so the shell reloads chat configuration.")]
  internal sealed class XaiApiKeyRefreshHandler(IStore store) : INotificationHandler<PostPipelineNotification>
  {
    public async Task Handle(PostPipelineNotification postPipelineNotification, CancellationToken cancellationToken)
    {
      bool refresh = postPipelineNotification.Request switch
      {
        XaiApiKeyState.SetXaiApiKeyActionSet.Action => !store.GetState<XaiApiKeyState>().SaveFailed,
        XaiApiKeyState.ClearXaiApiKeyActionSet.Action =>
          !store.GetState<XaiApiKeyState>().SaveFailed
          && !store.GetState<XaiApiKeyState>().ClearDeclined,
        _ => false,
      };

      if (refresh)
      {
        await store.GetState<AgentSurfaceState>().LoadChatConfiguration(cancellationToken);
      }
    }
  }
}
