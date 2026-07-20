#region Purpose
// Generates a Passwordless.dev sign-in verification token for a user.
#endregion

#region Design
// Runs server-side because token generation needs the Passwordless API secret, which must never reach the
// browser; the client redeems the returned token through the Passwordless JS sign-in flow.
// SDK exceptions map to a 400 SharedProblemDetails so the OneOf pipeline yields uniform ProblemDetails
// instead of an unhandled 500.
// Task 110 round-1 review (M1): the contract is now [ClientOnlyContract] — no [ApiEndpoint], not
// generated, not reachable over HTTP. This handler is dead/unwired pending full retirement of the
// legacy Passwordless.dev path (104-016/104-021); it mints a token for a caller-supplied UserId
// with no proof of identity, so it must not be re-exposed on the server without first closing that
// hazard.
#endregion

namespace TimeWarp.Architecture.Features.Auth.Application;

using Passwordless;
using Passwordless.Models;
using static TimeWarp.Architecture.Features.Auth.GetSignInToken;

public sealed class GetSignInToken
{
  public class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IPasswordlessClient PasswordlessClient;

    public Handler(IPasswordlessClient passwordlessClient)
    {
      PasswordlessClient = passwordlessClient;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Query request, CancellationToken cancellationToken)
    {
      try
      {
        AuthenticationTokenResponse authTokenResponse =
          await PasswordlessClient.GenerateAuthenticationTokenAsync
          (
            new AuthenticationOptions(request.UserId),
            cancellationToken
          );

        return new Response(authTokenResponse.Token);
      }
      catch (Exception ex)
      {
        return new SharedProblemDetails
        {
          Status = 400,
          Title = "Sign-in token creation failed",
          Detail = ex.Message
        };
      }
    }
  }
}
