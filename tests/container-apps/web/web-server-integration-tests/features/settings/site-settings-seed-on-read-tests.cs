#region Purpose
// HTTP proof that an emptied site-settings store re-seeds on the next read and boot still logs drift.
#endregion

#region Design
// Task 254: stands in for `dev db reset` while web-server keeps running. The test host registers a
// keyed SeedOnReadSiteSettingsStore.InnerStoreKey store it can empty, so the production decorator
// and hosted service run unchanged over it. The store starts with EntraAllowBootstrap true against
// configuration false, so the boot seed must log the AllowBootstrap drift warning. Options are
// pinned with PostConfigure so developer user secrets cannot change the expected seed values.
#endregion

namespace SiteSettingsSeedOnRead_;

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Features.Settings;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure.CredentialCeremonyHelpers;

public class Given_Emptied_Store_
{
  private static readonly EmptiableSiteSettingsStore Store = new();
  private static readonly CapturingLoggerProvider Logs = new();
  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Given_Emptied_Store_>();

  public static async Task SetupOnce()
  {
    await Store.AddAsync(SiteSettings.Create(false, true, PasskeyPromptMode.Required));
#if(api)
    Graph = await HostGraphFactory.CreateWebWithApiAsync(configureWeb: ConfigureWeb);
#else
    Graph = await HostGraphFactory.CreateWebAsync(ConfigureWeb);
#endif
  }

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  public static Task Boot_Should_Log_AllowBootstrap_Drift()
  {
    Logs.Messages.ShouldContain(message =>
      message.Contains("EntraAllowBootstrap is True", StringComparison.Ordinal)
      && message.Contains("dev entra reseed", StringComparison.Ordinal));
    return Task.CompletedTask;
  }

  public static async Task Settings_Get_Should_Return_Seeded_Values_Not_503()
  {
    (PrincipalId principalId, string sessionCookie) = await RegisterPasskeyAndMintSessionAsync(Web);
    await using (AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope())
    {
      IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
      await roleStore.SetRoleIdsAsync(principalId, [RoleIds.Member]);
    }

    Store.Clear();
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    HttpResponseMessage response = await client.GetAsync("api/settings");

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    GetSiteSettings.Response settings =
      JsonSerializer.Deserialize<GetSiteSettings.Response>(
        await response.Content.ReadAsStringAsync(),
        ContractSerializationDefaults.Options)
      ?? throw new InvalidOperationException("GetSiteSettings deserialized to null.");
    settings.EntraSignInEnabled.ShouldBeFalse();
    settings.EntraAllowBootstrap.ShouldBeFalse();
    settings.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Soft);
    settings.Version.ShouldBe(0);
  }

  public static async Task Offered_Should_Reseed_The_Deleted_Row()
  {
    Store.Clear();
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    HttpResponseMessage response = await client.GetAsync("api/identity/entra/offered");

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    GetEntraSignInOffered.Response? offered =
      JsonSerializer.Deserialize<GetEntraSignInOffered.Response>(
        await response.Content.ReadAsStringAsync(),
        ContractSerializationDefaults.Options);
    offered.ShouldNotBeNull();
    offered.Offered.ShouldBeFalse();
    SiteSettings? seeded = await Store.GetAsync();
    seeded.ShouldNotBeNull();
    seeded.EntraAllowBootstrap.ShouldBeFalse();
    seeded.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Soft);
  }

  private static void ConfigureWeb(IServiceCollection services)
  {
    services.AddKeyedSingleton<ISiteSettingsStore>(SeedOnReadSiteSettingsStore.InnerStoreKey, Store);
    services.PostConfigure<EntraAuthenticationOptions>(options =>
    {
      options.Enabled = false;
      options.AllowBootstrap = false;
      options.ReseedSiteSettings = false;
    });
    services.AddSingleton<ILoggerProvider>(Logs);
  }

  private sealed class EmptiableSiteSettingsStore : ISiteSettingsStore
  {
    private InMemorySiteSettingsStore Current = new();

    public void Clear() => Current = new InMemorySiteSettingsStore();

    public Task<SiteSettings?> GetAsync(CancellationToken cancellationToken = default) =>
      Current.GetAsync(cancellationToken);

    public Task AddAsync(SiteSettings siteSettings, CancellationToken cancellationToken = default) =>
      Current.AddAsync(siteSettings, cancellationToken);

    public Task UpdateAsync(SiteSettings siteSettings, CancellationToken cancellationToken = default) =>
      Current.UpdateAsync(siteSettings, cancellationToken);
  }

  private sealed class CapturingLoggerProvider : ILoggerProvider
  {
    private readonly List<string> Captured = [];

    public IReadOnlyList<string> Messages
    {
      get
      {
        lock (Captured)
        {
          return [.. Captured];
        }
      }
    }

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

    public void Dispose() { }

    private sealed class CapturingLogger(CapturingLoggerProvider owner) : ILogger
    {
      public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

      public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

      public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
      {
        if (!IsEnabled(logLevel))
        {
          return;
        }

        lock (owner.Captured)
        {
          owner.Captured.Add(formatter(state, exception));
        }
      }
    }
  }
}
