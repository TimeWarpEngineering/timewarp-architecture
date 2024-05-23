IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ProjectResource> apiServer =builder.AddProject<Projects.Api_Server>("api-server");
IResourceBuilder<ProjectResource> grpcServer = builder.AddProject<Projects.Grpc_Server>("grpc-server");

IResourceBuilder<ProjectResource> webServer =
  builder
    .AddProject<Projects.Web_Server>("web-server")
    .WithExternalHttpEndpoints()
    .WithReference(apiServer)
    .WithReference(grpcServer);

    webServer.WithReference(webServer);

builder.AddProject<Projects.Yarp>("yarp")
  .WithReference(apiServer)
  .WithReference(webServer)
  .WithReference(grpcServer);

builder.Build().Run();
