IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Declare project resources based on template flags
// Web Server is included in the template
IResourceBuilder<ProjectResource> webServer = builder.AddProject<Projects.Web_Server>(WebServerProjectResourceName)
  .WithExternalHttpEndpoints();

// Add references to other services if they exist
webServer = webServer.WithReference(cosmosdb);
// Self-reference for the web server
webServer.WithReference(webServer);

// YARP Reverse Proxy
// YARP is included in the template
bool isHttps = builder.Configuration["DOTNET_LAUNCH_PROFILE"] == "https";
int? ingressPort = int.TryParse(builder.Configuration["Ingress:Port"], out int port) ? port : null;

// Create the YARP resource
IResourceBuilder<YarpResource> yarp = builder.AddYarp(YarpResourceName)
  .WithEndpoint(scheme: isHttps ? "https" : "http", port: ingressPort);

// Add references to other services if they exist
yarp = yarp.WithReference(webServer);

// Load configuration from ReverseProxy section
yarp = yarp.LoadFromConfiguration("ReverseProxy");

builder.Build().Run();
