using TimeWarp.Architecture.GrpcServer.Services;

WebApplicationBuilder? webApplicationBuilder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS,
// visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
webApplicationBuilder.Services.AddGrpc();
webApplicationBuilder.Services.AddGrpcReflection();

WebApplication webApplication = webApplicationBuilder.Build();

// Configure the HTTP request pipeline.
webApplication.MapGrpcService<GreeterService>();
webApplication.MapGrpcReflectionService();
webApplication.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

webApplication.Run();
