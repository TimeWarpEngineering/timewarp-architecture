namespace TimeWarp.Architecture.Web.Server;

using FluentValidation;
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
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.CorsPolicies;
using TimeWarp.Architecture.Data;
using TimeWarp.Architecture.HostedServices;
using TimeWarp.Architecture.Infrastructure;
using TimeWarp.Architecture.Web.Infrastructure;

public class Program : IAspNetProgram
{
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
      .TryAddApplicationPart(typeof(Web_Server_Assembly).Assembly);

    aServiceCollection.AddFluentValidationAutoValidation();
    aServiceCollection.AddFluentValidationClientsideAdapters();

    // AddValidatorsFromAssemblyContaining will register all public Validators as scoped but
    // will NOT register internals. This feature is utilized.
    aServiceCollection.AddValidatorsFromAssemblyContaining<Web_Server_Assembly>();
    aServiceCollection.AddValidatorsFromAssemblyContaining<Web_Contracts_Assembly>();

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

    aWebApplication.UseResponseCompression();
    aWebApplication.UseBlazorFrameworkFiles();
    aWebApplication.UseStaticFiles();
    aWebApplication.UseRouting();
  }

  public static void ConfigureEndpoints(WebApplication aWebApplication)
  {
    aWebApplication.MapRazorPages();
    
    CommonServerModule.ConfigureEndpoints(aWebApplication);
    aWebApplication.MapControllers();
    aWebApplication.MapBlazorHub();
    aWebApplication.MapFallbackToPage("/_Host");
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
    ConfigureEnvironmentChecks(aServiceCollection);
    //ConfigureSqlDb(aServiceCollection, Configuration);
    aServiceCollection.AddHostedService<StartupHostedService>();
  }

  private static void ConfigureEnvironmentChecks(IServiceCollection aServiceCollection)
  {
    aServiceCollection.AddSingleton<SampleEnvironmentCheck>();
    
    aServiceCollection.CheckEnvironment<SampleEnvironmentCheck>
    (
      SampleEnvironmentCheck.Description, aSampleEnvironmentCheck => aSampleEnvironmentCheck.Check()
    );
  }
}
