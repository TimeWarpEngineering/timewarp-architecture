#region Purpose
// DI wiring for seed-on-read site settings: wrap the host's ISiteSettingsStore and add the boot seed.
#endregion

#region Design
// Call after every module that picks the store backend (InMemoryIdentityStoresModule, then
// PostgresDbModule's scoped EfSiteSettingsStore swap). The current unkeyed ISiteSettingsStore
// registration moves to the SeedOnReadSiteSettingsStore.InnerStoreKey key with its lifetime and
// shape (type / factory / instance) intact, so the in-memory singleton stays one instance.
// SiteSettingsSeeder takes the inner store — the boot retry must see 42P01, which the decorator
// turns into a null read. The decorator is scoped (EF inner is scoped; scoped over a singleton
// is fine). Test hosts replace the backend by registering a keyed InnerStoreKey store afterwards.
// Calling it twice throws: the second call would move the decorator itself to InnerStoreKey.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Features.Identity.Application;
using TimeWarp.Identity;

public static class SiteSettingsSeedRegistration
{
  public static void ConfigureServices(IServiceCollection serviceCollection)
  {
    if (serviceCollection.Any(IsInnerStore))
    {
      throw new InvalidOperationException(
        "SiteSettingsSeedRegistration.ConfigureServices was already called; a second call would wrap the decorator in itself.");
    }

    ServiceDescriptor current = serviceCollection.LastOrDefault(IsUnkeyedStore)
      ?? throw new InvalidOperationException(
        "Register an ISiteSettingsStore backend before SiteSettingsSeedRegistration.ConfigureServices.");
    foreach (ServiceDescriptor descriptor in serviceCollection.Where(IsUnkeyedStore).ToList())
    {
      serviceCollection.Remove(descriptor);
    }

    serviceCollection.Add(ToInnerStore(current));
    serviceCollection.AddScoped(serviceProvider => new SiteSettingsSeeder(
      serviceProvider.GetRequiredKeyedService<ISiteSettingsStore>(SeedOnReadSiteSettingsStore.InnerStoreKey),
      serviceProvider.GetRequiredService<IOptions<EntraAuthenticationOptions>>(),
      serviceProvider.GetRequiredService<ILogger<SiteSettingsSeeder>>()));
    serviceCollection.AddScoped<ISiteSettingsStore>(serviceProvider => new SeedOnReadSiteSettingsStore(
      serviceProvider.GetRequiredKeyedService<ISiteSettingsStore>(SeedOnReadSiteSettingsStore.InnerStoreKey),
      serviceProvider.GetRequiredService<SiteSettingsSeeder>(),
      serviceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment(),
      serviceProvider.GetRequiredService<ILogger<SeedOnReadSiteSettingsStore>>()));
    serviceCollection.AddHostedService<SiteSettingsSeedHostedService>();
  }

  private static bool IsUnkeyedStore(ServiceDescriptor descriptor) =>
    descriptor.ServiceType == typeof(ISiteSettingsStore) && !descriptor.IsKeyedService;

  private static bool IsInnerStore(ServiceDescriptor descriptor) =>
    descriptor.ServiceType == typeof(ISiteSettingsStore)
      && descriptor.IsKeyedService
      && Equals(descriptor.ServiceKey, SeedOnReadSiteSettingsStore.InnerStoreKey);

  private static ServiceDescriptor ToInnerStore(ServiceDescriptor current)
  {
    const string key = SeedOnReadSiteSettingsStore.InnerStoreKey;
    if (current.ImplementationInstance is { } instance)
    {
      return ServiceDescriptor.KeyedSingleton(typeof(ISiteSettingsStore), key, instance);
    }

    if (current.ImplementationFactory is { } factory)
    {
      return ServiceDescriptor.DescribeKeyed(
        typeof(ISiteSettingsStore),
        key,
        (serviceProvider, _) => factory(serviceProvider),
        current.Lifetime);
    }

    return ServiceDescriptor.DescribeKeyed(
      typeof(ISiteSettingsStore),
      key,
      current.ImplementationType
        ?? throw new InvalidOperationException("ISiteSettingsStore registration has no implementation."),
      current.Lifetime);
  }
}
