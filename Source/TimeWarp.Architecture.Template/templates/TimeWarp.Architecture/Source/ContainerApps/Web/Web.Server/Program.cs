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
using ProtoBuf.Grpc.Server;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Data;
using TimeWarp.Architecture.Features.Bases;
using TimeWarp.Architecture.HostedServices;
using TimeWarp.Architecture.Infrastructure;

public class Program : IProgram
{
  const string SwaggerVersion = "v1";
  const string SwaggerApiTitle = $"TimeWarp Architecture API {SwaggerVersion}";
  const string SwaggerEndPoint = $"/swagger/{SwaggerVersion}/swagger.json";
  //  public static IHostBuilder CreateHostBuilder(string[] aArgumentArray) =>
  //    Host
  //      .CreateDefaultBuilder(aArgumentArray)
  //      .ConfigureWebHostDefaults
  //      (
  //        aWebHostBuilder =>
  //        {
  //          aWebHostBuilder.UseStaticWebAssets();
  //          aWebHostBuilder.UseStartup<Startup>();
  //        }
  //      );

  public static Task<int> Main(string[] aArgumentArray)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(aArgumentArray);

    ConfigureConfiguration(builder.Configuration);
    ConfigureServices(builder.Services, builder.Configuration);

    WebApplication webApplication = builder.Build();

    ConfigureMiddleware(webApplication, webApplication.Services, webApplication.Environment);
    ConfigureEndpoints(webApplication, webApplication.Services);

    webApplication.Services.ValidateOptions(builder.Services);

    return webApplication.RunOaktonCommands(aArgumentArray);
  }

  public static void ConfigureConfiguration(ConfigurationManager aConfigurationManager) { }

  public static void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    ConfigureSettings(aServiceCollection, aConfiguration);
    ConfigureInfrastructure(aServiceCollection);
    aServiceCollection.AddAutoMapper(typeof(MappingProfile).Assembly);
    aServiceCollection.AddRazorPages();
    aServiceCollection.AddServerSideBlazor();
    aServiceCollection.AddCodeFirstGrpc();
    aServiceCollection.AddCodeFirstGrpcReflection();
    aServiceCollection.AddMvc()
      .AddFluentValidation
      (
        aFluentValidationMvcConfiguration =>
        {
          // RegisterValidatorsFromAssemblyContaining will register all public Validators as scoped but
          // will NOT register internals. This feature is utilized.
          aFluentValidationMvcConfiguration.RegisterValidatorsFromAssemblyContaining<Program>();
          aFluentValidationMvcConfiguration.RegisterValidatorsFromAssemblyContaining<BaseRequest>();
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

    aServiceCollection.AddMediatR(typeof(Program).GetTypeInfo().Assembly);

    ConfigureSwagger(aServiceCollection);
  }

  public static void ConfigureMiddleware(IApplicationBuilder aApplicationBuilder, IServiceProvider aServiceCollection, IHostEnvironment aHostEnvironment)
  {
    // Enable middleware to serve generated Swagger as a JSON endpoint.
    aApplicationBuilder.UseSwagger();

    // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
    // specifying the Swagger JSON endpoint.
    aApplicationBuilder.UseSwaggerUI
    (
      aSwaggerUIOptions => aSwaggerUIOptions.SwaggerEndpoint(SwaggerEndPoint, SwaggerApiTitle)
    );

    aApplicationBuilder.UseResponseCompression();

    if (aHostEnvironment.IsDevelopment())
    {
      aApplicationBuilder.UseDeveloperExceptionPage();
      aApplicationBuilder.UseWebAssemblyDebugging();
    }

    aApplicationBuilder.UseRouting();
    //aApplicationBuilder.UseGrpcWeb(new GrpcWebOptions() { DefaultEnabled = true });
    aApplicationBuilder.UseEndpoints
    (
      aEndpointRouteBuilder =>
      {
        aEndpointRouteBuilder.MapHealthChecks("/api/health");
        //aEndpointRouteBuilder.MapGrpcService<SuperheroService>();
        //aEndpointRouteBuilder.MapCodeFirstGrpcReflectionService();
        aEndpointRouteBuilder.MapControllers();
        aEndpointRouteBuilder.MapBlazorHub();
        aEndpointRouteBuilder.MapFallbackToPage("/_Host");
      }
    );
    aApplicationBuilder.UseStaticFiles();
    aApplicationBuilder.UseBlazorFrameworkFiles();
  }

  private static void ConfigureSwagger(IServiceCollection aServiceCollection)
  {
    // Register the Swagger generator, defining 1 or more Swagger documents
    aServiceCollection.AddSwaggerGen
      (
        aSwaggerGenOptions =>
        {
          aSwaggerGenOptions
          .SwaggerDoc
          (
            SwaggerVersion,
            new OpenApiInfo { Title = SwaggerApiTitle, Version = SwaggerVersion }
          );
          aSwaggerGenOptions.EnableAnnotations();

          // Set the comments path for the Swagger JSON and UI from Server.
          string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
          string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
          aSwaggerGenOptions.IncludeXmlComments(xmlPath);

          // Set the comments path for the Swagger JSON and UI from API.
          xmlFile = $"{typeof(BaseRequest).Assembly.GetName().Name}.xml";
          xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
          aSwaggerGenOptions.IncludeXmlComments(xmlPath);
        }
      );

    aServiceCollection.AddFluentValidationRulesToSwagger();
  }

  public static void ConfigureEndpoints(IEndpointRouteBuilder aEndpointRouteBuilder, IServiceProvider aServiceCollection) =>
  aEndpointRouteBuilder.MapControllers();

  private static void ConfigureSettings(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {
    aServiceCollection
      .ConfigureOptions<CosmosDbOptions, CosmosDbOptionsValidator>(aConfiguration)
      .ConfigureOptions<SampleOptions, SampleOptionsValidator>(aConfiguration);
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
    //aServiceCollection.AddHostedService<ProtobufGenerationHostedService>();
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
            accountEndpoint: cosmosOptions.EndPoint,
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
