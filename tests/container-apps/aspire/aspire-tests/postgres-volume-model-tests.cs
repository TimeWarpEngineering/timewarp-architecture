#region Purpose
// AppHost model inspection (task 266): the Postgres data volume keeps WithDataVolume's identity
// under WithVolume(env:), and Postgres:UseDataVolume=false still yields no volume.
#endregion

#region Design
// Model-only: DistributedApplicationTestingBuilder.CreateAsync runs the AppHost entry point up to
// Build and hands back the builder — nothing is built or started, so no Docker, no containers and
// no contact with the maintainer's real volume. Identity parity is checked against Aspire itself,
// not a hard-coded string: a throwaway probe resource gets WithDataVolume() on the same builder,
// and its generated name/target (with the probe's resource segment swapped for "postgres") is what
// the real resource must carry. An Aspire upgrade that moves WithDataVolume's target or naming
// scheme fails here instead of silently orphaning dev data.
#endregion

namespace Aspire.Tests;

[TestTag("Integration")]
public class PostgresVolumeModel_Given_
{
  private const string ProbeResourceName = "volume-probe";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<PostgresVolumeModel_Given_>();

  public static async Task DefaultConfig_Should_MatchWithDataVolumeIdentity()
  {
    await using IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>([]);

    ContainerMountAnnotation probe = appHost.AddPostgres(ProbeResourceName).WithDataVolume()
      .Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

    ContainerMountAnnotation mount = VolumeMounts(appHost).ShouldHaveSingleItem();
    mount.Source.ShouldBe(probe.Source!.Replace($"-{ProbeResourceName}-data", "-postgres-data", StringComparison.Ordinal));
    mount.Target.ShouldBe(probe.Target);
    mount.Target.ShouldBe("/var/lib/postgresql");
    mount.IsReadOnly.ShouldBeFalse();
  }

  public static async Task DefaultConfig_Should_ExposeMountPathEnvironmentVariable()
  {
    await using IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>([]);

    Dictionary<string, string> environment = await PostgresEnvironmentAsync(appHost);

    environment.ShouldContainKeyAndValue("POSTGRES_DATA_VOLUME", "/var/lib/postgresql");
    environment.ShouldNotContainKey("PGDATA");
  }

  public static async Task UseDataVolumeFalse_Should_HaveNoVolumeAndNoVariable()
  {
    await using IDistributedApplicationTestingBuilder appHost =
      await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_app_host>(
        ["--Postgres:UseDataVolume=false"]);

    VolumeMounts(appHost).ShouldBeEmpty();
    (await PostgresEnvironmentAsync(appHost)).ShouldNotContainKey("POSTGRES_DATA_VOLUME");
  }

  private static PostgresServerResource Postgres(IDistributedApplicationTestingBuilder appHost) =>
    appHost.Resources.OfType<PostgresServerResource>().Single(resource => resource.Name == "postgres");

  private static List<ContainerMountAnnotation> VolumeMounts(IDistributedApplicationTestingBuilder appHost) =>
    [.. Postgres(appHost).Annotations.OfType<ContainerMountAnnotation>().Where(mount => mount.Type == ContainerMountType.Volume)];

  private static async Task<Dictionary<string, string>> PostgresEnvironmentAsync(IDistributedApplicationTestingBuilder appHost)
  {
    PostgresServerResource postgres = Postgres(appHost);
    var context = new EnvironmentCallbackContext(
      new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run), postgres);
    foreach (EnvironmentCallbackAnnotation callback in postgres.Annotations.OfType<EnvironmentCallbackAnnotation>())
    {
      await callback.Callback(context);
    }

    return context.EnvironmentVariables.ToDictionary(pair => pair.Key, pair => pair.Value?.ToString() ?? "");
  }
}
