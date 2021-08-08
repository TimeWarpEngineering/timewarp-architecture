namespace TimeWarp.Blazor.Server
{
  using Microsoft.AspNetCore.Hosting;

#if UseHttpSys
  using Microsoft.AspNetCore.Server.HttpSys;
#endif

  using Microsoft.Extensions.Hosting;
  using TimeWarp.Blazor.Features.Hellos;

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
            aWebHostBuilder.UseStaticWebAssets();
            aWebHostBuilder.UseStartup<Startup>();
          }
        );

    public static void Main(string[] aArgumentArray)
    {
      var schemaGenerator = new ProtoBuf.Grpc.Reflection.SchemaGenerator
      {
        ProtoSyntax = ProtoBuf.Meta.ProtoSyntax.Proto3
      };
      string schema = schemaGenerator.GetSchema<IHelloService>();
      System.IO.Directory.CreateDirectory("protos");
      System.IO.File.WriteAllText("protos/helloservice.proto", schema);
      CreateHostBuilder(aArgumentArray).Build().Run();
    }
  }
}
