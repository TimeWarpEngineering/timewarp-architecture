IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<AzureCosmosDBResource> cosmos = builder.AddAzureCosmosDB(CosmosDbResourceName);
IResourceBuilder<AzureCosmosDBResource> cosmosdb = cosmos.AddDatabase(CosmosDbDatabaseName);
#if DEBUG
cosmosdb.RunAsEmulator();
#endif
IResourceBuilder<ProjectResource> apiServer = builder.AddProject<Projects.Api_Server>(ApiServerProjectResourceName).WithScalar();
IResourceBuilder<ProjectResource> grpcServer = builder.AddProject<Projects.Grpc_Server>(GrpcServerProjectResourceName);

IResourceBuilder<ProjectResource> webServer =
  builder
    .AddProject<Projects.Web_Server>(WebServerProjectResourceName)
    .WithExternalHttpEndpoints()
    .WithReference(cosmosdb)
    .WithReference(apiServer)
    .WithReference(grpcServer);

webServer.WithReference(webServer);

// builder.AddProject<Projects.Yarp>(YarpProjectResourceName)
//   .WithReference(apiServer)
//   .WithReference(webServer)
//   .WithReference(grpcServer);

// Using Configuration File
// builder.AddYarp(YarpResourceName)
//   .WithHttpEndpoint(port: 8001)
//   .WithReference(apiServer)
//   .WithReference(webServer)
//   .WithReference(grpcServer)
//   .LoadFromConfiguration("ReverseProxy");

bool isHttps = builder.Configuration["DOTNET_LAUNCH_PROFILE"] == "https";
int? ingressPort = int.TryParse(builder.Configuration["Ingress:Port"], out int port) ? port : null;

// builder.AddYarp("ingress")
//   .WithEndpoint(scheme: isHttps ? "https" : "http", port: ingressPort)
//   .WithReference(app1)
//   .WithReference(app2)
//   .LoadFromConfiguration("ReverseProxy");

// Using Code based routes
IResourceBuilder<YarpResource> yarp = builder.AddYarp(YarpResourceName)
  .WithEndpoint(scheme: isHttps ? "https" : "http", port: ingressPort)
  .WithReference(apiServer)
  .WithReference(webServer)
  .WithReference(grpcServer)
  .LoadFromConfiguration("ReverseProxy");
   // .WithHttpEndpoint(port: 8001)
  // .WithHttpsEndpoint(port: 8002)
  // .WithEnvironment(name: "ASPNETCORE_ENVIRONMENT", value: "Development")
  // .Route(routeId: "spa", target: webServer, path: "/{**catch-all}")
  // .Route(routeId: "api-server-api", target: apiServer, path: "/api/{**catch-all}")
  // .Route(routeId: "web-server-api", target: webServer, path: "/api/web-server/{**catch-all}")
  // .Route(routeId: "grpc", target: grpcServer, path: "/grpc/{**catch-all}", preservePath: false);

builder.Build().Run();
