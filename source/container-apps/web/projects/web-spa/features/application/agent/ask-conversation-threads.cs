#region Purpose
// Hands the Ask panel the transcript for the current conversation generation.
#endregion

#region Design
// Scoped to the WASM app, so the transcript outlives the panel (Close) and TimeWarpPage (navigation
// remounts it). It is keyed by AgentSurfaceState.ConversationGeneration: a new generation (New
// conversation) gets a new, empty thread and the previous one is dropped. Every agent the panel
// builds for the same generation shares one thread.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>One <see cref="AskConversationThread"/> per conversation generation.</summary>
public sealed class AskConversationThreads
{
  private AskConversationThread? Current;
  private int Generation;

  public AskConversationThread For(int generation)
  {
    if (Current is null || Generation != generation)
    {
      Current = new AskConversationThread();
      Generation = generation;
    }

    return Current;
  }
}
