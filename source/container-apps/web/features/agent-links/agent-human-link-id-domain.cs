#region Purpose
// Stable identity for an optional Agent ↔ Human link (never required for paid service).
#endregion

#region Design
// [TypedId] generates the house BCL surface so call sites cannot mix this Guid with ProfileId
// or PrincipalId. Empty remains unguardable for default(T) — use IsEmpty at edges.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks.Domain;

[TypedId]
public readonly partial record struct AgentHumanLinkId;
