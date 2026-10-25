#region Purpose
// Reloads Ask readiness after an admin or a user sets or clears an xAI API key.
#endregion

#region Design
// Task 398, review M5. Action handlers never dispatch actions (TWS0002), so the follow-up lives
// here, in the post-pipeline notification shape (AssistantIdentityRefreshHandler): it runs after
// the action's pipeline finishes. A successful set or clear changes whether Ask has a key. An
// open tab would otherwise keep the readiness it loaded at sign-in. The same reload follows a
// Profile set or clear of the personal key. A failed save (SaveFailed)
// skips the reload. A declined clear (ClearDeclined) skips it too: the key did not change.
// LoadChatConfiguration's own notification matches neither filter, so there is no loop.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using TimeWarp.Architecture.Features.Admin.Ai;
using TimeWarp.Architecture.Features.Profiles;

partial class AgentSurfaceState
{
  [CrossSliceReference(typeof(XaiApiKeyState), "An admin set or clear changes whether Ask has a key, so the shell reloads chat configuration.")]
  [CrossSliceReference(typeof(UserXaiApiKeyState), "A user set or clear changes whether Ask has a key, so the shell reloads chat configuration.")]
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
        UserXaiApiKeyState.SetUserXaiApiKeyActionSet.Action => !store.GetState<UserXaiApiKeyState>().SaveFailed,
        UserXaiApiKeyState.ClearUserXaiApiKeyActionSet.Action =>
          !store.GetState<UserXaiApiKeyState>().SaveFailed
          && !store.GetState<UserXaiApiKeyState>().ClearDeclined,
        _ => false,
      };

      if (refresh)
      {
        await store.GetState<AgentSurfaceState>().LoadChatConfiguration(cancellationToken);
      }
    }
  }
}
