#region Purpose
// Composition root for Web.Server: hosts the Blazor SPA (Server + WebAssembly interactivity), its API endpoints, and the chat hub.
#endregion

#region Design
// ConfigureConfiguration/ConfigureServices/ConfigureMiddleware/ConfigureEndpoints are public
// statics (IAspNetProgram) so integration-test hosts compose the exact production pipeline.
// Cross-cutting registrations delegate to modules (CommonServerModule etc.); PostgresDbModule
// stays a single commented call so the `postgres` feature flag is one-line to enable.
// Serilog bootstrap logger wraps host build so startup crashes are still captured; the app runs
// through RunOaktonCommands to expose environment checks as CLI commands.
// Web.Spa services are registered here too — prerendering runs SPA code on the server.
// API surface is generated FastEndpoints from [ApiEndpoint] web-contracts (no MVC BaseEndpoint
// shims). Pipeline order: UseRouting → UseAuthentication → UseAuthorization → UseAntiforgery
// (Blazor) → UseFastEndpoints. Auth before FE; no FE antiforgery for JSON APIs.
// IncludeAbstractValidators=false — FluentValidationBehavior remains the validation path.
#endregion

#nullable enable

namespace TimeWarp.Architecture.Web.Server;

using TimeWarp.Foundation.Abstractions;
using TimeWarp.Foundation.Common.Infrastructure;
using Serilog;

public class Program : IAspNetProgram
{
  const string ApiVersion = "v1";
  const string ApiTitle = $"TimeWarp.Architecture Web.Server API {ApiVersion}";

  public static async Task<int> Main(string[] argumentArray)
  {
    SelfLog.Enable(Console.Error);
    Thread.CurrentThread.Name = nameof(Main);

    Log.Logger = new LoggerConfiguration()
      .WriteTo.Console()
      .CreateBootstrapLogger();

    using ILoggerFactory loggerFactory = new LoggerFactory();
    loggerFactory.AddSerilog(Log.Logger);

    ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

    try
    {
      Log.Information("Starting web host");
      WebApplicationBuilder builder = WebApplication.CreateBuilder(argumentArray);
      builder.Host.UseSerilog((context, services, configuration) =>
        configuration
          .ReadFrom.Configuration(context.Configuration)
          .ReadFrom.Services(services)
          .Enrich.FromLogContext());

      ConfigureHostApplicationBuilder(builder);
      ConfigureConfiguration(builder.Configuration);
      ConfigureServices(builder.Services, builder.Configuration);

      WebApplication webApplication = builder.Build();

      webApplication.MapDefaultEndpoints();

      Log.Information($"EnvironmentName: {webApplication.Environment.EnvironmentName}");

      ConfigureMiddleware(webApplication);
      ConfigureEndpoints(webApplication);

      return await webApplication.RunOaktonCommands(argumentArray).ConfigureAwait(false);
    }
    catch (Exception exception)
    {
      Log.Fatal(exception, messageTemplate: "Host terminated unexpectedly");
      return 1;
    }
    finally
    {
      await Log.CloseAndFlushAsync().ConfigureAwait(false);
    }
  }

  private static void ConfigureHostApplicationBuilder(IHostApplicationBuilder builder)
  {
    builder.AddServiceDefaults();
  }

  public static void ConfigureConfiguration(ConfigurationManager configurationManager)
  {
    CommonServerModule.ConfigureConfiguration(configurationManager);
  }

  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    serviceCollection.AddSerilog();
    serviceCollection.AddHttpClient();
    serviceCollection.AddHttpClient(ServiceNames.WebServiceName, client => client.BaseAddress = ServiceUriHelper.GetServiceHttpsUri(ServiceNames.WebServiceName));
    serviceCollection.AddHttpClient(ServiceNames.ApiServiceName, client => client.BaseAddress = ServiceUriHelper.GetServiceHttpsUri(ServiceNames.ApiServiceName));

    serviceCollection
      .AddRazorComponents()
      .AddInteractiveServerComponents()
      .AddInteractiveWebAssemblyComponents();

    serviceCollection.AddCascadingAuthenticationState();
    serviceCollection.AddAuthorizationBuilder()
      .AddPolicy
      (
        AgentTokenDefaults.IdentityReadPolicy,
        policy => policy
          .AddAuthenticationSchemes(AgentTokenDefaults.Scheme)
          .RequireAuthenticatedUser()
          .RequireClaim(AgentTokenDefaults.ScopeClaimType, AgentScopes.IdentityRead)
      )
      // Task 110: any signed-in identity-session cookie — see IdentitySessionDefaults.AuthenticatedPolicy's
      // Design region for why this is deliberately not an admin/role-based policy.
      .AddPolicy
      (
        IdentitySessionDefaults.AuthenticatedPolicy,
        policy => policy
          .AddAuthenticationSchemes(IdentitySessionDefaults.Scheme)
          .RequireAuthenticatedUser()
      );
    // TODO: Review the options for this seesm like could just pass whole config???
    serviceCollection.AddPasswordlessSdk(options =>
    {
      options.ApiSecret = configuration["Passwordless:ApiSecret"] ?? throw new InvalidOperationException();
    });
    ConfigureAuthentication(serviceCollection, configuration);

