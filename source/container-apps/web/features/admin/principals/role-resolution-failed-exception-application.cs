#region Purpose
// Typed failure from IEffectiveRolesResolver when a store it reads cannot be read.
#endregion

#region Design
// Task 160: a role-store outage (connection pool exhaustion, network blip, missing schema) is
// infrastructure, not an authorization verdict. Swallowing it as empty roles would 403 and hide
// the outage, and treating a principal-store failure as a missing row would grant Member to an
// agent. Letting a raw exception escape IClaimsTransformation is also unsafe — PolicyEvaluator
// treats a failed AuthenticateResult as Challenge (401) when the throw is converted to Fail, and
// an untyped 500 is easy to confuse with an app crash. This type is the fail-closed signal:
// RoleResolutionFailureMiddleware maps it to 503. OperationCanceledException is never wrapped.
// Features substrate (same as IEffectiveRolesResolver) so Identity, claims transformation, and
// Admin.Principals share one type without TWA0009.
#endregion

namespace TimeWarp.Architecture.Features;

/// <summary>
/// Thrown when effective product roles cannot be resolved because a store read failed.
/// </summary>
public sealed class RoleResolutionFailedException : Exception
{
  /// <summary>Wraps the store failure that prevented role resolution.</summary>
  public RoleResolutionFailedException(string message, Exception innerException)
    : base(message, innerException)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(message);
    ArgumentNullException.ThrowIfNull(innerException);
  }
}
