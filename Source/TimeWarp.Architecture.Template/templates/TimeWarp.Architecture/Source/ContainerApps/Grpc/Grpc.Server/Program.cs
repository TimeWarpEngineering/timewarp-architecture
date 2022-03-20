using ProtoBuf.Grpc.Server;
using TimeWarp.Architecture.Features.Superheros;
using TimeWarp.Architecture.GrpcServer.Services;
using TimeWarp.Architecture.HostedServices;

const string AllowAllCorsPolicy = "AllowAll";

WebApplicationBuilder? webApplicationBuilder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS,
// visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
ConfigureServices(webApplicationBuilder.Services);

WebApplication webApplication = webApplicationBuilder.Build();
ConfigurePipeline(webApplication);

webApplication.Run();

static void ConfigureServices(IServiceCollection aServiceCollection)
{
  //aServiceCollection.AddGrpc();
  //aServiceCollection.AddGrpcReflection();
  aServiceCollection.AddCodeFirstGrpc();
  aServiceCollection.AddCodeFirstGrpcReflection();

  
  aServiceCollection.AddCors
  (
    o => o.AddPolicy
    (
      AllowAllCorsPolicy, builder =>
      {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader()
               .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
      }
    )
  );
  
  //aServiceCollection.AddHostedService<ProtobufGenerationHostedService>();
}

static void ConfigurePipeline(WebApplication aWebApplication)
{
  aWebApplication.UseRouting();
  aWebApplication.UseGrpcWeb(new GrpcWebOptions() { DefaultEnabled = true });
  aWebApplication.UseCors();

  //aWebApplication.MapGrpcService<GreeterService>().RequireCors("AllowAll").EnableGrpcWeb();
  aWebApplication.MapGrpcService<SuperheroService>().RequireCors(AllowAllCorsPolicy);
  //aWebApplication.MapGrpcReflectionService();
  aWebApplication.MapCodeFirstGrpcReflectionService();
  aWebApplication.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
}
