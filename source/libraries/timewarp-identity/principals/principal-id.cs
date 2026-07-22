#region Purpose
// Stable principal identity: a non-empty Guid that never recycles and identifies a subject across credentials and sessions.
#endregion

#region Design
// Hybrid identity: the server mints PrincipalId (Guid v7) at first registration; passkeys/agent keys bind to it later.
// [TypedId] generates the house BCL surface (Value/New/From, STJ JsonConverter as plain Guid string fail-closed on empty,
// IParsable/ISpanParsable, TypeConverter, IComparable) so call sites cannot mix principal Guids with credential Guids
// and contracts never fail-open to default(PrincipalId). Empty remains unguardable for default(T) — use IsEmpty at edges.
#endregion

namespace TimeWarp.Identity;

// TypedId attribute namespace: dual-mode MSBuild <Using> in timewarp-identity.csproj (task 115).

[TypedId]
public readonly partial record struct PrincipalId;

