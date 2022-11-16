namespace TimeWarp.Architecture.Web.Server;

using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Mime;
using System.Reflection;
using TimeWarp.Architecture;

public class Program
{
  public static void Main(string[] args)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    ConfigureServices(builder.Services, builder.Configuration);

    WebApplication webApplication = builder.Build();

    Console.WriteLine($"EnvironmentName: {webApplication.Environment.EnvironmentName}");

    ConfigureMiddleware(webApplication);
    ConfigureEndpoints(webApplication);

    webApplication.Run();
  }
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {  
    serviceCollection.AddRazorPages();
    serviceCollection.AddServerSideBlazor();

    serviceCollection.Configure<ApiBehaviorOptions>
    (
      aApiBehaviorOptions => aApiBehaviorOptions.SuppressInferBindingSourcesForParameters = true
    );

    serviceCollection.AddResponseCompression
    (
      aResponseCompressionOptions =>
        aResponseCompressionOptions.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat
        (
          new[] { MediaTypeNames.Application.Octet }
        )
    );

    Web.Spa.Program.ConfigureServices(serviceCollection, configuration);
  }

  public static void ConfigureMiddleware(WebApplication webApplication)
  {
    CommonServerModule.ConfigureMiddleware(webApplication);

    // https://docs.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-6.0
    // CORS Is not a security feature, CORS relaxes security.An API is not safer by allowing CORS.
    // Although sometimes, you might want to allow other sites to make cross-origin requests to your app to be functional.
    if (webApplication.Environment.IsDevelopment())
    {
      webApplication.UseDeveloperExceptionPage();
      webApplication.UseWebAssemblyDebugging();
    }

    webApplication.UseResponseCompression();
    webApplication.UseBlazorFrameworkFiles();
    webApplication.UseStaticFiles();
    webApplication.UseRouting();
  }

  public static void ConfigureEndpoints(WebApplication webApplication)
  {
    webApplication.MapRazorPages();
    
    CommonServerModule.ConfigureEndpoints(webApplication);
    webApplication.MapControllers();
    webApplication.MapBlazorHub();
    webApplication.MapFallbackToPage("/_Host");
  }
}
