#region Purpose
// Proves identity-session is always DefaultScheme and entra is a named OIDC scheme only when enabled.
#endregion

#region Design
// RFC 219 D10 composition pin. Calls Web.Server.Program.ConfigureServices (same path as the
// IModule 2-arg overload) and inspects the resulting IServiceCollection / a built provider —
// no live tenant. UseEntra synonym must register entra AND keep identity-session as default.
// AddMicrosoftIdentityWebAppAuthentication is gone; OpenIdConnectHandler is the named scheme.
// Multi-tenant TenantId=organizations must install EntraIssuerValidator on the named scheme.
#endregion

namespace EntraSchemeRegistration_;

using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Services;
using TimeWarp.Identity;
using WebServerProgram = TimeWarp.Architecture.Web.Server.Program;

public class ConfigureAuthentication_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ConfigureAuthentication_Given_>();

  private static WebApplicationBuilder CreateBuilder(string environmentName)
  {
    WebApplicationBuilder builder =
      WebApplication.CreateBuilder
      (
        new WebApplicationOptions
        {
          ApplicationName = typeof(TimeWarp.Architecture.Web.Server.IAssemblyMarker).Assembly.GetName().Name,
          EnvironmentName = environmentName,
          ContentRootPath = ProjectContentRoot.Resolve(typeof(TimeWarp.Architecture.Web.Server.IAssemblyMarker).Assembly),
        }
      );

    IList<IConfigurationSource> configurationSources = ((IConfigurationBuilder)builder.Configuration).Sources;
    for (int index = configurationSources.Count - 1; index >= 0; index--)
    {
      if (configurationSources[index] is JsonConfigurationSource jsonSource && jsonSource.Path == "secrets.json")
        configurationSources.RemoveAt(index);
    }

    return builder;
  }

  private static bool HasOpenIdConnectHandler(IServiceCollection serviceCollection) =>
    serviceCollection.Any(descriptor => descriptor.ImplementationType == typeof(OpenIdConnectHandler));

  public static async Task Default_Host_Should_Use_Identity_Session_And_Not_Register_Entra()
  {
    WebApplicationBuilder builder = CreateBuilder(Environments.Development);
    WebServerProgram.ConfigureServices(builder.Services, builder.Configuration);

    HasOpenIdConnectHandler(builder.Services).ShouldBeFalse(
      "Entra OIDC must not register when Authentication:Entra:Enabled is false.");

    await using ServiceProvider provider = builder.Services.BuildServiceProvider(validateScopes: false);
    AuthenticationOptions authenticationOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
    authenticationOptions.DefaultScheme.ShouldBe(IdentitySessionDefaults.Scheme);
  }

  public static async Task Entra_Enabled_Should_Keep_Identity_Session_Default_And_Register_Named_Entra()
  {
    Guid tenantId = Guid.Parse("30f3971f-4719-4f20-9b6f-88916e0b95bd");
    WebApplicationBuilder builder = CreateBuilder(Environments.Development);
    builder.Configuration.AddInMemoryCollection
    (
      new Dictionary<string, string?>
      {
        [MockAuthenticationDefaults.EntraEnabledKey] = "true",
        ["Authentication:Entra:Instance"] = "https://login.microsoftonline.com/",
        ["Authentication:Entra:TenantId"] = tenantId.ToString("D"),
        ["Authentication:Entra:ClientId"] = Guid.NewGuid().ToString("D"),
        ["Authentication:Entra:CallbackPath"] = "/signin-oidc",
        ["Authentication:Entra:TrustedTenants:0"] = tenantId.ToString("D"),
        ["Authentication:Entra:AllowBootstrap"] = "true"
      }
    );

    WebServerProgram.ConfigureServices(builder.Services, builder.Configuration);

    HasOpenIdConnectHandler(builder.Services).ShouldBeTrue(
      "Enabled Entra must register OpenIdConnectHandler for the named entra scheme.");

    await using ServiceProvider provider = builder.Services.BuildServiceProvider(validateScopes: false);
    AuthenticationOptions authenticationOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
    authenticationOptions.DefaultScheme.ShouldBe(
      IdentitySessionDefaults.Scheme,
      "Entra must never become DefaultScheme (RFC 219 D10).");
    authenticationOptions.DefaultChallengeScheme.ShouldNotBe(EntraLinkDefaults.Scheme);

    IAuthenticationSchemeProvider schemes = provider.GetRequiredService<IAuthenticationSchemeProvider>();
    AuthenticationScheme? entra = await schemes.GetSchemeAsync(EntraLinkDefaults.Scheme);
    entra.ShouldNotBeNull();
    entra!.HandlerType.ShouldBe(typeof(OpenIdConnectHandler));
    AuthenticationScheme? identitySession = await schemes.GetSchemeAsync(IdentitySessionDefaults.Scheme);
    identitySession.ShouldNotBeNull();
  }

  public static async Task Obsolete_UseEntra_Should_Enable_Named_Scheme_Not_Default()
  {
    Guid tenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    WebApplicationBuilder builder = CreateBuilder(Environments.Development);
    builder.Configuration.AddInMemoryCollection
    (
      new Dictionary<string, string?>
      {
        [MockAuthenticationDefaults.UseEntraKey] = "true",
        ["Authentication:Entra:Instance"] = "https://login.microsoftonline.com/",
        ["Authentication:Entra:TenantId"] = tenantId.ToString("D"),
        ["Authentication:Entra:ClientId"] = Guid.NewGuid().ToString("D"),
        ["Authentication:Entra:CallbackPath"] = "/signin-oidc",
        ["Authentication:Entra:TrustedTenants:0"] = tenantId.ToString("D")
      }
    );

    WebServerProgram.ConfigureServices(builder.Services, builder.Configuration);

    HasOpenIdConnectHandler(builder.Services).ShouldBeTrue(
      "Obsolete UseEntra synonym must map to named entra, not skip registration.");

    await using ServiceProvider provider = builder.Services.BuildServiceProvider(validateScopes: false);
    AuthenticationOptions authenticationOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
    authenticationOptions.DefaultScheme.ShouldBe(
      IdentitySessionDefaults.Scheme,
      "UseEntra synonym must never restore Entra as DefaultScheme.");
  }

  public static async Task Organizations_Tenant_Should_Install_Entra_Issuer_Validator()
  {
    Guid tenantId = Guid.Parse("30f3971f-4719-4f20-9b6f-88916e0b95bd");
    string expectedIssuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(tenantId));
    WebApplicationBuilder builder = CreateBuilder(Environments.Development);
    builder.Configuration.AddInMemoryCollection
    (
      new Dictionary<string, string?>
      {
        [MockAuthenticationDefaults.EntraEnabledKey] = "true",
        ["Authentication:Entra:Instance"] = "https://login.microsoftonline.com/",
        ["Authentication:Entra:TenantId"] = "organizations",
        ["Authentication:Entra:ClientId"] = Guid.NewGuid().ToString("D"),
        ["Authentication:Entra:CallbackPath"] = "/signin-oidc",
        ["Authentication:Entra:TrustedTenants:0"] = tenantId.ToString("D"),
        ["Authentication:Entra:AllowBootstrap"] = "true"
      }
    );

    WebServerProgram.ConfigureServices(builder.Services, builder.Configuration);

    await using ServiceProvider provider = builder.Services.BuildServiceProvider(validateScopes: false);
    OpenIdConnectOptions openIdConnectOptions =
      provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(EntraLinkDefaults.Scheme);
    openIdConnectOptions.TokenValidationParameters.IssuerValidator.ShouldNotBeNull(
      "organizations authority must install EntraIssuerValidator so concrete tid issuers are accepted.");

    JsonWebToken matchingToken = CreateUnsignedJsonWebToken(expectedIssuer, tenantId);
    string accepted = openIdConnectOptions.TokenValidationParameters.IssuerValidator!(
      expectedIssuer,
      matchingToken,
      openIdConnectOptions.TokenValidationParameters);
    accepted.ShouldBe(expectedIssuer);

    const string wrongIssuer = "https://login.microsoftonline.com/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/v2.0";
    Should.Throw<SecurityTokenInvalidIssuerException>
    (
      () => openIdConnectOptions.TokenValidationParameters.IssuerValidator!(
        wrongIssuer,
        matchingToken,
        openIdConnectOptions.TokenValidationParameters)
    );
  }

  private static JsonWebToken CreateUnsignedJsonWebToken(string issuer, Guid tenantId)
  {
    string header = Base64UrlEncoder.Encode("""{"alg":"none","typ":"JWT"}""");
    string payload = Base64UrlEncoder.Encode($"{{\"iss\":\"{issuer}\",\"tid\":\"{tenantId:D}\"}}");
    return new JsonWebToken($"{header}.{payload}.");
  }
}
