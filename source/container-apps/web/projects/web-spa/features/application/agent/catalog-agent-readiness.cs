#region Purpose
// States of the Ask surface: probing, configured, no key, signed out, or the probe failed.
#endregion

#region Design
// Task 293: NotConfigured means only "the server answered and has no key". A 401 is
// Unauthenticated and any other failure is Error, so Ask never tells a signed-out user, or one whose
// request failed, to set a key that is already set.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Whether Ask can call a model.</summary>
public enum CatalogAgentReadiness
{
  /// <summary>The configuration probe has not finished.</summary>
  Unknown = 0,

  /// <summary>The server can call a model.</summary>
  Configured = 1,

  /// <summary>The server answered and has no key. Ask shows the user-secrets command.</summary>
  NotConfigured = 2,

  /// <summary>The server answered 401. Ask asks the user to sign in.</summary>
  Unauthenticated = 3,

  /// <summary>The probe failed for another reason. Ask shows the status and problem detail.</summary>
  Error = 4
}
