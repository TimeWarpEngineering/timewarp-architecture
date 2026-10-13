#region Purpose
// Choose: record a referenced resource for the current conversation and close the picker.
#endregion

#region Design
// The reference is kept with AgentSurfaceState.ConversationGeneration, so New conversation starts
// with none. The same token chosen twice is kept once, at the end. At most ChosenCap are kept, the
// oldest dropped first. page_context publishes them so the agent sees each token's label, type,
// and typed id. The component inserts the token text at the caret before dispatching this.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AskReferenceState
{
  public static class ChooseActionSet
  {
    public sealed class Action : IBaseAction
    {
      public ResourceReferenceItem Item { get; }

      public Action(ResourceReferenceItem item)
      {
        Item = item ?? throw new ArgumentNullException(nameof(item));
      }
    }

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        int generation = AgentSurfaceState.ConversationGeneration;
        List<ResourceReferenceItem> chosen = AskReferenceState.ChosenGeneration == generation
          ? [.. AskReferenceState.Chosen]
          : [];
        chosen.RemoveAll(item => item.Token == action.Item.Token);
        chosen.Add(action.Item);
        if (chosen.Count > ChosenCap)
        {
          chosen.RemoveRange(0, chosen.Count - ChosenCap);
        }

        AskReferenceState.Chosen = chosen;
        AskReferenceState.ChosenGeneration = generation;
        AskReferenceState.IsOpen = false;
        AskReferenceState.SearchGeneration++;
        AskReferenceState.ResetPicker();
        return ValueTask.CompletedTask;
      }
    }
  }
}
