#region Purpose
// Stable credential identity: a non-empty Guid that never recycles and distinguishes one credential from another.
#endregion

#region Design
// Mirrors PrincipalId via [TypedId] so call sites cannot mix principal Guids with credential Guids (RFC D3). Empty is
// rejected by From/JsonConverter — an unassigned id is not a credential. New uses Guid v7 for time-sortable ids.
#endregion

namespace TimeWarp.Identity;

// TypedId attribute namespace: dual-mode MSBuild <Using> in timewarp-identity.csproj (task 115).

[TypedId]
public readonly partial record struct CredentialId;

