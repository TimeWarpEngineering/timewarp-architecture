#region Purpose
// One pipeline check that refuses passkey ceremonies when Passkey is not an enabled sign-in method.
#endregion

#region Design
// Registered at order 400, outside FluentValidationBehavior (500), so the refusal runs before
// validation and before the handler. Start endpoints therefore create no WebAuthn ceremony state.
// The passkey ceremony commands share this check, including CompleteEntraBootstrapExisting, which
// verifies a passkey assertion and signs that principal in. add-passkey and agent-key or agent-token
// commands are not browser sign-in and are not in the set. The problem type is stable so clients
// can branch on it. Rows for existing passkeys stay; listing Passkey again lets those ceremonies run.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using System.Reflection;
using IdentityContracts = TimeWarp.Architecture.Features.Identity;

/// <summary>Returns 403 for passkey ceremonies when Passkey is not listed.</summary>
public sealed class PasskeySignInMethodGateBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
  where TRequest : notnull
{
  private static readonly HashSet<Type> PasskeyCommands =
  [
    typeof(IdentityContracts.StartPasskeyAuthentication.Command),
    typeof(IdentityContracts.CompletePasskeyAuthentication.Command),
    typeof(IdentityContracts.StartPasskeyRegistration.Command),
    typeof(IdentityContracts.CompletePasskeyRegistration.Command),
    typeof(IdentityContracts.StartAddExistingPasskey.Command),
    typeof(IdentityContracts.CompleteAddExistingPasskey.Command),
    typeof(IdentityContracts.CompleteEntraBootstrapExisting.Command)
  ];

  private readonly IOptions<SignInOptions> SignInOptions;

  public PasskeySignInMethodGateBehavior(IOptions<SignInOptions> signInOptions)
  {
    SignInOptions = signInOptions;
  }

  public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
  {
    if (!PasskeyCommands.Contains(typeof(TRequest)) || SignInOptions.Value.Lists(IdentityContracts.SignInMethod.Passkey))
    {
      return await next().ConfigureAwait(false);
    }

    _ = request;
    _ = cancellationToken;
    return Problem(IdentityProblems.SignInMethodDisabled());
  }

  private static TResponse Problem(SharedProblemDetails problemDetails)
  {
    Type successType = typeof(TResponse).GetGenericArguments()[0];
    Type oneOfType = typeof(OneOf<,>).MakeGenericType(successType, typeof(SharedProblemDetails));
    MethodInfo fromProblem = oneOfType.GetMethod("FromT1")
      ?? throw new InvalidOperationException("Passkey ceremony responses must be OneOf<T, SharedProblemDetails>.");
    return (TResponse)fromProblem.Invoke(null, [problemDetails])!;
  }
}
