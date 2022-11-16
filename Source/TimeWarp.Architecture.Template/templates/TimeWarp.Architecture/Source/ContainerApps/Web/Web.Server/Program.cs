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


    Web.Spa.Program.ConfigureServices(serviceCollection, configuration);
  }

  public static void ConfigureMiddleware(WebApplication webApplication)
  {
    CommonServerModule.ConfigureMiddleware(webApplication);

    if (webApplication.Environment.IsDevelopment())
    {
      webApplication.UseDeveloperExceptionPage();
      webApplication.UseWebAssemblyDebugging();
    }

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
