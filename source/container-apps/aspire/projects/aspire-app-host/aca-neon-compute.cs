#region Purpose
// Scale and size applied only to the aca-neon publish target's container apps.
#endregion

#region Design
// Task 317: Neon Free suspends compute after 5 minutes and that cannot be turned off, and the
// ACA consumption grant is spent by replicas that stay up. minReplicas 0 lets the app scale to
// zero; maxReplicas 1 and the consumption minimum (0.25 CPU, 0.5Gi) keep a cold start small.
// The callback runs after Aspire has built the container app, so it only overwrites scale and
// each container's CPU and memory. It publishes no HTTP probe. Aspire does not emit one for
// Compose, Kubernetes, or ACA, so nothing polls /alive. A probe that opened Postgres would keep
// Neon awake.
// CPU is stored as a double. Azure.Provisioning.BicepSyntax.Value formats it with the invariant
// culture when it writes Bicep (json('0.25')), so the process culture does not change the literal.
#endregion

namespace TimeWarp.Architecture.Aspire;

using Azure.Provisioning.AppContainers;

/// <summary>Consumption-plan scale for the <c>aca-neon</c> container apps.</summary>
internal static class AcaNeonCompute
{
  internal const int MinReplicas = 0;
  internal const int MaxReplicas = 1;
  internal const double CpuCores = 0.25;
  internal const string Memory = "0.5Gi";

  /// <summary>Longer than the default Postgres retry (4 tries, 2 seconds) so a Neon wake-up can finish.</summary>
  internal const string RetryCount = "6";

  /// <summary>Seconds. EF's maximum back-off delay. With <see cref="RetryCount"/>, covers a cold Neon compute start.</summary>
  internal const string RetryDelaySeconds = "8";

  /// <summary>Sets scale-to-zero and the small container size on one container app.</summary>
  internal static void Apply(ContainerApp app)
  {
    app.Template.Scale.MinReplicas = MinReplicas;
    app.Template.Scale.MaxReplicas = MaxReplicas;

    foreach (Azure.Provisioning.BicepValue<ContainerAppContainer> containerValue in app.Template.Containers)
    {
      if (containerValue.Value is not ContainerAppContainer container)
      {
        continue;
      }

      container.Resources.Cpu = CpuCores;
      container.Resources.Memory = Memory;
    }
  }
}
