#region Purpose
// ConfirmFormSuggestionActionSet: asks before a draft replaces unsaved edits on one form.
#endregion

#region Design
// The dialog stays in the handler (TWA0026). FormPrefillScope dispatches this only when its
// working copy is dirty. The message says the draft replaces unsaved changes on the form: the
// copy may differ because of an earlier draft, not only because the person typed. Noun names
// the form ("profile", "role"). Decline drops the draft when its id is still the one that was
// asked about. Accept sets Accepted for that same id. A newer draft that arrives while the
// dialog is open is left as it is: not dropped, not accepted.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using TimeWarp.Architecture.Services;

partial class FormSuggestionState
{
  public static class ConfirmFormSuggestionActionSet
  {
    [CommandPaletteOptOut("Asks before a prefill replaces unsaved form edits. The form runs it.")]
    public sealed class Action : IBaseAction
    {
      public string Form { get; }

      public string Noun { get; }

      public Action(string form, string noun)
      {
        ArgumentException.ThrowIfNullOrWhiteSpace(form);
        ArgumentException.ThrowIfNullOrWhiteSpace(noun);
        Form = form;
        Noun = noun;
      }
    }

    internal sealed class Handler(IStore store, IConfirmationService confirmation) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        ArgumentNullException.ThrowIfNull(action);
        FormSuggestion? asked = FormSuggestionState.For(action.Form);
        if (asked is null)
        {
          return;
        }

        bool accepted = await confirmation.ConfirmAsync
        (
          "Replace unsaved edits?",
          "An agent drafted new " + action.Noun + " values. Applying them replaces unsaved changes on this form.",
          "Apply draft",
          "Keep mine",
          cancellationToken
        );

        FormSuggestion? current = FormSuggestionState.For(action.Form);
        if (current is null || !string.Equals(current.Id, asked.Id, StringComparison.Ordinal))
        {
          return;
        }

        if (!accepted)
        {
          FormSuggestionState.Remove(action.Form);
          return;
        }

        FormSuggestionState.Put(action.Form, current with { Accepted = true });
      }
    }
  }
}
