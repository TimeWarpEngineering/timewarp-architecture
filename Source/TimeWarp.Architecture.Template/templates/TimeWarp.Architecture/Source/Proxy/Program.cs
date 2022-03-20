WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
WebApplication webApplication = builder.Build();
webApplication.MapReverseProxy();
webApplication.Run();
