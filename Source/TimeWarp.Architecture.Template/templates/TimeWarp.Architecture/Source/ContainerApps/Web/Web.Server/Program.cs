namespace TimeWarp.Architecture.Web.Server;

using FluentValidation.AspNetCore;
using MediatR;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Oakton;
using Oakton.Environment;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.CorsPolicies;
using TimeWarp.Architecture.Data;
using TimeWarp.Architecture.HostedServices;
using TimeWarp.Architecture.Infrastructure;
using TimeWarp.Architecture.Web.Infrastructure;

public class Program : IAspNetProgram
{
  const string SwaggerVersion = "v1";
  const string SwaggerApiTitle = $"TimeWarp.Architecture Web.Server API {SwaggerVersion}";
  const string SwaggerBasePath = "api/web-server";
  const string SwaggerEndpoint = $"/swagger/{SwaggerVersion}/swagger.json";

  public static Task<int> Main(string[] aArgumentArray)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(aArgumentArray);

    ConfigureConfiguration(builder.Configuration);
    ConfigureServices(builder.Services, builder.Configuration);

    WebApplication webApplication = builder.Build();

    Console.WriteLine($"EnvironmentName: {webApplication.Environment.EnvironmentName}");

    ConfigureMiddleware(webApplication);
    ConfigureEndpoints(webApplication);

    webApplication.Services.ValidateOptions(builder.Services);

