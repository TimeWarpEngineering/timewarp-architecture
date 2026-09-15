#region Purpose
// Allow or refuse-with-problem result of IEntraSignInPolicy.EvaluateAsync.
#endregion

#region Design
// Not OneOf: products replacing the policy should not take a mediator dependency for this seam.
// Allowed=true implies Problem is null; Refuse requires a problem (Sign-in disabled, Bootstrap
// not allowed, Untrusted tenant). Callers branch on Allowed then write Problem.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using TimeWarp.Foundation.Types;

public sealed class EntraSignInDecision
{
  private EntraSignInDecision(bool allowed, SharedProblemDetails? problem)
  {
    Allowed = allowed;
    Problem = problem;
  }

  public bool Allowed { get; }
  public SharedProblemDetails? Problem { get; }

  public static EntraSignInDecision Allow() => new(true, null);

  public static EntraSignInDecision Refuse(SharedProblemDetails problem)
  {
    ArgumentNullException.ThrowIfNull(problem);
    return new(false, problem);
  }
}
