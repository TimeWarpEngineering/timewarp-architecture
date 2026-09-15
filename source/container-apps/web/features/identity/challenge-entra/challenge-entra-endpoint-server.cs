#region Purpose
// Hand-written FastEndpoint that issues Challenge("entra") for link or bootstrap.
#endregion

#region Design
// Generated [ApiEndpoint] handlers always serialize a mediator JSON body; Challenge writes a 302
// (or the fake handler completes the ticket). AllowAnonymous at the HTTP metadata so bootstrap
// works; link authenticates identity-session explicitly and 401s without bouncing to /Login.
// Scheme not registered (configuration Enabled false) is 404 so the route does not advertise a
// scheme that is not registered. Runtime policy (IEntraSignInPolicy / site settings) refuses new
// challenges with 403 Sign-in disabled when EntraSignInEnabled is false — existing sessions stay.
// prompt=select_account is on OpenIdConnectOptions; the test fake handler ignores it.
// FastEndpoints auto-sends 204 unless Send.* or MarkResponseStart runs — Challenge/Redirect write
// headers without HasStarted on TestServer, so we mark after ChallengeAsync.
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
  }

  public override async Task HandleAsync(CancellationToken cancellationToken)
  {
    EntraAuthenticationOptions entra = HttpContext.RequestServices
      .GetRequiredService<IOptions<EntraAuthenticationOptions>>().Value;
    if (!entra.Enabled)
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

    AuthenticationProperties properties = new()
    {
      RedirectUri = LocalReturnUrl.Sanitize(HttpContext.Request.Query["returnUrl"].ToString())
    };
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
