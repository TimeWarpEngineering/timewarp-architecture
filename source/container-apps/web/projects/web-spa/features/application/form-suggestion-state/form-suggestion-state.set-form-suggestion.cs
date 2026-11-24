#region Purpose
// SetFormSuggestionActionSet: stores an agent draft of one edit form. Does not save.
#endregion

#region Design
// CatalogAction so the prefill tool can Execute it. Calling ISender.Send from the agent
// pipeline is TWA0022, including inside a helper. Visibility is Agent and the action takes
// required arguments, so Ctrl-K does not list it; CommandPaletteOptOut keeps it out if those
// ever go away. FormPrefill.SuggestionTypes keeps it off the agent tool list. It carries no
// permission of its own: the Prefill tool checks the submit action's permissions before it runs
// this, and this handler sends nothing to the server. The handler writes the JSON, a new id, and
// clears Accepted for that form only.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class FormSuggestionState
{
  public static class SetFormSuggestionActionSet
  {
    [CatalogAction
    (
      Description = "Store an agent draft of an edit form without saving it.",
      Visibility = ActionVisibility.Agent
    )]
    [CommandPaletteOptOut("Agent draft storage. The Prefill tool runs it; it is not a command.")]
    public sealed class Action : IBaseAction
    {
      public string Form { get; }

      public string SuggestionJson { get; }

      public Action(string form, string suggestionJson)
      {
        ArgumentException.ThrowIfNullOrWhiteSpace(form);
        ArgumentNullException.ThrowIfNull(suggestionJson);
        Form = form;
        SuggestionJson = suggestionJson;
      }
    }

    internal sealed class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        FormSuggestionState.Put
        (
          action.Form,
          new FormSuggestion(action.SuggestionJson, Guid.NewGuid().ToString("N"), Accepted: false)
        );
        return default;
      }
    }
  }
}
