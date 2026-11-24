#region Purpose
// ClearFormSuggestionActionSet: drops a form's draft after the form has applied it.
#endregion

#region Design
// Not a catalog action. FormPrefillScope dispatches it after copying the supplied fields onto the
// working copy. Id is the draft that was applied: a newer draft for the same form that arrived
// meanwhile stays. Sign-out clears every draft through Initialize.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class FormSuggestionState
{
  public static class ClearFormSuggestionActionSet
  {
    [CommandPaletteOptOut("Clears a form draft after the form applies it. The form runs it.")]
    public sealed class Action : IBaseAction
    {
      public string Form { get; }

      public string Id { get; }

      public Action(string form, string id)
      {
        ArgumentException.ThrowIfNullOrWhiteSpace(form);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Form = form;
        Id = id;
      }
    }

    internal sealed class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        if (FormSuggestionState.For(action.Form) is { } current
          && string.Equals(current.Id, action.Id, StringComparison.Ordinal))
        {
          FormSuggestionState.Remove(action.Form);
        }

        return default;
      }
    }
  }
}
