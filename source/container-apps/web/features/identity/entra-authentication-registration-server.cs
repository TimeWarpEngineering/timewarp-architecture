#region Purpose
// Registers Entra as a named OpenID Connect scheme under identity-session default; never DefaultScheme.
#endregion

#region Design
// RFC 219 D10: AddOpenIdConnect("entra") with cookieScheme left to the existing identity-session
// cookie. Do not call AddMicrosoftIdentityWebAppAuthentication — that helper steals DefaultScheme.
// OnTicketReceived always HandleResponse(), then EntraTicketHttp (attach/bootstrap/sync-hit +
// SignIn identity-session + redirect). prompt=select_account on both Options.Prompt and the
// redirect-to-IdP event. MapInboundClaims=false so tid/oid/iss keep JWT names.
// IssuerValidator is always EntraIssuerValidator (organizations/common/consumers metadata issuer is
// a {tenantid} placeholder; single-tenant GUID authorities use the same tid pin). ValidateIssuer
// stays true — do not disable it.
// YARP and ACA terminate TLS and forward to Web.Server over http without UseForwardedHeaders
// (104-031). OpenIdConnectHandler would then emit an http redirect_uri. Pin CorrelationCookie
// and NonceCookie SecurePolicy.Always so SameSite=None cookies stay Secure behind http (do not
// rely on framework defaults). PublicOrigin (when set) overrides ProtocolMessage.RedirectUri on
// challenge and on authorization-code redemption so Entra sees the browser origin.
// AuthenticationProperties.RedirectUri stays the local return path — LocalReturnUrl.Sanitize, not
// PublicOrigin.
// EntraSchemeRegistrationLogHostedService logs informational version at StartingAsync so a stale
// build is visible on boot.
// OpenIdConnectOptions defaults ClaimActions.DeleteClaim("iss") and then runs ClaimActions on
// an empty JSON payload after TokenValidated, which strips iss from the id_token identity.
// Remove that delete so TryRead sees iss. OnTokenValidated also copies SecurityToken.Issuer
// when the JWT identity omitted iss (JsonWebToken first-class Issuer property).
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

public static class EntraAuthenticationRegistration
{
  public static void AddNamedEntraScheme(AuthenticationBuilder authenticationBuilder, IConfiguration configuration)
  {
    ArgumentNullException.ThrowIfNull(authenticationBuilder);
    ArgumentNullException.ThrowIfNull(configuration);

    authenticationBuilder.Services.AddHostedService<EntraSchemeRegistrationLogHostedService>();
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
        options.ClaimActions.Remove("iss");
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Prompt = "select_account";
        options.SignInScheme = IdentitySessionDefaults.Scheme;
        options.TokenValidationParameters.IssuerValidator = EntraIssuerValidator.Validate;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Events.OnRedirectToIdentityProvider = context =>
        {
          context.ProtocolMessage.Prompt = "select_account";
          ApplyPublicRedirectUri(context.HttpContext, context.ProtocolMessage);
          return Task.CompletedTask;
        };
        options.Events.OnAuthorizationCodeReceived = context =>
        {
          ApplyPublicRedirectUri(context.HttpContext, context.TokenEndpointRequest);
          return Task.CompletedTask;
        };
        options.Events.OnTokenValidated = context =>
        {
          CopyIssuerClaimIfMissing(context.Principal, context.SecurityToken);
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

  private static void CopyIssuerClaimIfMissing(ClaimsPrincipal? principal, SecurityToken? securityToken)
  {
    if (principal is null || securityToken is null)
    {
      return;
    }

    if (principal.FindFirst("iss") is not null)
    {
      return;
    }

    string? issuer = securityToken.Issuer;
    if (string.IsNullOrWhiteSpace(issuer))
    {
      return;
    }

    if (principal.Identity is not ClaimsIdentity claimsIdentity)
    {
      return;
    }

    claimsIdentity.AddClaim(new Claim("iss", issuer));
  }

  private static void ApplyPublicRedirectUri(HttpContext httpContext, OpenIdConnectMessage? message)
  {
    if (message is null)
    {
      return;
    }

    EntraAuthenticationOptions entraOptions = httpContext.RequestServices
      .GetRequiredService<IOptions<EntraAuthenticationOptions>>().Value;
    if (entraOptions.TryGetPublicRedirectUri(out string? redirectUri))
    {
      message.RedirectUri = redirectUri;
    }
  }
}
