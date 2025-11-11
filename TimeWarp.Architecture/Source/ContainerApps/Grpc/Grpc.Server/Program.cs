public partial class Program
{
  private static void Main(string[] args)
  {
    const string AllowAllCorsPolicy = "AllowAll";

    WebApplicationBuilder? webApplicationBuilder = WebApplication.CreateBuilder(args);

    webApplicationBuilder.AddServiceDefaults();

    // Additional configuration is required to successfully run gRPC on macOS.
    // For instructions on how to configure Kestrel and gRPC clients on macOS,
    // visit https://go.microsoft.com/fwlink/?linkid=2099682

    // Add services to the container.
    ConfigureServices(webApplicationBuilder.Services);

    WebApplication webApplication = webApplicationBuilder.Build();

    webApplication.MapDefaultEndpoints();
    ConfigurePipeline(webApplication);

    webApplication.Run();

    static void ConfigureServices(IServiceCollection serviceCollection)
    {
      //serviceCollection.AddGrpc();
      //serviceCollection.AddGrpcReflection();
      serviceCollection.AddCodeFirstGrpc();
      serviceCollection.AddCodeFirstGrpcReflection();

      serviceCollection.AddCors
      (
        o => o.AddPolicy
        (
          AllowAllCorsPolicy, builder =>
            builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding"))
      );

      //serviceCollection.AddHostedService<ProtobufGenerationHostedService>();
    }

    static void ConfigurePipeline(WebApplication webApplication)
    {
      webApplication.UseRouting();
      webApplication.UseGrpcWeb(new GrpcWebOptions() { DefaultEnabled = true });
      webApplication.UseCors();

      //webApplication.MapGrpcService<GreeterService>().RequireCors("AllowAll").EnableGrpcWeb();
      webApplication.MapGrpcService<SuperheroService>().RequireCors(AllowAllCorsPolicy);
      //webApplication.MapGrpcReflectionService();
      webApplication.MapCodeFirstGrpcReflectionService();
      webApplication.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
    }
  }
}