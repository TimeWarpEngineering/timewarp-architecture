namespace TimeWarp.Blazor.Server
{
  using Microsoft.AspNetCore.Hosting;

#if UseHttpSys
  using Microsoft.AspNetCore.Server.HttpSys;
#endif

  using Microsoft.Extensions.Hosting;

  public class Program
  {
    public static IHostBuilder CreateHostBuilder(string[] aArgumentArray) =>
      Host.CreateDefaultBuilder(aArgumentArray)
        .ConfigureWebHostDefaults
        (
          aWebHostBuilder =>
          {
#if UseHttpSys
            aWebHostBuilder.UseHttpSys
            (
              options =>
              {
                options.AllowSynchronousIO = false;
                options.Authentication.Schemes = AuthenticationSchemes.None;
                options.Authentication.AllowAnonymous = true;
                options.MaxConnections = null;
                options.MaxRequestBodySize = 30000000;
                options.UrlPrefixes.Add("http://localhost:5005");
              }
            );
#endif
            aWebHostBuilder.UseStartup<Startup>();
          }
        );

    public static void Main(string[] aArgumentArray) => CreateHostBuilder(aArgumentArray).Build().Run();
  }
}
