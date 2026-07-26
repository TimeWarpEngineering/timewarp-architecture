#region Purpose
// Stable Profile identity: a non-empty Guid that never recycles and identifies a user's
// personalization settings across sessions.
#endregion

#region Design
// [TypedId] generates the house BCL surface (Value/New/From, STJ JsonConverter as plain Guid string
// fail-closed on empty, IParsable/ISpanParsable, TypeConverter, IComparable) so call sites cannot mix
// Profile Guids with other aggregates' ids and contracts never fail-open to default(ProfileId).
// Empty remains unguardable for default(T) — use IsEmpty at edges.
#endregion

namespace TimeWarp.Architecture.Features.Profiles.Domain;

// TypedId attribute namespace: dual-mode MSBuild <Using> in web-domain.csproj (task 115).

[TypedId]
public readonly partial record struct ProfileId;

