var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Api_Server>("api-server");

builder.AddProject<Projects.Yarp>("yarp");

builder.Build().Run();
