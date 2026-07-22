namespace TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;

using global::Aspire.Hosting;
using global::Aspire.Hosting.Testing;

class SpaTestConvention : TimeWarpTestingConvention
{
  public SpaTestConvention() : base(ConfigureServices) {}

  private static void ConfigureServices(ServiceCollection serviceCollection)
  {
    // Register the Aspire DistributedApplication
    serviceCollection.AddSingleton
    (
      async _ =>
      {
        IDistributedApplicationTestingBuilder appHost =
          await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>
          (
            // Ephemeral postgres: test AppHosts must NOT share the deterministic data volume
            // (overlapping instances corrupt its WAL and hang WaitFor - see AppHost Design region).
            ["--Postgres:UseDataVolume=false"]
          );

        DistributedApplication app = await appHost.BuildAsync();
        await app.StartAsync();
        return app;
      }
    );

    // Register the SpaTestApplication that uses the Aspire DistributedApplication
    serviceCollection.AddSingleton<ISpaTestApplication>(provider =>
    {
      Task<DistributedApplication> distributedAppTask = provider.GetRequiredService<Task<DistributedApplication>>();
      return new AspireSpaTestApplication(distributedAppTask);
    });
  }
}
