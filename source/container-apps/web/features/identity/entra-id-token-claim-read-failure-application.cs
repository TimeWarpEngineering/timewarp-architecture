#region Purpose
// First failing check when EntraIdTokenClaims.TryRead cannot read tid, oid, or iss.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

public enum EntraIdTokenClaimReadFailure
{
  None = 0,
  MissingTenantId,
  UnparsableTenantId,
  MissingObjectId,
  UnparsableObjectId,
  MissingIssuer
}
