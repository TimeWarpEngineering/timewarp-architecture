#region Purpose
// Evaluation mode for IEntraSignInPolicy — challenge vs sync-hit vs bootstrap-create.
#endregion

#region Design
// Keep the interface small for crunchit 008-004 replacements. Challenge is the only mode that
// returns Sign-in disabled (new challenges only; in-flight tickets and existing sessions stay).
// SyncHit and BootstrapCreate both require a trusted tenant (empty list refuses). AllowBootstrap
// gates Create only — sync-hit of an active EntraAccount still issues a session.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

public enum EntraSignInMode
{
  Challenge = 0,
  SyncHit = 1,
  BootstrapCreate = 2
}
