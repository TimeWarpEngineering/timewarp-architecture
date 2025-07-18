#nullable enable
// ReSharper disable RedundantNameQualifier
namespace TimeWarp.Architecture.Web.Server;

using Abstractions;
using Common.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;


public class Program : IAspNetProgram
{
  const string SwaggerVersion = "v1";
  const string SwaggerApiTitle = $"TimeWarp.Architecture Web.Server API {SwaggerVersion}";
  const string SwaggerBasePath = "api/web-server";
  const string SwaggerEndpoint = $"/swagger/{SwaggerVersion}/swagger.json";

  public static Task<int> Main(string[] argumentArray)
  {
    SelfLog.Enable(Console.Error);
    Thread.CurrentThread.Name = nameof(Main);

    Log.Logger = new LoggerConfiguration()
      .WriteTo.Console()
      .CreateBootstrapLogger();

    ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog(Log.Logger);

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

      webApplication.Services.ValidateOptions(builder.Services, logger);

      return webApplication.RunOaktonCommands(argumentArray);
    }
    catch (Exception exception)
    {
      Log.Fatal(exception, messageTemplate: "Host terminated unexpectedly");
      return Task.FromResult(1);
    }
    finally
    {
      Log.CloseAndFlush();
    }
  }
  private static void ConfigureHostApplicationBuilder(IHostApplicationBuilder builder)
  {
    builder.AddServiceDefaults();
    #if(cosmosdb)
    CosmosDbModule.ConfigureHostApplicationBuilder(builder);
    #endif
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
    serviceCollection.AddAuthorization();
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
#if(cosmosdb)
    CosmosDbModule.ConfigureServices(serviceCollection, configuration);
#endif
    //PostgresDbModule.ConfigureServices(aServiceCollection, aConfiguration);
    serviceCollection.AddSingleton<IChatHubService, ChatHubService>();
    CorsPolicy.Any.Apply(serviceCollection);
    ConfigureInfrastructure(serviceCollection);
    serviceCollection.AddSignalR();
    serviceCollection.AddAutoMapper(typeof(TimeWarp.Architecture.Web.Application.IAssemblyMarker).Assembly);
    // serviceCollection.AddRazorPages();
    // serviceCollection.AddServerSideBlazor();
    serviceCollection.AddMvc()
      .TryAddApplicationPart(typeof(TimeWarp.Architecture.Web.Server.IAssemblyMarker).Assembly);

    serviceCollection.AddFluentValidationAutoValidation();
    serviceCollection.AddFluentValidationClientsideAdapters();

    // AddValidatorsFromAssemblyContaining will register all public Validators as scoped but
    // will NOT register internals. This feature is utilized.
    serviceCollection.AddValidatorsFromAssemblyContaining<TimeWarp.Architecture.Web.Server.IAssemblyMarker>();
    serviceCollection.AddValidatorsFromAssemblyContaining<TimeWarp.Architecture.Web.Contracts.IAssemblyMarker>();

    serviceCollection.Configure<ApiBehaviorOptions>
    (
      aApiBehaviorOptions => aApiBehaviorOptions.SuppressInferBindingSourcesForParameters = true
    );

    serviceCollection.AddResponseCompression
    (
      aResponseCompressionOptions =>
        aResponseCompressionOptions.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat
        (
          new[]
          {
            MediaTypeNames.Application.Octet
          }
        )
    );

    Web.Spa.Program.ConfigureServices(serviceCollection, configuration);

    serviceCollection
      .AddMediatR
      (
        mediatRServiceConfiguration =>
          mediatRServiceConfiguration.RegisterServicesFromAssemblies
          (
            typeof(TimeWarp.Architecture.Web.Server.IAssemblyMarker).GetTypeInfo().Assembly,
            typeof(TimeWarp.Architecture.Web.Application.IAssemblyMarker).GetTypeInfo().Assembly
          )
      );

    CommonServerModule
      .AddSwaggerGen
      (
        serviceCollection,
        SwaggerVersion,
        SwaggerApiTitle,
        [typeof(TimeWarp.Architecture.Web.Server.IAssemblyMarker), typeof(TimeWarp.Architecture.Web.Contracts.IAssemblyMarker)]
      );
  }
  private static void ConfigureAuthentication(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    serviceCollection.AddMicrosoftIdentityWebAppAuthentication(configuration);
    // serviceCollection.AddMicrosoftIdentityWebApiAuthentication(configuration);
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

    CommonServerModule.UseSwaggerUi(webApplication, SwaggerBasePath, SwaggerEndpoint, SwaggerApiTitle);

    webApplication.UseResponseCompression();
    webApplication.UseBlazorFrameworkFiles();
    webApplication.UseStaticFiles();
    webApplication.UseRouting();
    webApplication.UseAntiforgery();
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
    webApplication.MapControllers();
    webApplication.MapHub<ChatHub>(ChatHubConstants.Route);

    // Map the new endpoint to expose service discovery information
    webApplication.MapGet
    (
      "/service-discovery",
      async context =>
      {
        var services = new Dictionary<string, Uri?>
        {
          { Configuration.ServiceNames.GrpcServiceName, ServiceUriHelper.GetServiceHttpsUri(Configuration.ServiceNames.GrpcServiceName) },
          { Configuration.ServiceNames.ApiServiceName, ServiceUriHelper.GetServiceHttpsUri(Configuration.ServiceNames.ApiServiceName) },
          { Configuration.ServiceNames.WebServiceName, ServiceUriHelper.GetServiceHttpsUri(Configuration.ServiceNames.WebServiceName) }
        };

        await context.Response.WriteAsJsonAsync(services);
      }
    );
  }

  private static void ConfigureSettings(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    serviceCollection
      .ConfigureOptions<SampleOptions, SampleOptionsValidator>(configuration);

    serviceCollection.Configure<ApiBehaviorOptions>
    (
      aApiBehaviorOptions => aApiBehaviorOptions.SuppressInferBindingSourcesForParameters = true
    );
  }

  private static void ConfigureInfrastructure(IServiceCollection serviceCollection)
  {
    serviceCollection.AddHealthChecks();
    //  .AddDbContextCheck<SqlDbContext>();

    ConfigureEnvironmentChecks(serviceCollection);
    //ConfigureSqlDb(aServiceCollection, Configuration);
  }

  private static void ConfigureEnvironmentChecks(IServiceCollection serviceCollection)
  {
    serviceCollection.AddSingleton<SampleEnvironmentCheck>();

    serviceCollection.CheckEnvironment<SampleEnvironmentCheck>
    (
      SampleEnvironmentCheck.Description, aSampleEnvironmentCheck => aSampleEnvironmentCheck.Check()
    );
  }
}
