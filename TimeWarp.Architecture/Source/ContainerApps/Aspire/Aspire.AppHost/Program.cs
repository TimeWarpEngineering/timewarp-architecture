IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

#if cosmosdb
// Add CosmosDB resource
IResourceBuilder<AzureCosmosDBResource> cosmos = builder.AddAzureCosmosDB(CosmosDbResourceName);
IResourceBuilder<AzureCosmosDBResource> cosmosdb = cosmos.AddDatabase(CosmosDbDatabaseName);
//-:cnd:noEmit
#if DEBUG
cosmosdb.RunAsEmulator();
#endif
//+:cnd:noEmit
#endif
// Declare project resources based on template flags
#if api
// API Server is included in the template
IResourceBuilder<ProjectResource> apiServer = builder.AddProject<Projects.Api_Server>(ApiServerProjectResourceName).WithScalar();
#endif
#if grpc
// gRPC Server is included in the template
IResourceBuilder<ProjectResource> grpcServer = builder.AddProject<Projects.Grpc_Server>(GrpcServerProjectResourceName);
#endif
#if web
// Web Server is included in the template
IResourceBuilder<ProjectResource> webServer = builder.AddProject<Projects.Web_Server>(WebServerProjectResourceName)
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
bool isHttps = builder.Configuration["DOTNET_LAUNCH_PROFILE"] == "https";
int? ingressPort = int.TryParse(builder.Configuration["Ingress:Port"], out int port) ? port : null;

// Create the YARP resource
IResourceBuilder<YarpResource> yarp = builder.AddYarp(YarpResourceName)
  .WithEndpoint(scheme: isHttps ? "https" : "http", port: ingressPort);

// Add references to other services if they exist
#if api
yarp = yarp.WithReference(apiServer);
#endif
#if web
yarp = yarp.WithReference(webServer);
#endif
#if grpc
yarp = yarp.WithReference(grpcServer);
#endif

// Load configuration from ReverseProxy section
yarp = yarp.LoadFromConfiguration("ReverseProxy");
#endif

builder.Build().Run();
