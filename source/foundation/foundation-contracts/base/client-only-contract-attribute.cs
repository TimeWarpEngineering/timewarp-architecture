#region Purpose
// Marks a routed contract as deliberately having no server endpoint (mock-mode/demo only).
#endregion

#region Design
// The endpoint-coverage analyzer (TWA0006) flags every [ApiRoute] contract that no endpoint in
// the server compilation serves — this attribute is the explicit, reasoned opt-out, so "not
// implemented yet" is a visible decision instead of silent drift (the 405 bug class).
// Reason is required: an unexplained opt-out is just the drift with paperwork.
#endregion

namespace TimeWarp.Foundation.Features;

/// <summary>
/// Declares that this routed contract intentionally has no server endpoint
/// (e.g. served only by the SPA's mock mode). Suppresses TWA0006 for the contract.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ClientOnlyContractAttribute : Attribute
{
  public string Reason { get; }

  public ClientOnlyContractAttribute(string reason)
  {
    Reason = reason;
  }
}
