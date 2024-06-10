IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<AzureCosmosDBResource> cosmos = builder.AddAzureCosmosDB(CosmosDbResourceName);
IResourceBuilder<AzureCosmosDBResource> cosmosdb = cosmos.AddDatabase(CosmosDbDatabaseName);
#if DEBUG
cosmosdb.RunAsEmulator();
#endif
IResourceBuilder<ProjectResource> apiServer =builder.AddProject<Projects.Api_Server>(ApiServerProjectResourceName);
IResourceBuilder<ProjectResource> grpcServer = builder.AddProject<Projects.Grpc_Server>(GrpcServerProjectResourceName);

IResourceBuilder<ProjectResource> webServer =
  builder
    .AddProject<Projects.Web_Server>(WebServerProjectResourceName)
    .WithExternalHttpEndpoints()
    .WithReference(cosmosdb)
    .WithReference(apiServer)
    .WithReference(grpcServer);

    webServer.WithReference(webServer);

builder.AddProject<Projects.Yarp>(YarpProjectResourceName)
  .WithReference(apiServer)
  .WithReference(webServer)
  .WithReference(grpcServer);

builder.Build().Run();
