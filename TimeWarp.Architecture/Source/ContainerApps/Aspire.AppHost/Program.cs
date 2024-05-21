var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Api_Server>("api-server");

builder.Build().Run();
