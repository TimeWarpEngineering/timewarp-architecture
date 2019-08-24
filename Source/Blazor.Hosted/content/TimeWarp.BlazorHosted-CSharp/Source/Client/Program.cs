namespace BlazorHosted_CSharp.Client
{
  using Microsoft.AspNetCore.Blazor.Hosting;

  public class Program
  {
    private static IWebAssemblyHostBuilder CreateHostBuilder() =>
        BlazorWebAssemblyHost.CreateDefaultBuilder()
        .UseBlazorStartup<Startup>();

    private static void Main() => CreateHostBuilder().Build().Run();
  }
}