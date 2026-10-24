#region Purpose
// Evaluation mode for IEntraSignInPolicy — challenge vs sync-hit vs bootstrap-create vs link.
#endregion

#region Design
// Keep the interface small. Challenge is the only mode that
// returns Sign-in disabled (new challenges only; in-flight tickets and existing sessions stay).
// SyncHit, BootstrapCreate, and Link call IEntraSignInPolicy with the token tid. Restricted mode
// requires that tid in the site-settings accepted list; Any accepts any non-empty tid.
// Authentication:Entra:TenantId is the OIDC authority, not this check. AllowBootstrap gates Create
// only — sync-hit of an active EntraAccount still issues a session. Link uses the same tenant
// acceptance without AllowBootstrap.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

public enum EntraSignInMode
{
  Challenge = 0,
  SyncHit = 1,
  BootstrapCreate = 2,
  Link = 3
}
