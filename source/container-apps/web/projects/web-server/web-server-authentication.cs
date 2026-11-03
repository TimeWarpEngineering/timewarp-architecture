#region Purpose
// Web.Server authentication schemes and authorization policies, and the server-side permission evaluation.
#endregion

#region Design
// Task 154: identity-session cookie OnRedirectToLogin is dual-mode — HTML/page deep links 302 to
// /Login?returnUrl=…; /api challenges stay 401. Classification SSOT:
// IdentitySessionCookieChallenge (platform/identity-host). Forbid always 403 (never Login).
// Task 183: after Web.Spa.Program.ConfigureServices, re-register AuthenticationStateProvider as
// HostedIdentitySessionAuthenticationStateProvider so prerender uses HttpContext.User (cookie)
// instead of an anonymous session self-call — see that type's Design region.
// Task 230: identity-session OnValidatePrincipal rejects bad PrincipalId claims and inactive/
// merged principals, then SignOutAsync(IdentitySessionDefaults.Scheme) so the stale cookie is
// cleared once instead of re-validated on every request.
#endregion

namespace TimeWarp.Architecture.Web.Server;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Admin.Principals;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Features.Identity.Application;

/// <summary>Authentication schemes, authorization policies, and permission evaluation for Web.Server.</summary>
internal static class WebServerAuthentication
{
  /// <summary>
  /// Cascading authentication state, the authenticated and permission policies, and the
  /// identity-session, agent-token, mock and (when listed) Entra schemes.
  /// </summary>
  internal static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    serviceCollection.AddCascadingAuthenticationState();
    serviceCollection.AddAuthorizationBuilder()
      // Task 110: any signed-in identity-session cookie — see IdentitySessionDefaults.AuthenticatedPolicy's
      // Design region for why this is deliberately not an admin/role-based policy.
      // Round-1 review M4 (nit): this explicit AddAuthenticationSchemes(IdentitySessionDefaults.Scheme)
      // restriction is WHY roles endpoints get a clean 401 for an unauthenticated request. A bare
      // fail-closed [ApiEndpoint] with no marker at all (only reachable if TWA0013 is suppressed) has
      // no policy to restrict the scheme — it falls through to ASP.NET Core's DEFAULT authentication
      // scheme (always identity-session; RFC 219 D10 retired the 104-021 Entra-as-default branch).
      // identity-session cookie events return 401/403. The clean-401 property specifically belongs
      // to an explicit scheme-restricted policy like this one, not to the bare fail-closed default.
      .AddPolicy
      (
        IdentitySessionDefaults.AuthenticatedPolicy,
        policy => policy
          // mock-identity-session (task 145-009): fail-closed header auth for closed-box BFF;
          // handler returns NoResult when mock is off, so Production/normal Dev are unchanged.
          .AddAuthenticationSchemes(IdentitySessionDefaults.Scheme, MockIdentityPrincipalHandler.SchemeName)
          .RequireAuthenticatedUser()
      );
    // Task 182-002/003/006: permission-centric policies (policy name == PermissionIds). Admin,
    // credential, agent identity, and metered-demo contracts use PermissionIds; schemes stay on
    // [EndpointAuthorize(AuthenticationSchemes)]. Agent scope→permission expansion is in
    // PermissionEvaluator (IAgentPermissionScopeSource → IAgentCallerContext). SPA uses
    // AddPermissionClaimPolicies separately.
    // IDE0200 wants a method group. TWA0024 only sees the call
    // PermissionPolicyRegistration.AddPermissionPolicies(options), so the lambda stays.
#pragma warning disable IDE0200
    serviceCollection.AddAuthorization(options =>
      PermissionPolicyRegistration.AddPermissionPolicies(options));
#pragma warning restore IDE0200
    AddAuthenticationSchemes(serviceCollection, configuration);
  }

  /// <summary>
  /// Effective roles, the permission evaluator and handler, and the role claims transformation.
  /// Registered after the identity stores and request accessors (see Program.ConfigureServices).
  /// </summary>
  internal static void ConfigurePermissionEvaluation(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    // Task 147-004 / 147-006: effective roles + request claims (PrincipalRoleClaimsTransformation
    // still projects roles for diagnostics). Resolver is scoped so it can resolve
    // EfPrincipalRoleStore under postgres without a captive dependency.
    // Task 182-002: PermissionRequirementHandler is the server enforcement path — always via
    // IPermissionEvaluator (scheme-aware expansion of roles→permissions). SPA projects
    // GetCurrentSession.Permissions as claims (182-003).
    serviceCollection.Configure<BootstrapAdministratorOptions>(
      configuration.GetSection("Authentication"));
    serviceCollection.AddScoped<IEffectiveRolesResolver, EffectiveRolesResolver>();
    serviceCollection.AddScoped<IPermissionEvaluator, PermissionEvaluator>();
    serviceCollection.AddScoped<IAuthorizationHandler, PermissionRequirementHandler>();
    serviceCollection.AddScoped<IClaimsTransformation, PrincipalRoleClaimsTransformation>();
  }

  /// <summary>
  /// Task 183: last registration wins. Call after Web.Spa.Program.ConfigureServices so hosted
  /// prerender reads the cookie principal.
  /// </summary>
  internal static void PreferHostedIdentitySessionState(IServiceCollection serviceCollection)
  {
    // Task 183: last registration wins — prefer cookie principal during hosted prerender.
    // Only when SPA registered IdentitySessionAuthenticationStateProvider (not mock / Entra).
    if (serviceCollection.Any(static d =>
          d.ServiceType == typeof(AuthenticationStateProvider)
          && d.ImplementationType == typeof(IdentitySessionAuthenticationStateProvider)))
    {
      serviceCollection.AddScoped<AuthenticationStateProvider, HostedIdentitySessionAuthenticationStateProvider>();
    }
  }

  private static void AddAuthenticationSchemes(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    // RFC 219 D10: identity-session is always DefaultScheme. Entra is a named OIDC scheme when
    // SignInOptions lists Microsoft365 — never AddMicrosoftIdentityWebAppAuthentication (that
    // helper steals DefaultScheme). The list is read from configuration here so scheme
    // registration does not wait on options validation.
    bool microsoft365Listed = SignInOptions.ConfigurationLists(configuration, SignInMethod.Microsoft365);

    serviceCollection.AddSingleton<IValidateOptions<EntraAuthenticationOptions>, EntraAuthenticationOptionsStartupValidator>();
    serviceCollection.AddOptions<EntraAuthenticationOptions>()
      .Bind(configuration.GetSection(EntraAuthenticationOptions.SectionKey))
      .ValidateOnStart();

    AuthenticationBuilder authenticationBuilder = serviceCollection.AddAuthentication(IdentitySessionDefaults.Scheme);

    authenticationBuilder
      .AddCookie(IdentitySessionDefaults.Scheme, options =>
      {
        options.Cookie.Name = IdentitySessionDefaults.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        // Dual-mode challenge (task 154): non-API HTML/page deep links 302 to /Login?returnUrl=…
        // so the SPA and task-153 client flow can run; /api/… stays 401 (contract seam). Forbid
        // is always 403 — authenticated-but-insufficient policy must not bounce to Login.
        // Classification SSOT: IdentitySessionCookieChallenge.
        options.Events.OnRedirectToLogin = context =>
        {
          if (IdentitySessionCookieChallenge.ShouldRedirectToLogin(context.Request))
          {
            context.Response.Redirect(
              IdentitySessionCookieChallenge.BuildLoginRedirectTarget(context.Request));
          }
          else
          {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
          }

          return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
          context.Response.StatusCode = StatusCodes.Status403Forbidden;
          return Task.CompletedTask;
        };
        options.Events.OnValidatePrincipal = async context =>
        {
          IPrincipalStore principalStore = context.HttpContext.RequestServices.GetRequiredService<IPrincipalStore>();
          string? claimValue = context.Principal?.FindFirstValue(IdentitySessionDefaults.PrincipalIdClaimType);
          if (!Guid.TryParse(claimValue, out Guid guid) || guid == Guid.Empty)
          {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentitySessionDefaults.Scheme);
            return;
          }

          Principal? principal = await principalStore.GetPrincipalAsync(
            PrincipalId.From(guid),
            context.HttpContext.RequestAborted);
          if (principal?.IsActive != true)
          {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentitySessionDefaults.Scheme);
          }
        };
      })
      // Agent bearer-token scheme (task 104-004): named scheme alongside identity-session.
      // AgentTokenAuthenticationHandler owns authenticate/challenge/forbid for this scheme;
      // token lifetime lives in AgentTokenOptions (WebServerSettings).
      .AddScheme<AuthenticationSchemeOptions, AgentTokenAuthenticationHandler>(AgentTokenDefaults.Scheme, _ => { })
      // Closed-box mock principal (task 145-009): always registered; handler is fail-closed
      // (Development/Testing + Authentication:UseMock + header). Listed on AuthenticatedPolicy.
      .AddScheme<AuthenticationSchemeOptions, MockIdentityPrincipalHandler>(MockIdentityPrincipalHandler.SchemeName, _ => { });

    serviceCollection.AddScoped<EntraTicketProcessor>();
    if (microsoft365Listed)
    {
      EntraAuthenticationRegistration.AddNamedEntraScheme(authenticationBuilder, configuration);
    }
  }
}
