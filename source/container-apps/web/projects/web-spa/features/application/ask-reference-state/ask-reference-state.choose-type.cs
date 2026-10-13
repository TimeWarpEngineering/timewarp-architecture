#region Purpose
// ChooseType: pick a resource type and list its items (first search, no debounce).
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AskReferenceState
{
  public static class ChooseTypeActionSet
  {
    public sealed class Action : IBaseAction
    {
      public string Prefix { get; }

      public Action(string prefix)
      {
        Prefix = prefix;
      }
    }

    internal sealed class Handler
    (
      IStore store,
      IWebServerApiService webServerApiService
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        ResourceReferenceTypeRow? row = null;
        foreach (ResourceReferenceTypeRow type in AskReferenceState.Types)
        {
          if (string.Equals(type.Prefix, action.Prefix, StringComparison.Ordinal))
          {
            row = type;
          }
        }

        // Only a type the user was offered can be searched.
        if (!AskReferenceState.IsOpen || row is null)
        {
          return;
        }

        AskReferenceState.SelectedType = row;
        AskReferenceState.Items = [];
        AskReferenceState.HasMore = false;
        AskReferenceState.HighlightedIndex = -1;
        int generation = AskReferenceState.BeginSearch("");
        await SearchAsync(() => AskReferenceState, webServerApiService, generation, TimeSpan.Zero, cancellationToken);
      }
    }
  }
}
