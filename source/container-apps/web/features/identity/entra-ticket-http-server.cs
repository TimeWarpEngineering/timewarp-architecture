#region Purpose
// HTTP adapter for Entra ticket completion: problem-details or identity-session cookie + redirect.
#endregion

#region Design
// Shared by OpenIdConnect OnTicketReceived and the test fake handler so both paths HandleResponse
// (never let OIDC sign in as the ambient user), then IssueAsync(identity-session) and redirect.
// Problem responses use the contract-seam serializer so SPA/tests see the same camelCase shape.
// MarkResponseStart after Redirect/WriteAsJson so FastEndpoints does not replace the status with 204.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture.Abstractions;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.Identity.Application;
using TimeWarp.Foundation.Types;

public static class EntraTicketHttp
{
  public static async Task HandleTicketAsync
  (
    HttpContext httpContext,
    ClaimsPrincipal? entraPrincipal,
    AuthenticationProperties? properties,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(httpContext);

    EntraTicketProcessor processor = httpContext.RequestServices.GetRequiredService<EntraTicketProcessor>();
    IBrowserSessionService sessions = httpContext.RequestServices.GetRequiredService<IBrowserSessionService>();

    AuthenticationProperties ticketProperties = properties ?? new AuthenticationProperties();
    string mode = ticketProperties.Items.TryGetValue(EntraLinkDefaults.ModeItemKey, out string? storedMode)
      ? storedMode ?? ""
      : EntraTicketProcessor.ModeBootstrap;
    ticketProperties.Items.TryGetValue(EntraLinkDefaults.LinkCallerPrincipalIdItemKey, out string? callerText);
    PrincipalId? caller = Guid.TryParse(callerText, out Guid callerGuid) && callerGuid != Guid.Empty
      ? PrincipalId.From(callerGuid)
      : null;
    string returnUrl = LocalReturnUrl.Sanitize(ticketProperties.RedirectUri);

    if (entraPrincipal is null || !EntraIdTokenClaims.TryRead(entraPrincipal, out EntraIdTokenClaims claims))
    {
      await WriteProblemAsync(httpContext, IdentityProblems.InvalidEntraToken(), cancellationToken);
      return;
    }

    OneOf<PrincipalId, SharedProblemDetails> result = await processor.ProcessAsync(
      claims,
      mode,
      caller,
      cancellationToken);
    if (result.IsT1)
    {
      await WriteProblemAsync(httpContext, result.AsT1, cancellationToken);
      return;
    }

    await sessions.IssueAsync(result.AsT0, claims.DisplayName, cancellationToken);
    httpContext.Response.Redirect(returnUrl);
    httpContext.MarkResponseStart();
  }

  public static async Task WriteProblemAsync
  (
    HttpContext httpContext,
    SharedProblemDetails problem,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(httpContext);
    ArgumentNullException.ThrowIfNull(problem);

    httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status400BadRequest;
    httpContext.Response.ContentType = "application/problem+json";
    await httpContext.Response.WriteAsJsonAsync(problem, ContractSerializationDefaults.Options, cancellationToken);
    httpContext.MarkResponseStart();
  }
}
