#region Purpose
// Explicit, reasoned opt-out from authentication for a generated FastEndpoint — the anonymous
// counterpart to [EndpointAuthorize].
#endregion

#region Design
// Task 110: the generator's fail-open default (no marker -> AllowAnonymous()) let an [ApiEndpoint]
// contract's real auth intent go unstated — including contracts that themselves declare
// IAuthApiRequest/[AuthApiRequest]. The fix flips the default to fail-closed (no marker -> emit
// NOTHING, so FastEndpoints requires authentication by default) and makes anonymous a STATED
// choice: every [ApiEndpoint] contract now carries exactly one of [EndpointAuthorize] or this
// attribute (TWA0013 enforces the pairing; TWA0014 flags both-present or a contradiction with
// IAuthApiRequest).
// Reason is a REQUIRED ctor arg, mirroring ClientOnlyContractAttribute's rationale verbatim: an
// unexplained opt-out is just the fail-open drift with paperwork. Write the actual reason a human
// would need to trust the decision (pre-auth ceremony, public demo data, ambient-session read, …)
// — not a placeholder.
// Lives in TimeWarp.Architecture.Attributes (not timewarp-architecture-analyzers) for the same
// reason EndpointAuthorizeAttribute does: contract assemblies reference this package without a
// Roslyn dependency, and the FastEndpoint generator matches it by simple name across any
// consumer's root namespace.
#endregion

namespace TimeWarp.Architecture.Attributes;

/// <summary>
/// Declares that this [ApiEndpoint] contract is deliberately anonymous — the generator emits
/// AllowAnonymous() for it. Mutually exclusive with [EndpointAuthorize] in intent (TWA0014 flags
/// both present on the same contract as a conflict); [EndpointAuthorize] wins at generation if both
/// somehow appear.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class EndpointAllowAnonymousAttribute : Attribute
{
  public string Reason { get; }

  public EndpointAllowAnonymousAttribute(string reason)
  {
    Reason = reason;
  }
}
