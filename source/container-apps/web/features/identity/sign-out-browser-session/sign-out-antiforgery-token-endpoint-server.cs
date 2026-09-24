#region Purpose
// Issues the antiforgery cookie + request token the browser needs to POST the sign-out form.
#endregion

#region Design
// Task 251: called by sign-out.ts with a same-origin browser fetch, so the antiforgery cookie
// lands in the browser jar and the token is bound to the browser's current identity-session user
// (the same user the form POST will present). Side-effect free apart from the antiforgery cookie;
// GetAndStoreTokens also sets Cache-Control no-cache and X-Frame-Options SAMEORIGIN. Anonymous:
// a signed-out browser gets an anonymous token and the POST stays an idempotent no-op.
// JSON via ContractSerializationDefaults (camelCase seam options); MarkResponseStart so
// FastEndpoints does not append its own response.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

public sealed class SignOutAntiforgeryTokenEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get(SignOutBrowserSession.AntiforgeryTokenPath.TrimStart('/'));
    AllowAnonymous();
  }

  public override async Task HandleAsync(CancellationToken cancellationToken)
  {
    IAntiforgery antiforgery = HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
    AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(HttpContext);

    HttpContext.Response.StatusCode = StatusCodes.Status200OK;
    await HttpContext.Response.WriteAsJsonAsync
    (
      new SignOutBrowserSession.AntiforgeryToken(tokens.FormFieldName, tokens.RequestToken!),
      ContractSerializationDefaults.Options,
      cancellationToken
    );
    HttpContext.MarkResponseStart();
  }
}
