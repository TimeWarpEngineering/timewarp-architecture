#region Purpose
// SessionChanged: forget the @ picker and chosen references when the signed-in principal changes.
#endregion

#region Design
// Dispatched by AuthenticationStateListener on every authentication state change, with the
// principal's id (PrincipalIdOf; empty when signed out, since the generated dispatch method takes
// a non-null string). Types, Chosen and the open picker belong to the principal that read them;
// after a sign-out or a sign-in as someone else they would show one user's labels and type list
// to the next. A change notification for the same principal (a
// passkey ceremony, a session re-read) keeps everything, so a conversation's references survive.
// SearchGeneration is bumped so an in-flight search from the old session is dropped.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AskReferenceState
{
  /// <summary>Id claim of the principal the picker state belongs to; empty when signed out.</summary>
  public string SessionPrincipalId { get; private set; } = "";

  /// <summary>
  /// The id SessionChanged keys on: the sub, oid, then NameIdentifier claim, else the identity name;
  /// empty when signed out.
  /// </summary>
  public static string PrincipalIdOf(ClaimsPrincipal user)
  {
    ArgumentNullException.ThrowIfNull(user);
    if (user.Identity?.IsAuthenticated != true)
    {
      return "";
    }

    Claim? id = user.FindFirst("sub") ?? user.FindFirst("oid") ?? user.FindFirst(ClaimTypes.NameIdentifier);
    return id?.Value ?? "name:" + user.Identity.Name;
  }

  public static class SessionChangedActionSet
  {
    public sealed class Action : IBaseAction
    {
      /// <summary>From <see cref="PrincipalIdOf"/>.</summary>
      public string PrincipalId { get; }

      public Action(string principalId)
      {
        PrincipalId = principalId ?? throw new ArgumentNullException(nameof(principalId));
      }
    }

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        if (string.Equals(AskReferenceState.SessionPrincipalId, action.PrincipalId, StringComparison.Ordinal))
        {
          return ValueTask.CompletedTask;
        }

        AskReferenceState.SessionPrincipalId = action.PrincipalId;
        AskReferenceState.IsOpen = false;
        AskReferenceState.Types = [];
        AskReferenceState.Chosen = [];
        AskReferenceState.SearchGeneration++;
        AskReferenceState.ResetPicker();
        return ValueTask.CompletedTask;
      }
    }
  }
}
