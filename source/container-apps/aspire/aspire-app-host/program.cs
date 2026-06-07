namespace TimeWarp.Architecture.Aspire;

internal class Program
{
  private static void Main(string[] args)
  {
    IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

#if cosmosdb
    // Add CosmosDB resource
    IResourceBuilder<AzureCosmosDBResource> cosmos = builder.AddAzureCosmosDB(CosmosDbResourceName);
    //-:cnd:noEmit
#if DEBUG
    cosmos = cosmos.RunAsEmulator();
#endif
    //+:cnd:noEmit
    IResourceBuilder<global::Aspire.Hosting.Azure.AzureCosmosDBDatabaseResource> cosmosdb = cosmos.AddCosmosDatabase(CosmosDbDatabaseName);
#endif
    // Declare project resources based on template flags
#if api
    // API Server is included in the template
    IResourceBuilder<ProjectResource> apiServer = builder
      .AddProject<Projects.api_server>(ApiServerProjectResourceName, options => options.LaunchProfileName = "Api.Server")
      .WithScalar();
#endif
#if grpc
    // gRPC Server is included in the template
    IResourceBuilder<ProjectResource> grpcServer = builder.AddProject<Projects.grpc_server>(GrpcServerProjectResourceName, options => options.LaunchProfileName = "Grpc.Server");
#endif
#if web
    // Web Server is included in the template
    IResourceBuilder<ProjectResource> webServer = builder.AddProject<Projects.web_server>(WebServerProjectResourceName, options => options.LaunchProfileName = "Web.Server")
      .WithExternalHttpEndpoints();

    // Add references to other services if they exist
#if cosmosdb
    webServer = webServer.WithReference(cosmosdb);
#endif
#if api
    webServer = webServer.WithReference(apiServer);
#endif
#if grpc
    webServer = webServer.WithReference(grpcServer);
#endif
    // Self-reference for the web server
    webServer.WithReference(webServer);
#endif

#if yarp
    // YARP Reverse Proxy
    // YARP is included in the template
    int? ingressPort = int.TryParse(builder.Configuration["Ingress:Port"], out int port) ? port : null;

    // Create the YARP resource
    IResourceBuilder<YarpResource> yarp = builder.AddYarp(YarpResourceName);

    if (ingressPort is not null)
    {
      yarp = yarp.WithHostPort(ingressPort.Value);
    }

    yarp = yarp.WithConfiguration(yarpConfiguration =>
    {
#if api
      yarpConfiguration.AddRoute("/api/{**catch-all}", apiServer);
#endif
#if grpc
      yarpConfiguration.AddRoute("/grpc/{**catch-all}", grpcServer)
        .WithTransformPathRemovePrefix("/grpc");
#endif
#if web
      yarpConfiguration.AddRoute(webServer);
#endif
    });
#endif

    builder.Build().Run();
  }
}

#if cosmosdb

#if DEBUG

#endif
#endif
#if api

#endif
#if grpc

#endif
#if web

#if cosmosdb

#endif
#if api

#endif
#if grpc

#endif

#endif
#if yarp

#if api

#endif
#if web

#endif
#if grpc

#endif

#endif