    CommonServerModule.ConfigureServices(serviceCollection, configuration);
    ConfigureSettings(serviceCollection, configuration);
    WebInfrastructureModule.ConfigureServices(serviceCollection, configuration);
    CommonInfrastructureModule.ConfigureServices(serviceCollection, configuration);
    //PostgresDbModule.ConfigureServices(serviceCollection, configuration);
    serviceCollection.AddSingleton<IChatHubService, ChatHubService>();
    CorsPolicy.Any.Apply(serviceCollection);
    ConfigureInfrastructure(serviceCollection);
    serviceCollection.AddSignalR();
    // serviceCollection.AddRazorPages();
    // serviceCollection.AddServerSideBlazor();

    serviceCollection.AddHttpContextAccessor();
    serviceCollection.AddScoped<IBrowserSessionService, CookieBrowserSessionService>();
    serviceCollection.AddScoped<IAgentCallerContext, AgentCallerContext>();

    // AddValidatorsFromAssemblyContaining will register all public Validators as scoped but
    // will NOT register internals. This feature is utilized.
    serviceCollection.AddValidatorsFromAssemblyContaining<TimeWarp.Architecture.Web.Server.IAssemblyMarker>();
    serviceCollection.AddValidatorsFromAssemblyContaining<TimeWarp.Architecture.Web.Contracts.IAssemblyMarker>();

    serviceCollection.AddFastEndpoints(options =>
    {
      // FluentValidationBehavior (mediator) owns validation — do not auto-wire FE validators.
      options.IncludeAbstractValidators = false;
      // ApplicationName is web-server (the endpoint assembly). Without DisableAutoDiscovery, FE
      // would scan that assembly automatically AND again via Assemblies → duplicate routes.
      options.DisableAutoDiscovery = true;
      options.Assemblies =
      [
        typeof(TimeWarp.Architecture.Web.Server.IAssemblyMarker).Assembly
      ];
    });

    serviceCollection.AddResponseCompression
    (
      responseCompressionOptions =>
        responseCompressionOptions.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat
        (
          new[]
          {
            MediaTypeNames.Application.Octet
          }
        )
    );

    Web.Spa.Program.ConfigureServices(serviceCollection, configuration);

    serviceCollection
      .AddMediator
      (
        mediatorServiceConfiguration =>
          mediatorServiceConfiguration.RegisterServicesFromAssemblies
          (
            typeof(TimeWarp.Architecture.Web.Server.IAssemblyMarker).GetTypeInfo().Assembly,
            typeof(TimeWarp.Architecture.Web.Application.IAssemblyMarker).GetTypeInfo().Assembly
          )
      );
    serviceCollection.AddScoped(typeof(IPipelineBehavior<,>), typeof(FluentValidationBehavior<,>));

