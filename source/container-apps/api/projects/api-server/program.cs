#region Purpose
// Api.Server composition root: Aspire-enrolled host wiring Mediator, FastEndpoints, and Oakton.
#endregion

#region Design
// Cross-server setup is delegated to CommonServerModule; only Api-specific wiring belongs here.
// Configure* are public statics (IAspNetProgram) so integration-test hosts compose the exact
// same pipeline as production.
// Validators register from both the server and contracts assemblies — contracts own request
// validators, and only public validators are picked up (internals are deliberately excluded).
// Mediator pipeline behaviors run in registration order: Generic wraps FluentValidation.
// Oakton runs the host so the binary exposes diagnostic commands alongside serving.
// Permissive CORS and Scalar (OpenAPI document UI) are development-only surfaces.
// OpenAPI document registration lives in CommonServerModule.AddOpenApi (FastEndpoints.OpenApi);
// Scalar maps after UseFastEndpoints so endpoint metadata is available.
// AllowEmptyRequestDtos=true so FE.OpenApi accepts propertyless request DTOs (EmptyRequestBinder
// already handles runtime binding; without the flag document generation throws).
// Agent bearer (task 104-030): named agent-token scheme + scope policies (parity with web-server),
// AgentBearerStoresModule for IAgentTokenStore/IPrincipalStore, pipeline
// UseAuthentication → UseAuthorization → UseFastEndpoints. Ceremonies stay on web-server;
// see how-to-agent-identity-host-split-web-vs-api.md.
#endregion

namespace TimeWarp.Architecture.Api.Server;

using Microsoft.AspNetCore.Authentication;
using TimeWarp.Architecture.Abstractions;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Infrastructure;
using TimeWarp.Architecture.Services;
using TimeWarp.Identity;

public class Program : IAspNetProgram
{
  const string ApiTitle = "TimeWarp.Architecture Api.Server API";
  const string ApiVersion = "v1";

  public static Task<int> Main(string[] argumentArray)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(argumentArray);
    builder.AddServiceDefaults();

    ConfigureConfiguration(builder.Configuration);
    ConfigureServices(builder.Services, builder.Configuration);

    WebApplication webApplication = builder.Build();

    webApplication.MapDefaultEndpoints();

    ConfigureMiddleware(webApplication);
    ConfigureEndpoints(webApplication);

    return webApplication.RunOaktonCommands(argumentArray);
  }

  public static void ConfigureConfiguration(ConfigurationManager configurationManager)
  {
    CommonServerModule.ConfigureConfiguration(configurationManager);
  }

  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    CommonServerModule.ConfigureServices(serviceCollection, configuration);
    ConfigureSettings(serviceCollection, configuration);

    CorsPolicy.Any.Apply(serviceCollection);

    AgentBearerStoresModule.ConfigureServices(serviceCollection, configuration);
    serviceCollection.AddHttpContextAccessor();
    serviceCollection.AddScoped<IAgentCallerContext, AgentCallerContext>();

    serviceCollection.AddAuthentication()
      .AddScheme<AuthenticationSchemeOptions, AgentTokenAuthenticationHandler>(AgentTokenDefaults.Scheme, _ => { });

    serviceCollection.AddAuthorizationBuilder()
      .AddPolicy
      (
        AgentTokenDefaults.IdentityReadPolicy,
        policy => policy
          .AddAuthenticationSchemes(AgentTokenDefaults.Scheme)
          .RequireAuthenticatedUser()
          .RequireClaim(AgentTokenDefaults.ScopeClaimType, AgentScopes.IdentityRead)
      )
      .AddPolicy
      (
        AgentTokenDefaults.DemoInvokePolicy,
        policy => policy
          .AddAuthenticationSchemes(AgentTokenDefaults.Scheme)
          .RequireAuthenticatedUser()
          .RequireClaim(AgentTokenDefaults.ScopeClaimType, AgentScopes.DemoInvoke)
      );

    // AddValidatorsFromAssemblyContaining will register all public Validators as scoped but
    // will NOT register internals. This feature is utilized.
    serviceCollection.AddValidatorsFromAssemblyContaining<TimeWarp.Architecture.Api.Server.IAssemblyMarker>();
    serviceCollection.AddValidatorsFromAssemblyContaining<TimeWarp.Architecture.Api.Contracts.IAssemblyMarker>();

    serviceCollection.AddFastEndpoints(options =>
    {
      options.IncludeAbstractValidators = false; //This will run all AbstractValidators in the FastEndpoints pipeline.
      options.Assemblies = new[]
      {
        typeof(TimeWarp.Architecture.Api.Server.IAssemblyMarker).Assembly,
        typeof(TimeWarp.Architecture.Api.Contracts.IAssemblyMarker).Assembly
      };
    });

    CommonServerModule.AddOpenApi(serviceCollection, ApiVersion, ApiTitle);
    serviceCollection
      .AddMediator
      (
        mediatorServiceConfiguration =>
          mediatorServiceConfiguration
            .RegisterServicesFromAssemblies
            (
              typeof(TimeWarp.Architecture.Api.Server.IAssemblyMarker).GetTypeInfo().Assembly,
              typeof(TimeWarp.Architecture.Api.Application.IAssemblyMarker).GetTypeInfo().Assembly
            )
      );
    serviceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(GenericPipelineBehavior<,>));
    serviceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(FluentValidationBehavior<,>));
  }

  public static void ConfigureMiddleware(WebApplication webApplication)
  {
    CommonServerModule.ConfigureMiddleware(webApplication);

    // Auth before FastEndpoints so [EndpointAuthorize] policies see an authenticated principal
    // (same order as web-server: UseAuthentication → UseAuthorization → UseFastEndpoints).
    webApplication.UseAuthentication();
    webApplication.UseAuthorization();

    webApplication.UseFastEndpoints(config =>
    {
      config.Endpoints.RoutePrefix = null;
      // Empty request DTOs (propertyless Query/Command + EmptyRequestBinder) are first-class;
      // FE.OpenApi otherwise throws when generating /openapi/*.json for those endpoints.
      config.Endpoints.AllowEmptyRequestDtos = true;
    });

    if (webApplication.Environment.IsDevelopment())
    {
      webApplication.UseCors(CorsPolicy.Any.Name);
      CommonServerModule.UseScalarApiReference(webApplication, ApiVersion, ApiTitle);
    }
  }

  public static void ConfigureEndpoints(WebApplication webApplication)
  {
    CommonServerModule.ConfigureEndpoints(webApplication);
  }

  private static void ConfigureSettings(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    // Placeholder signature: settings wiring lands here when api-server gains options binding.
    _ = serviceCollection;
    _ = configuration;
  }
}