    return webApplication.RunOaktonCommands(aArgumentArray);
  }
  public static void ConfigureConfiguration(ConfigurationManager aConfigurationManager)
  {
    CommonServerModule.ConfigureConfiguration(aConfigurationManager); ;
  }

  public static void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    CommonServerModule.ConfigureServices(aServiceCollection, aConfiguration);
    ConfigureSettings(aServiceCollection, aConfiguration);
    WebInfrastructureModule.ConfigureServices(aServiceCollection, aConfiguration);
    CorsPolicy.Any.Apply(aServiceCollection);
    ConfigureInfrastructure(aServiceCollection);
    aServiceCollection.AddAutoMapper(typeof(MappingProfile).Assembly);
    aServiceCollection.AddRazorPages();
    aServiceCollection.AddServerSideBlazor();
    aServiceCollection.AddMvc()
      .TryAddApplicationPart(typeof(Web_Server_Assembly).Assembly)
      .AddFluentValidation
      (
        aFluentValidationMvcConfiguration =>
        {
          // RegisterValidatorsFromAssemblyContaining will register all public Validators as scoped but
          // will NOT register internals. This feature is utilized.
          aFluentValidationMvcConfiguration.RegisterValidatorsFromAssemblyContaining<Web_Server_Assembly>();
          aFluentValidationMvcConfiguration.RegisterValidatorsFromAssemblyContaining<Web_Contracts_Assembly>();
        }
      );

    aServiceCollection.Configure<ApiBehaviorOptions>
    (
      aApiBehaviorOptions => aApiBehaviorOptions.SuppressInferBindingSourcesForParameters = true
    );

    aServiceCollection.AddResponseCompression
    (
      aResponseCompressionOptions =>
        aResponseCompressionOptions.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat
        (
          new[] { MediaTypeNames.Application.Octet }
        )
    );

    Web.Spa.Program.ConfigureServices(aServiceCollection, aConfiguration);

    aServiceCollection
      .AddMediatR
      (
        typeof(Web_Server_Assembly).GetTypeInfo().Assembly,
        typeof(Web_Application_Assembly).GetTypeInfo().Assembly
      );

    CommonServerModule
      .AddSwaggerGen
      (
        aServiceCollection,
        SwaggerVersion,
        SwaggerApiTitle,
        new Type[] { typeof(Web_Server_Assembly), typeof(Web_Contracts_Assembly) }
      );
  }

  public static void ConfigureMiddleware(WebApplication aWebApplication)
  {
    CommonServerModule.ConfigureMiddleware(aWebApplication);

    // https://docs.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-6.0
    // CORS Is not a security feature, CORS relaxes security.An API is not safer by allowing CORS.
    // Although sometimes, you might want to allow other sites to make cross-origin requests to your app to be functional.
    if (aWebApplication.Environment.IsDevelopment())
    {
      aWebApplication.UseCors(CorsPolicy.Any.Name);
      aWebApplication.UseDeveloperExceptionPage();
      aWebApplication.UseWebAssemblyDebugging();
    }

    CommonServerModule.UseSwaggerUi(aWebApplication, SwaggerBasePath, SwaggerEndpoint, SwaggerApiTitle);

    aWebApplication.UseResponseCompression();

    aWebApplication.UseRouting();

    aWebApplication.UseEndpoints
    (
      aEndpointRouteBuilder =>
      {
        aEndpointRouteBuilder.MapHealthChecks("/api/health");
        aEndpointRouteBuilder.MapControllers();
        aEndpointRouteBuilder.MapBlazorHub();
        aEndpointRouteBuilder.MapFallbackToPage("/_Host");
      }
    );

    aWebApplication.UseStaticFiles();
    aWebApplication.UseBlazorFrameworkFiles();
  }

  public static void ConfigureEndpoints(WebApplication aWebApplication)
  {
    CommonServerModule.ConfigureEndpoints(aWebApplication);
    aWebApplication.MapControllers();
  }

  private static void ConfigureSettings(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    aServiceCollection
      .ConfigureOptions<SampleOptions, SampleOptionsValidator>(aConfiguration);

    aServiceCollection.Configure<ApiBehaviorOptions>
    (
      aApiBehaviorOptions => aApiBehaviorOptions.SuppressInferBindingSourcesForParameters = true
    );
  }

  private static void ConfigureInfrastructure(IServiceCollection aServiceCollection)
  {
    aServiceCollection.AddHealthChecks()
      .AddDbContextCheck<CosmosDbContext>
      (
        name: nameof(CosmosDbContext),
        HealthStatus.Unhealthy,
        null,
        PerformCosmosHealthCheck()
      );
    //.AddDbContextCheck<SqlDbContext>();

    ConfigureEnvironmentChecks(aServiceCollection);
    ConfigureCosmosDb(aServiceCollection);
    //ConfigureSqlDb(aServiceCollection, Configuration);
    aServiceCollection.AddHostedService<StartupHostedService>();
  }

  private static Func<CosmosDbContext, CancellationToken, Task<bool>> PerformCosmosHealthCheck() =>
    async (aCosmosDbContext, _) =>
    {
      try
      {
        await aCosmosDbContext.Database.GetCosmosClient().ReadAccountAsync().ConfigureAwait(true);
      }
      catch (HttpRequestException)
      {
        return false;
      }

      return true;
    };

  private static void ConfigureEnvironmentChecks(IServiceCollection aServiceCollection)
  {
    aServiceCollection.AddSingleton<SampleEnvironmentCheck>();
    aServiceCollection.AddSingleton<CosmosDbEnvironmentCheck>();

    aServiceCollection.CheckEnvironment<SampleEnvironmentCheck>
    (
      SampleEnvironmentCheck.Description, aSampleEnvironmentCheck => aSampleEnvironmentCheck.Check()
    );

    aServiceCollection.CheckEnvironment<CosmosDbEnvironmentCheck>
    (
      CosmosDbEnvironmentCheck.Description,
      async (aCosmosDbEnvironmentCheck) => await aCosmosDbEnvironmentCheck.CheckAsync()
    );
  }

  private static void ConfigureCosmosDb(IServiceCollection aServiceCollection)
  {

    using IServiceScope scope = aServiceCollection.BuildServiceProvider().CreateScope();
    {
      CosmosDbOptions cosmosOptions = scope.ServiceProvider.GetRequiredService<IOptions<CosmosDbOptions>>().Value;

      _ = aServiceCollection.AddDbContext<CosmosDbContext>
      (
        aDbContextOptionsBuilder =>
          aDbContextOptionsBuilder
          .UseCosmos
          (
            accountEndpoint: cosmosOptions.Endpoint,
            accountKey: cosmosOptions.AccessKey,
            databaseName: nameof(CosmosDbContext),
            cosmosOptionsAction: CosmosOptionsAction()
          )
      );
    }

    static Action<Microsoft.EntityFrameworkCore.Infrastructure.CosmosDbContextOptionsBuilder> CosmosOptionsAction()
    {
      return _ => new CosmosClientOptions
      {
        HttpClientFactory = () =>
        {
          HttpMessageHandler httpMessageHandler = new HttpClientHandler()
          {
            ServerCertificateCustomValidationCallback =
              HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
          };

          return new HttpClient(httpMessageHandler);
        },
        ConnectionMode = ConnectionMode.Gateway
      };
    }
  }
}
