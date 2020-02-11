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
            #region UseHttpSys
            // The default is kestrel
            #if UseHttpSys 
            aWebHostBuilder.UseHttpSys
            (
              aHttpSysOptions =>
              {
                aHttpSysOptions.AllowSynchronousIO = false;
                aHttpSysOptions.Authentication.Schemes = AuthenticationSchemes.None;
                aHttpSysOptions.Authentication.AllowAnonymous = true;
                aHttpSysOptions.MaxConnections = null;
                aHttpSysOptions.MaxRequestBodySize = 30000000;
                aHttpSysOptions.UrlPrefixes.Add("http://localhost:5005");
              }
            );
            #endif
            #endregion
            aWebHostBuilder.UseStartup<Startup>();
          }
        );

    public static void Main(string[] aArgumentArray) => CreateHostBuilder(aArgumentArray).Build().Run();
  }
}
