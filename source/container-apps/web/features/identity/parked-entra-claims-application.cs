#region Purpose
// Validated Entra claims held between the OIDC ticket and the bootstrap choose-page decision.
#endregion

#region Design
// Short-lived, single-use. The OIDC ticket already proved tid/oid/iss; this is not a second
// token validation. Destination is the post-choice local path (LocalReturnUrl.Sanitize).
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

public sealed class ParkedEntraClaims
{
  public ParkedEntraClaims(EntraIdTokenClaims claims, string destination)
  {
    Claims = claims;
    Destination = destination;
  }

  public EntraIdTokenClaims Claims { get; }
  public string Destination { get; }
}
