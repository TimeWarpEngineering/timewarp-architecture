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
