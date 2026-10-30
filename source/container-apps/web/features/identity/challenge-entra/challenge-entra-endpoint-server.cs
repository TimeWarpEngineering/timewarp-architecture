#region Purpose
// Hand-written FastEndpoint that issues Challenge("entra") for link or bootstrap.
#endregion

#region Design
// Generated [ApiEndpoint] handlers always serialize a mediator JSON body; Challenge writes a 302
// (or the fake handler completes the ticket). AllowAnonymous at the HTTP metadata so bootstrap
// works; link authenticates identity-session explicitly and 401s without bouncing to /Login.
// Scheme not registered (Microsoft365 not listed) is 404 so the route does not advertise a
// scheme that is not registered. Runtime policy (IEntraSignInPolicy / site settings) refuses new
// challenges with 403 Sign-in disabled when EntraSignInEnabled is false — existing sessions stay.
// OpenAPI would omit 401 and 403 because AllowAnonymous suppresses them. Summary states the
// anonymous bootstrap choice, and Description lists the handler statuses (400, 401, 403, 404).
// prompt=select_account is on OpenIdConnectOptions; the test fake handler ignores it.
// FastEndpoints auto-sends 204 unless Send.* or MarkResponseStart runs — Challenge/Redirect write
// headers without HasStarted on TestServer, so we mark after ChallengeAsync.
// WithTags("Identity") is the tag the generator gives its siblings (namespace leaf), so Scalar
// groups this operation with them.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using System.Security.Claims;
using TimeWarp.Architecture.Features.Identity.Application;

public sealed class ChallengeEntraEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get(ChallengeEntra.Path.TrimStart('/'));
    AllowAnonymous();
    Summary(summary =>
    {
      summary.Description =
        "Intentionally anonymous. A visitor with no session can start Microsoft 365 bootstrap. "
        + "Link mode returns 401 when identity-session is missing. "
        + "Sign-in disabled returns 403. An unlisted Microsoft 365 method returns 404. "
        + "mode must be link or bootstrap (400). Success is an OIDC redirect.";
      summary.Responses[400] = "mode must be link or bootstrap.";
      summary.Responses[401] = "Link mode without an identity session.";
      summary.Responses[403] = "Entra sign-in is disabled.";
      summary.Responses[404] = "Microsoft 365 is not a listed sign-in method.";
    });
    Description
    (
      builder =>
      {
        builder
          .WithTags("Identity")
          .ProducesProblem(400)
          .Produces(401)
          .ProducesProblem(403)
          .Produces(404);
      }
    );
  }

  public override async Task HandleAsync(CancellationToken cancellationToken)
  {
    SignInOptions signIn = HttpContext.RequestServices
      .GetRequiredService<IOptions<SignInOptions>>().Value;
    if (!signIn.Lists(SignInMethod.Microsoft365))
    {
      await Send.NotFoundAsync(cancellation: cancellationToken);
      return;
    }

    IEntraSignInPolicy policy = HttpContext.RequestServices.GetRequiredService<IEntraSignInPolicy>();
    EntraSignInDecision decision = await policy.EvaluateAsync(
      EntraSignInMode.Challenge,
      tenantId: null,
      cancellationToken);
    if (!decision.Allowed)
    {
      await EntraTicketHttp.WriteProblemAsync(
        HttpContext,
        decision.Problem ?? IdentityProblems.SignInDisabled(),
        cancellationToken);
      return;
    }

    string mode = HttpContext.Request.Query["mode"].ToString();
    bool isLink = string.Equals(mode, EntraTicketProcessor.ModeLink, StringComparison.OrdinalIgnoreCase);
    bool isBootstrap = string.Equals(mode, EntraTicketProcessor.ModeBootstrap, StringComparison.OrdinalIgnoreCase);
    if (!isLink && !isBootstrap)
    {
      await EntraTicketHttp.WriteProblemAsync(HttpContext, IdentityProblems.InvalidEntraMode(), cancellationToken);
      return;
    }

    string returnUrl = LocalReturnUrl.Sanitize(HttpContext.Request.Query["returnUrl"].ToString());
    AuthenticationProperties properties = new()
    {
      RedirectUri = returnUrl
    };
    properties.Items[EntraLinkDefaults.ReturnUrlItemKey] = returnUrl;
    properties.Items[EntraLinkDefaults.ModeItemKey] = isLink
      ? EntraTicketProcessor.ModeLink
      : EntraTicketProcessor.ModeBootstrap;

    if (isLink)
    {
      AuthenticateResult session = await HttpContext.AuthenticateAsync(IdentitySessionDefaults.Scheme);
      string? principalClaim = session.Principal?.FindFirstValue(IdentitySessionDefaults.PrincipalIdClaimType);
      if (!session.Succeeded
        || !Guid.TryParse(principalClaim, out Guid principalGuid)
        || principalGuid == Guid.Empty)
      {
        await Send.UnauthorizedAsync(cancellation: cancellationToken);
        return;
      }

      properties.Items[EntraLinkDefaults.LinkCallerPrincipalIdItemKey] = principalGuid.ToString();
    }

    await HttpContext.ChallengeAsync(EntraLinkDefaults.Scheme, properties);
    HttpContext.MarkResponseStart();
  }
}
