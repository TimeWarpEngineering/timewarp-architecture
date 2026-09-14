#region Purpose
// Registers Entra as a named OpenID Connect scheme under identity-session default; never DefaultScheme.
#endregion

#region Design
// RFC 219 D10: AddOpenIdConnect("entra") with cookieScheme left to the existing identity-session
// cookie. Do not call AddMicrosoftIdentityWebAppAuthentication — that helper steals DefaultScheme.
// OnTicketReceived always HandleResponse(), then EntraTicketHttp (attach/bootstrap/sync-hit +
// SignIn identity-session + redirect). prompt=select_account on both Options.Prompt and the
// redirect-to-IdP event. MapInboundClaims=false so tid/oid/iss keep JWT names.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

public static class EntraAuthenticationRegistration
{
  public static void AddNamedEntraScheme(AuthenticationBuilder authenticationBuilder, IConfiguration configuration)
  {
    ArgumentNullException.ThrowIfNull(authenticationBuilder);
    ArgumentNullException.ThrowIfNull(configuration);

    authenticationBuilder.AddOpenIdConnect
    (
      EntraLinkDefaults.Scheme,
      options =>
      {
        EntraAuthenticationOptions entra = new();
        configuration.GetSection(EntraAuthenticationOptions.SectionKey).Bind(entra);

        string instance = string.IsNullOrWhiteSpace(entra.Instance)
          ? "https://login.microsoftonline.com/"
          : entra.Instance.TrimEnd('/');
        string tenant = string.IsNullOrWhiteSpace(entra.TenantId) ? "organizations" : entra.TenantId.Trim();

        options.Authority = $"{instance}/{tenant}/v2.0";
        options.ClientId = entra.ClientId;
        options.ClientSecret = entra.ClientSecret;
        options.CallbackPath = string.IsNullOrWhiteSpace(entra.CallbackPath) ? "/signin-oidc" : entra.CallbackPath;
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = false;
        options.GetClaimsFromUserInfoEndpoint = false;
        options.MapInboundClaims = false;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Prompt = "select_account";
        options.SignInScheme = IdentitySessionDefaults.Scheme;
        options.Events.OnRedirectToIdentityProvider = context =>
        {
          context.ProtocolMessage.Prompt = "select_account";
          return Task.CompletedTask;
        };
        options.Events.OnTicketReceived = async context =>
        {
          context.HandleResponse();
          await EntraTicketHttp.HandleTicketAsync(
            context.HttpContext,
            context.Principal,
            context.Properties,
            context.HttpContext.RequestAborted);
        };
      }
    );
  }
}
