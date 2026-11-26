#region Purpose
// Browser-facing sign-out: antiforgery-validated form POST that ends the identity session and 303s to /Login.
#endregion

#region Design
// Task 251: the browser posts here (sign-out.ts form submit), so the expired identity-session
// Set-Cookie reaches the browser jar in every render mode and the 303 is a full navigation — the
// next page load re-reads the (now absent) cookie and a Server circuit is torn down with the page.
// Session end reuses EndBrowserSession.Handler via ISender — no second sign-out implementation.
// Antiforgery: validated explicitly with IAntiforgery.IsRequestValidAsync. Blazor's
// UseAntiforgery middleware only validates endpoints carrying antiforgery metadata and only
// records the result, and FastEndpoints' antiforgery integration is not enabled on this host
// (JSON APIs stay cookie-free of it), so this endpoint owns the check. Invalid/missing token →
// 400 with nothing cleared. AllowAnonymous at HTTP metadata: an already-expired session still
// signs out (idempotent, like EndBrowserSession).
// MarkResponseStart after writing the redirect so FastEndpoints does not replace it with 204.
// WithTags("Identity") is the tag the generator gives its siblings (namespace leaf), so Scalar
// groups this operation with them.
// OpenAPI (task 458-007): the operation states the wire contract, not FastEndpoints' default 204:
// 303 with Location, 400, and the antiforgery form field (BrowserNavigationOpenApi's Design region).
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

public sealed class SignOutBrowserSessionEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Post(SignOutBrowserSession.Path.TrimStart('/'));
    AllowAnonymous();
    AllowFormData(urlEncoded: true);
    Summary(summary =>
    {
      summary.Description =
        "Intentionally anonymous. A browser form post that ends the identity session: an expired "
        + "session still signs out. Call GET " + SignOutBrowserSession.AntiforgeryTokenPath
        + " first for the form field and the antiforgery cookie.";
      summary.Responses[303] = "The session is ended (or there was none). Location is "
        + SignOutBrowserSession.RedirectPath + ".";
      summary.Responses[400] = "The antiforgery token is missing or invalid. Nothing is cleared. The body is empty.";
    });
    Description
    (
      builder => builder
        .WithTags("Identity")
        .ClearDefaultProduces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status303SeeOther)
        .Produces(StatusCodes.Status400BadRequest)
        .AddOpenApiOperationTransformer((operation, context, cancellationToken) =>
        {
          _ = cancellationToken;
          BrowserNavigationOpenApi.SetAntiforgeryForm(operation, context);
          BrowserNavigationOpenApi.AddLocation
          (
            operation,
            "303",
            "The sign-in page.",
            SignOutBrowserSession.RedirectPath
          );
          return Task.CompletedTask;
        })
    );
  }

  public override async Task HandleAsync(CancellationToken cancellationToken)
  {
    IAntiforgery antiforgery = HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
    if (!await antiforgery.IsRequestValidAsync(HttpContext))
    {
      HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
      HttpContext.MarkResponseStart();
      return;
    }

    ISender sender = HttpContext.RequestServices.GetRequiredService<ISender>();
    _ = await sender.Send(new EndBrowserSession.Command(), cancellationToken);

    HttpContext.Response.StatusCode = StatusCodes.Status303SeeOther;
    HttpContext.Response.Headers.Location = SignOutBrowserSession.RedirectPath;
    HttpContext.MarkResponseStart();
  }
}
