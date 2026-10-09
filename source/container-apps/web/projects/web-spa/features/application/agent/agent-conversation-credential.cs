#region Purpose
// The short-lived, scoped credential one Ask conversation runs under.
#endregion

#region Design
// This is not a bearer the model holds and not an authentication scheme. It is an advisory
// client-side guardrail, not a security boundary: the browser still executes catalog actions on
// the signed-in store, and server [EndpointAuthorize] is the boundary. The credential records whose
// principal minted the conversation, which permission ids it may spend (the panel copies every
// permission claim the principal holds when it mints one), and when it stops being valid. A null
// credential is the session-only path and means unbounded.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>One conversation's principal, permission scope, and expiry.</summary>
public sealed record AgentConversationCredential(
  Guid Id,
  Guid PrincipalId,
  IReadOnlyList<string> Scopes,
  string DisplayName,
  DateTimeOffset ExpiresAt);
