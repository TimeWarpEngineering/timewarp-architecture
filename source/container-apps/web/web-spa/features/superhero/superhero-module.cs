namespace TimeWarp.Architecture.Features.Superheros;

public class SuperheroModule
{
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    serviceCollection.AddScoped<ServiceUriProvider>();
    serviceCollection.AddScoped<SuperheroGrpcServiceProvider>();
  }
}
