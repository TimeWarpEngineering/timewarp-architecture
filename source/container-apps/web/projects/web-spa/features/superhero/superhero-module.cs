#region Purpose
// DI registration for the superhero feature: the service-URI resolver and the gRPC client provider its state actions depend on.
#endregion

namespace TimeWarp.Architecture.Features.Superheros;

public class SuperheroModule
{
  public static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    serviceCollection.AddScoped<ServiceUriProvider>();
    serviceCollection.AddScoped<SuperheroGrpcServiceProvider>();
  }
}
