#region Purpose
// Stores the credential minted for the current Ask conversation.
#endregion

#region Design
// The scope array is replaced, not mutated, so a later issuer call cannot change a credential
// the authority is reading. Pieces stay separate fields so the state clone does not have to
// round-trip the record.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AgentSurfaceState
{
  public static class SetConversationActionSet
  {
    public sealed class Action : IBaseAction
    {
      public Guid Id { get; }

      public Guid PrincipalId { get; }

      public string[] Scopes { get; }

      public DateTimeOffset ExpiresAt { get; }

      public string DisplayName { get; }

      public Action(Guid id, Guid principalId, string[] scopes, DateTimeOffset expiresAt, string displayName)
      {
        Id = id;
        PrincipalId = principalId;
        Scopes = scopes;
        ExpiresAt = expiresAt;
        DisplayName = displayName;
      }
    }

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        AgentSurfaceState.ConversationId = action.Id;
        AgentSurfaceState.ConversationPrincipalId = action.PrincipalId;
        AgentSurfaceState.ConversationScopes = action.Scopes is null ? [] : [.. action.Scopes];
        AgentSurfaceState.ConversationExpiresAt = action.ExpiresAt;
        AgentSurfaceState.ConversationDisplayName = action.DisplayName;
        return default;
      }
    }
  }
}
