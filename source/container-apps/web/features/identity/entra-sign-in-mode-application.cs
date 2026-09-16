#region Purpose
// Evaluation mode for IEntraSignInPolicy — challenge vs sync-hit vs bootstrap-create vs link.
#endregion

#region Design
// Keep the interface small for crunchit 008-004 replacements. Challenge is the only mode that
// returns Sign-in disabled (new challenges only; in-flight tickets and existing sessions stay).
// SyncHit, BootstrapCreate, and Link all require token tid to GUID-equal Authentication:Entra:TenantId
// (task 227). organizations / common authority is not a GUID, so those tickets refuse Untrusted
// tenant. AllowBootstrap gates Create only — sync-hit of an active EntraAccount still issues a
// session. Link is the same tenant pin without AllowBootstrap.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

public enum EntraSignInMode
{
  Challenge = 0,
  SyncHit = 1,
  BootstrapCreate = 2,
  Link = 3
}
