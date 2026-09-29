#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:package Microsoft.Extensions.Logging.Abstractions
#:package Microsoft.Extensions.Options
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0007;IDE0008

// Host-free SeedOnReadSiteSettingsStore coverage (task 254).
// Run standalone:  dotnet run source/container-apps/web/features/identity/seed-on-read-site-settings-store-tests.cs

#region Purpose
// Jaribu runfile: empty read seeds once, emptied store re-seeds, concurrent reads race to one row, 42P01 reads null, isDevelopment reaches reseed.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Identity.SeedOnReadSiteSettingsStoreTests
{

  using System;
  using System.Collections.Generic;
  using System.Threading;
  using System.Threading.Tasks;
  using Microsoft.Extensions.Logging;
  using Microsoft.Extensions.Logging.Abstractions;
  using Microsoft.Extensions.Options;
  using Shouldly;
  using TimeWarp.Architecture.Features.Identity.Application;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Application")]
  public class SeedOnReadSiteSettingsStore_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<SeedOnReadSiteSettingsStore_Given_>();

    public static async Task Empty_Store_Read_Should_Seed_From_Configuration()
    {
      InMemorySiteSettingsStore inner = new();
      SeedOnReadSiteSettingsStore store = Create(inner, enabled: true, allowBootstrap: true);

      SiteSettings? read = await store.GetAsync();

      read.ShouldNotBeNull();
      read.EntraSignInEnabled.ShouldBeTrue();
      read.EntraAllowBootstrap.ShouldBeTrue();
      read.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Soft);
      (await inner.GetAsync()).ShouldNotBeNull();
    }

    public static async Task Existing_Row_Should_Be_Returned_Unchanged()
    {
      InMemorySiteSettingsStore inner = new();
      await inner.AddAsync(SiteSettings.Create(false, false, PasskeyPromptMode.Required));
      SeedOnReadSiteSettingsStore store = Create(inner, enabled: true, allowBootstrap: true);

      SiteSettings? read = await store.GetAsync();

      read.ShouldNotBeNull();
      read.EntraSignInEnabled.ShouldBeFalse();
      read.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Required);
    }

    public static async Task Emptied_Store_Should_Reseed_On_Next_Read()
    {
      ResettableSiteSettingsStore inner = new();
      await inner.AddAsync(SiteSettings.Create(false, false, PasskeyPromptMode.Required));
      SeedOnReadSiteSettingsStore store = Create(inner, enabled: true, allowBootstrap: false);

      inner.Clear();
      SiteSettings? read = await store.GetAsync();

      read.ShouldNotBeNull();
      read.EntraSignInEnabled.ShouldBeTrue();
      read.EntraAllowBootstrap.ShouldBeFalse();
      read.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Soft);
      read.Version.ShouldBe(0);
    }

    public static async Task Concurrent_Empty_Reads_Should_Produce_One_Row()
    {
      AddRaceSiteSettingsStore inner = new(expectedAdds: 2);
      SeedOnReadSiteSettingsStore first = Create(inner, enabled: true, allowBootstrap: true);
      SeedOnReadSiteSettingsStore second = Create(inner, enabled: true, allowBootstrap: true);

      SiteSettings?[] reads = await Task.WhenAll(
        Task.Run(() => first.GetAsync()),
        Task.Run(() => second.GetAsync()));

      inner.AddAttempts.ShouldBe(2);
      inner.RejectedAdds.ShouldBe(1);
      reads[0].ShouldNotBeNull();
      reads[1].ShouldNotBeNull();
      reads[0]!.Id.ShouldBe(reads[1]!.Id);
      reads[0]!.Version.ShouldBe(reads[1]!.Version);
      reads[0]!.EntraSignInEnabled.ShouldBeTrue();
      reads[1]!.EntraSignInEnabled.ShouldBeTrue();
    }

    public static async Task Concurrent_Empty_Reads_With_Development_Reseed_Should_Not_Conflict()
    {
      AddRaceSiteSettingsStore inner = new(expectedAdds: 2);
      SeedOnReadSiteSettingsStore first = Create(
        inner, enabled: true, allowBootstrap: true, reseedSiteSettings: true, isDevelopment: true);
      SeedOnReadSiteSettingsStore second = Create(
        inner, enabled: true, allowBootstrap: true, reseedSiteSettings: true, isDevelopment: true);

      SiteSettings?[] reads = await Task.WhenAll(
        Task.Run(() => first.GetAsync()),
        Task.Run(() => second.GetAsync()));

      reads[0].ShouldNotBeNull();
      reads[1].ShouldNotBeNull();
      reads[0]!.EntraSignInEnabled.ShouldBeTrue();
      reads[1]!.EntraAllowBootstrap.ShouldBeTrue();
      (await inner.GetAsync()).ShouldNotBeNull();
    }

    public static async Task Development_Reseed_Losing_Update_Race_Should_Reget_Instead_Of_Throwing()
    {
      ConflictOnceUpdateSiteSettingsStore inner = new();
      SeedOnReadSiteSettingsStore store = Create(
        inner, enabled: true, allowBootstrap: true, reseedSiteSettings: true, isDevelopment: true);

      SiteSettings? read = await store.GetAsync();

      inner.Conflicts.ShouldBe(1);
      read.ShouldNotBeNull();
      read.EntraSignInEnabled.ShouldBeTrue();
      read.EntraAllowBootstrap.ShouldBeTrue();
    }

    public static async Task Undefined_Table_Should_Read_Null_And_Warn()
    {
      ResettableSiteSettingsStore inner = new() { ThrowUndefinedTable = true };
      CapturingLogger<SeedOnReadSiteSettingsStore> logger = new();
      SeedOnReadSiteSettingsStore store = Create(inner, enabled: true, allowBootstrap: true, storeLogger: logger);

      SiteSettings? read = await store.GetAsync();

      read.ShouldBeNull();
      logger.Levels.ShouldContain(LogLevel.Warning);
      logger.Messages.ShouldContain(message => message.Contains("identity.site_settings", StringComparison.Ordinal));
    }

    public static async Task Other_Failures_Should_Propagate()
    {
      ResettableSiteSettingsStore inner = new() { ThrowOther = true };
      SeedOnReadSiteSettingsStore store = Create(inner, enabled: true, allowBootstrap: true);

      await Should.ThrowAsync<InvalidOperationException>(() => store.GetAsync());
    }

    public static async Task Development_Reseed_Flag_Should_Reach_Lazy_Seed()
    {
      InMemorySiteSettingsStore inner = new();
      CapturingLogger<SiteSettingsSeeder> seederLogger = new();
      SeedOnReadSiteSettingsStore store = Create(
        inner,
        enabled: true,
        allowBootstrap: true,
        reseedSiteSettings: true,
        isDevelopment: true,
        seederLogger: seederLogger);

      SiteSettings? read = await store.GetAsync();

      read.ShouldNotBeNull();
      read.Version.ShouldBe(1);
      seederLogger.Messages.ShouldContain(message => message.Contains("Overwrote EntraSignInEnabled", StringComparison.Ordinal));
    }

    public static async Task Non_Development_Reseed_Flag_Should_Be_Ignored_On_Lazy_Seed()
    {
      InMemorySiteSettingsStore inner = new();
      CapturingLogger<SiteSettingsSeeder> seederLogger = new();
      SeedOnReadSiteSettingsStore store = Create(
        inner,
        enabled: true,
        allowBootstrap: true,
        reseedSiteSettings: true,
        isDevelopment: false,
        seederLogger: seederLogger);

      SiteSettings? read = await store.GetAsync();

      read.ShouldNotBeNull();
      read.Version.ShouldBe(0);
      seederLogger.Messages.ShouldContain(message => message.Contains("honoured only in Development", StringComparison.Ordinal));
    }

    public static async Task Update_Should_Pass_Through_To_Inner()
    {
      InMemorySiteSettingsStore inner = new();
      SeedOnReadSiteSettingsStore store = Create(inner, enabled: false, allowBootstrap: false);
      SiteSettings seeded = (await store.GetAsync())!;

      seeded.ReplacePolicy(true, true, PasskeyPromptMode.Required);
      await store.UpdateAsync(seeded);

      SiteSettings? stored = await inner.GetAsync();
      stored.ShouldNotBeNull();
      stored.EntraSignInEnabled.ShouldBeTrue();
      stored.Version.ShouldBe(1);
    }

    private static SeedOnReadSiteSettingsStore Create(
      ISiteSettingsStore inner,
      bool enabled,
      bool allowBootstrap,
      bool reseedSiteSettings = false,
      bool isDevelopment = false,
      ILogger<SiteSettingsSeeder>? seederLogger = null,
      ILogger<SeedOnReadSiteSettingsStore>? storeLogger = null)
    {
      IOptions<EntraAuthenticationOptions> options = Options.Create(
        new EntraAuthenticationOptions
        {
          Enabled = enabled,
          AllowBootstrap = allowBootstrap,
          ReseedSiteSettings = reseedSiteSettings
        });
      SiteSettingsSeeder seeder = new(inner, options, seederLogger ?? NullLogger<SiteSettingsSeeder>.Instance);
      return new SeedOnReadSiteSettingsStore(
        inner,
        seeder,
        isDevelopment,
        storeLogger ?? NullLogger<SeedOnReadSiteSettingsStore>.Instance);
    }
  }

  // Wraps InMemorySiteSettingsStore so a test can empty it (the `dev db reset` shape) or make reads
  // fail like an unmigrated Postgres table.
  internal sealed class ResettableSiteSettingsStore : ISiteSettingsStore
  {
    private InMemorySiteSettingsStore Current = new();

    public bool ThrowUndefinedTable { get; set; }
    public bool ThrowOther { get; set; }

    public void Clear() => Current = new InMemorySiteSettingsStore();

    public Task<SiteSettings?> GetAsync(CancellationToken cancellationToken = default)
    {
      if (ThrowUndefinedTable)
      {
        throw new InvalidOperationException(
          "Npgsql.PostgresException: 42P01: relation \"identity.site_settings\" does not exist");
      }

      if (ThrowOther)
      {
        throw new InvalidOperationException("Connection refused.");
      }

      return Current.GetAsync(cancellationToken);
    }

    public Task AddAsync(SiteSettings siteSettings, CancellationToken cancellationToken = default) =>
      Current.AddAsync(siteSettings, cancellationToken);

    public Task UpdateAsync(SiteSettings siteSettings, CancellationToken cancellationToken = default) =>
      Current.UpdateAsync(siteSettings, cancellationToken);
  }

  // Holds every AddAsync until expectedAdds callers have arrived, so concurrent empty reads are
  // forced to both attempt the insert — the race the seeder's re-Get has to resolve.
  internal sealed class AddRaceSiteSettingsStore : ISiteSettingsStore
  {
    private readonly InMemorySiteSettingsStore Inner = new();
    private readonly int ExpectedAdds;
    private readonly TaskCompletionSource AllArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int Attempts;
    private int Rejected;

    public AddRaceSiteSettingsStore(int expectedAdds)
    {
      ExpectedAdds = expectedAdds;
    }

    public int AddAttempts => Volatile.Read(ref Attempts);
    public int RejectedAdds => Volatile.Read(ref Rejected);

    public Task<SiteSettings?> GetAsync(CancellationToken cancellationToken = default) =>
      Inner.GetAsync(cancellationToken);

    public async Task AddAsync(SiteSettings siteSettings, CancellationToken cancellationToken = default)
    {
      if (Interlocked.Increment(ref Attempts) >= ExpectedAdds)
      {
        AllArrived.TrySetResult();
      }

      await AllArrived.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
      try
      {
        await Inner.AddAsync(siteSettings, cancellationToken);
      }
      catch (InvalidOperationException)
      {
        Interlocked.Increment(ref Rejected);
        throw;
      }
    }

    public Task UpdateAsync(SiteSettings siteSettings, CancellationToken cancellationToken = default) =>
      Inner.UpdateAsync(siteSettings, cancellationToken);
  }

  // First UpdateAsync throws ConcurrencyConflictException — the reseed losing to a concurrent
  // seeder that already bumped the row — so the seeder's conflict catch runs deterministically.
  internal sealed class ConflictOnceUpdateSiteSettingsStore : ISiteSettingsStore
  {
    private readonly InMemorySiteSettingsStore Inner = new();

    public int Conflicts { get; private set; }

    public Task<SiteSettings?> GetAsync(CancellationToken cancellationToken = default) =>
      Inner.GetAsync(cancellationToken);

    public Task AddAsync(SiteSettings siteSettings, CancellationToken cancellationToken = default) =>
      Inner.AddAsync(siteSettings, cancellationToken);

    public Task UpdateAsync(SiteSettings siteSettings, CancellationToken cancellationToken = default)
    {
      if (Conflicts == 0)
      {
        Conflicts++;
        throw new ConcurrencyConflictException(
          typeof(SiteSettings), siteSettings.Id.ToString(), siteSettings.Version, siteSettings.Version + 1);
      }

      return Inner.UpdateAsync(siteSettings, cancellationToken);
    }
  }

  internal sealed class CapturingLogger<T> : ILogger<T>
  {
    public List<string> Messages { get; } = [];
    public List<LogLevel> Levels { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
      LogLevel logLevel,
      EventId eventId,
      TState state,
      Exception? exception,
      Func<TState, Exception?, string> formatter)
    {
      lock (Messages)
      {
        Levels.Add(logLevel);
        Messages.Add(formatter(state, exception));
      }
    }
  }
}
