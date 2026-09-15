#region Purpose
// Typed singleton identity for the site-settings aggregate — one well-known id, never minted per row.
#endregion

#region Design
// Site settings is a single-row aggregate (task 219-006). A dedicated TypedId keeps it out of
// PrincipalId/CredentialId space so stores and EF conversions cannot mix the singleton with a
// principal. Singleton is a well-known Guid (not New()) so in-memory and EF rows agree without a
// second "current settings" lookup table. Empty remains unguardable for default(T) — use IsEmpty
// at edges; Create always uses Singleton.
#endregion

namespace TimeWarp.Identity;

[TypedId]
public readonly partial record struct SiteSettingsId;