    CommonServerModule
      .AddOpenApi
      (
        serviceCollection,
        ApiVersion,
        ApiTitle,
        [typeof(TimeWarp.Architecture.Web.Server.IAssemblyMarker), typeof(TimeWarp.Architecture.Web.Contracts.IAssemblyMarker)]
      );
  }
  private static void ConfigureAuthentication(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    serviceCollection.AddMicrosoftIdentityWebAppAuthentication(configuration);
    // serviceCollection.AddMicrosoftIdentityWebApiAuthentication(configuration);

    // A second AddAuthentication() call (no defaultScheme argument) adds this NAMED cookie scheme
    // alongside whatever AddMicrosoftIdentityWebAppAuthentication registered as default — the
    // dormant Entra registration is untouched (lock #10 / 104-021). See IdentitySessionDefaults.
    serviceCollection.AddAuthentication()
      .AddCookie(IdentitySessionDefaults.Scheme, options =>
      {
        options.Cookie.Name = IdentitySessionDefaults.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        // Ceremony/session endpoints are JSON APIs, not browser-redirect flows — an unauthenticated
        // or forbidden request must get a status code, not a 302 to a login page that does not exist.
        options.Events.OnRedirectToLogin = context =>
        {
          context.Response.StatusCode = StatusCodes.Status401Unauthorized;
          return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
          context.Response.StatusCode = StatusCodes.Status403Forbidden;
          return Task.CompletedTask;
        };
      })
      // Agent bearer-token scheme (task 104-004): a THIRD named scheme on the same chain, alongside
      // the identity-session cookie scheme above — neither touches the other, nor the dormant Entra
      // default (lock #10). AgentTokenAuthenticationHandler owns all authenticate/challenge/forbid
      // behavior for this scheme; AuthenticationSchemeOptions carries no scheme-specific settings of
      // its own (token lifetime lives in AgentTokenOptions, bound separately in ConfigureSettings).
      .AddScheme<AuthenticationSchemeOptions, AgentTokenAuthenticationHandler>(AgentTokenDefaults.Scheme, _ => { });
  }

  public static void ConfigureMiddleware(WebApplication webApplication)
  {
    CommonServerModule.ConfigureMiddleware(webApplication);

    // https://docs.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-6.0
    // CORS Is not a security feature, CORS relaxes security.An API is not safer by allowing CORS.
    // Although sometimes, you might want to allow other sites to make cross-origin requests to your app to be functional.
    if (webApplication.Environment.IsDevelopment())
    {
      webApplication.UseCors(CorsPolicy.Any.Name);
      webApplication.UseDeveloperExceptionPage();
      webApplication.UseWebAssemblyDebugging();
    }

    CommonServerModule.UseScalarApiReference(webApplication, ApiVersion, ApiTitle);

    webApplication.UseResponseCompression();
    // Static assets (including the Blazor WASM framework files) are served exclusively by
    // MapStaticAssets in ConfigureEndpoints. Do not add UseBlazorFrameworkFiles or UseStaticFiles:
    // UseBlazorFrameworkFiles' MapWhen branch 404s the dynamic /_framework/resource-collection.*.js
    // endpoint required by WebAssembly interactivity, and UseStaticFiles bypasses the fingerprinted
    // caching headers.
    webApplication.UseRouting();

    // Identity session (task 104-003): named cookie scheme only — the dormant Entra registration's
    // own auth flow is untouched. Ceremony endpoints (register/authenticate) are anonymous by
    // design (they establish the session); GetCurrentSession reads whatever session exists, if any.
    webApplication.UseAuthentication();
    webApplication.UseAuthorization();

    // Blazor antiforgery for interactive components — not applied to FastEndpoints JSON APIs.
    webApplication.UseAntiforgery();

    webApplication.UseFastEndpoints(config =>
    {
      config.Endpoints.RoutePrefix = null;
    });
  }

  public static void ConfigureEndpoints(WebApplication webApplication)
  {
    webApplication.MapStaticAssets();
    webApplication.MapRazorComponents<App>()
      .AddInteractiveServerRenderMode()
      .AddInteractiveWebAssemblyRenderMode()
      .AddAdditionalAssemblies
      (
        typeof(TimeWarp.State.AssemblyMarker).Assembly,
        typeof(TimeWarp.State.Plus.AssemblyMarker).Assembly,
        typeof(TimeWarp.Architecture.Web.Spa.AssemblyMarker).Assembly
      );

    webApplication.MapHealthChecks("/api/health");

    CommonServerModule.ConfigureEndpoints(webApplication);
    webApplication.MapHub<ChatHub>(ChatHubConstants.Route);

    // Map the new endpoint to expose service discovery information
    webApplication.MapGet
    (
      "/service-discovery",
      async context =>
      {
        var services = new Dictionary<string, Uri?>
        {
          { TimeWarp.Foundation.Configuration.ServiceNames.GrpcServiceName, ServiceUriHelper.GetServiceHttpsUri(TimeWarp.Foundation.Configuration.ServiceNames.GrpcServiceName) },
          { TimeWarp.Foundation.Configuration.ServiceNames.ApiServiceName, ServiceUriHelper.GetServiceHttpsUri(TimeWarp.Foundation.Configuration.ServiceNames.ApiServiceName) },
          { TimeWarp.Foundation.Configuration.ServiceNames.WebServiceName, ServiceUriHelper.GetServiceHttpsUri(TimeWarp.Foundation.Configuration.ServiceNames.WebServiceName) }
        };

        await context.Response.WriteAsJsonAsync(services);
      }
    );
  }

  private static void ConfigureSettings(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    serviceCollection
      .AddFluentValidatedOptions<SampleOptions, SampleOptionsValidator>(configuration)
      .ValidateOnStart();

    serviceCollection
      .AddFluentValidatedOptions<WebAuthnOptions, WebAuthnOptionsValidator>(configuration)
      .ValidateOnStart();

    serviceCollection
      .AddFluentValidatedOptions<AgentTokenOptions, AgentTokenOptionsValidator>(configuration)
      .ValidateOnStart();
  }

  private static void ConfigureInfrastructure(IServiceCollection serviceCollection)
  {
    serviceCollection.AddHealthChecks();
    //  .AddDbContextCheck<SqlDbContext>();

    ConfigureEnvironmentChecks(serviceCollection);
    //ConfigureSqlDb(serviceCollection, Configuration);
  }

  private static void ConfigureEnvironmentChecks(IServiceCollection serviceCollection)
  {
    serviceCollection.AddSingleton<SampleEnvironmentCheck>();

    serviceCollection.CheckEnvironment<SampleEnvironmentCheck>
    (
      SampleEnvironmentCheck.Description, sampleEnvironmentCheck => sampleEnvironmentCheck.Check()
    );
  }
}
