namespace TimeWarp.Architecture.Web.Server;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
  private static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    serviceCollection.AddRazorPages();
    serviceCollection.AddServerSideBlazor();

    Pwa.Client.Program.ConfigureServices(serviceCollection, configuration);
  }

  private static void ConfigureMiddleware(WebApplication webApplication)
  {
    if (webApplication.Environment.IsDevelopment())
    {
      webApplication.UseWebAssemblyDebugging();
    }

    webApplication.UseBlazorFrameworkFiles();
    webApplication.UseStaticFiles();
    webApplication.UseRouting();
  }

  private static void ConfigureEndpoints(WebApplication webApplication)
  {
    webApplication.MapRazorPages();
    webApplication.MapControllers();
    webApplication.MapBlazorHub();
    webApplication.MapFallbackToPage("/_Host");
  }
}
