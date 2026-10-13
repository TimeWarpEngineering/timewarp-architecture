#region Purpose
// Filter: narrow the type list, or search the chosen type's list query, for the typed term.
#endregion

#region Design
// On the type list, a term that is exactly a permitted prefix followed by ':' ("feedback:")
// chooses that type, as typing @feedback: does in the composer. With a type chosen, the search
// waits SearchDelay and is dropped if a newer term arrived meanwhile (SearchGeneration).
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AskReferenceState
{
  public static class FilterActionSet
  {
    public sealed class Action : IBaseAction
    {
      public string Term { get; }

      public Action(string term)
      {
        Term = term ?? "";
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
        if (!AskReferenceState.IsOpen)
        {
          return;
        }

        if (AskReferenceState.SelectedType is null)
        {
          ResourceReferenceTypeRow? exact = ExactPrefix(AskReferenceState.Types, action.Term);
          if (exact is null)
          {
            AskReferenceState.SearchGeneration++;
            AskReferenceState.ShowTypes(action.Term);
            return;
          }

          AskReferenceState.SelectedType = exact;
          AskReferenceState.Items = [];
          int chosen = AskReferenceState.BeginSearch("");
          await SearchAsync(() => AskReferenceState, webServerApiService, chosen, TimeSpan.Zero, cancellationToken);
          return;
        }

        int generation = AskReferenceState.BeginSearch(action.Term);
        await SearchAsync(() => AskReferenceState, webServerApiService, generation, SearchDelay, cancellationToken);
      }

      private static ResourceReferenceTypeRow? ExactPrefix(IReadOnlyList<ResourceReferenceTypeRow> types, string term)
      {
        if (!term.EndsWith(':', StringComparison.Ordinal))
        {
          return null;
        }

        string prefix = term[..^1].Trim();
        foreach (ResourceReferenceTypeRow type in types)
        {
          if (string.Equals(type.Prefix, prefix, StringComparison.OrdinalIgnoreCase))
          {
            return type;
          }
        }

        return null;
      }
    }
  }
}
