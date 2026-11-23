#region Purpose
// Starts a new Ask conversation: ask-before-editing, no credential, new generation.
#endregion

#region Design
// The generation bump tells the panel to mint a new credential and rebuild on a new, empty
// AskConversationThread; AskConversationThreads drops the previous transcript.
// Privacy dismissal is a shell preference and is left as the person set it. A live microphone is
// stopped. Idle does not import ask-ai.js.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AgentSurfaceState
{
  public static class NewConversationActionSet
  {
    public sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store,
      IJSRuntime jsRuntime
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        await AgentSurfaceState.ReleaseVoiceCaptureAsync(jsRuntime);
        AgentSurfaceState.EditMode = AgentEditMode.AskBeforeEditing;
        AgentSurfaceState.ConversationId = null;
        AgentSurfaceState.ConversationPrincipalId = null;
        AgentSurfaceState.ConversationScopes = [];
        AgentSurfaceState.ConversationExpiresAt = null;
        AgentSurfaceState.ConversationDisplayName = null;
        AgentSurfaceState.ConversationGeneration++;
      }
    }
  }
}
