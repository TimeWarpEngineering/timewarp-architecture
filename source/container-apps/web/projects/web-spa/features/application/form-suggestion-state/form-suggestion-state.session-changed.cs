#region Purpose
// SessionChanged: drop every agent draft when the signed-in principal changes.
#endregion

#region Design
// Dispatched by AuthenticationStateListener with AskReferenceState.PrincipalIdOf on every
// authentication state change, next to the Ask resets. A draft belongs to the person whose agent
// wrote it; FormPrefillScope applies whatever waits for its form, so a draft that outlived a
// sign-out would fill the next person's form on the same circuit. The listener's signed-out path
// reaches here with an empty id. A notification for the same principal keeps the drafts.
// SignOut clears them too, through Initialize, which also forgets the principal.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class FormSuggestionState
{
  public static class SessionChangedActionSet
  {
    [CommandPaletteOptOut("State reset run by authentication changes.")]
    public sealed class Action : IBaseAction
    {
      /// <summary>From <see cref="AskReferenceState.PrincipalIdOf"/>.</summary>
      public string PrincipalId { get; }

      public Action(string principalId)
      {
        PrincipalId = principalId ?? throw new ArgumentNullException(nameof(principalId));
      }
    }

    internal sealed class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        ArgumentNullException.ThrowIfNull(action);
        if (string.Equals(FormSuggestionState.SessionPrincipalId, action.PrincipalId, StringComparison.Ordinal))
        {
          return default;
        }

        FormSuggestionState.SessionPrincipalId = action.PrincipalId;
        FormSuggestionState.Drafts = new(StringComparer.Ordinal);
        return default;
      }
    }
  }
}
