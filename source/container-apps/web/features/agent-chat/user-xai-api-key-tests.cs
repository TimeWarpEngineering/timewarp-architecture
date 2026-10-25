#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0007;IDE0008

#region Purpose
// Per-user xAI API key: policy ladder, isolation, validation, and a log line that omits the secret.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.AgentChats
{

  using Microsoft.Extensions.FileProviders;
  using Microsoft.Extensions.Hosting;
  using Microsoft.Extensions.Logging;
  using Microsoft.Extensions.Options;
  using OneOf;
  using Shouldly;
  using TimeWarp.Architecture.Abstractions;
  using TimeWarp.Architecture.Features.AgentChats.Application;
  using TimeWarp.Architecture.Secrets;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Unit")]
  public class UserXaiApiKey_Given_
  {
    private const string UserSecret = "xai-user-key-399-aaaa";
    private const string OtherSecret = "xai-user-key-399-bbbb";
    private const string SiteSecret = "xai-site-key-399-ssss";
    private const string HostSecret = "xai-host-key-399-hhhh";
    private static readonly Guid UserA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<UserXaiApiKey_Given_>();

    public static async Task Host_Wins_Even_When_Required_And_Both_Other_Keys_Exist()
    {
      Harness harness = await Harness.OpenAsync(UserA, HostSecret, XaiByokMode.Required);
      await harness.StoreUserAsync(UserA, UserSecret);
      await harness.StoreSiteAsync(SiteSecret);

      ResolvedXaiApiKey resolved = await harness.Resolver.ResolveAsync(CancellationToken.None);

      resolved.ApiKey.ShouldBe(HostSecret);
      resolved.Origin.ShouldBe(XaiApiKeyOrigin.Host);
      resolved.Mode.ShouldBe(XaiByokMode.Required);
    }

    public static async Task Required_Ignores_The_Site_Key()
    {
      Harness harness = await Harness.OpenAsync(UserA, hostKey: null, XaiByokMode.Required);
      await harness.StoreSiteAsync(SiteSecret);

      ResolvedXaiApiKey resolved = await harness.Resolver.ResolveAsync(CancellationToken.None);

      resolved.ApiKey.ShouldBeNull();
      resolved.Origin.ShouldBe(XaiApiKeyOrigin.None);
    }

    public static async Task Allow_Uses_The_User_Key_Then_The_Site_Key()
    {
      Harness harness = await Harness.OpenAsync(UserA, hostKey: null, XaiByokMode.Allow);
      await harness.StoreSiteAsync(SiteSecret);
      ResolvedXaiApiKey site = await harness.Resolver.ResolveAsync(CancellationToken.None);
      site.ApiKey.ShouldBe(SiteSecret);
      site.Origin.ShouldBe(XaiApiKeyOrigin.Site);

      await harness.StoreUserAsync(UserA, UserSecret);
      ResolvedXaiApiKey user = await harness.Resolver.ResolveAsync(CancellationToken.None);
      user.ApiKey.ShouldBe(UserSecret);
      user.Origin.ShouldBe(XaiApiKeyOrigin.User);
    }

    public static async Task Off_Ignores_The_User_Key()
    {
      Harness harness = await Harness.OpenAsync(UserA, hostKey: null, XaiByokMode.Off);
      await harness.StoreUserAsync(UserA, UserSecret);
      await harness.StoreSiteAsync(SiteSecret);

      ResolvedXaiApiKey resolved = await harness.Resolver.ResolveAsync(CancellationToken.None);

      resolved.ApiKey.ShouldBe(SiteSecret);
      resolved.Origin.ShouldBe(XaiApiKeyOrigin.Site);
      resolved.Mode.ShouldBe(XaiByokMode.Off);
    }

    public static async Task Policy_Change_Is_Visible_On_The_Next_Resolve()
    {
      Harness harness = await Harness.OpenAsync(UserA, hostKey: null, XaiByokMode.Allow);
      await harness.StoreUserAsync(UserA, UserSecret);
      await harness.StoreSiteAsync(SiteSecret);

      ResolvedXaiApiKey allow = await harness.Resolver.ResolveAsync(CancellationToken.None);
      await harness.SetModeAsync(XaiByokMode.Off);
      ResolvedXaiApiKey off = await harness.Resolver.ResolveAsync(CancellationToken.None);
      await harness.SetModeAsync(XaiByokMode.Required);
      ResolvedXaiApiKey required = await harness.Resolver.ResolveAsync(CancellationToken.None);

      allow.ApiKey.ShouldBe(UserSecret);
      off.ApiKey.ShouldBe(SiteSecret);
      required.ApiKey.ShouldBe(UserSecret);
      required.Origin.ShouldBe(XaiApiKeyOrigin.User);
    }

    public static async Task User_A_Purpose_Does_Not_Open_User_B()
    {
      Harness harness = await Harness.OpenAsync(UserA, hostKey: null, XaiByokMode.Allow);
      (await harness.Users.SetAsync(UserA, UserSecret, CancellationToken.None)).IsT0.ShouldBeTrue();
      (await harness.Users.SetAsync(UserB, OtherSecret, CancellationToken.None)).IsT0.ShouldBeTrue();

      string? crossed = await harness.Store.UnprotectAsync(
        UserXaiApiKeyNames.NameFor(UserA),
        UserXaiApiKeyNames.PurposeFor(UserB),
        CancellationToken.None);
      string? own = await harness.Users.ReadAsync(UserA, CancellationToken.None);
      string? other = await harness.Users.ReadAsync(UserB, CancellationToken.None);

      crossed.ShouldBeNull();
      own.ShouldBe(UserSecret);
      other.ShouldBe(OtherSecret);
    }

    public static async Task Rejected_Key_Is_Not_Stored_And_The_Detail_Omits_It()
    {
      Harness harness = await Harness.OpenAsync(UserA, hostKey: null, XaiByokMode.Allow);
      harness.Probe.Problem = UserXaiApiKeyProblems.ProviderRejected("xAI rejected the API key.");

      OneOf<UserXaiApiKeyStatus, SharedProblemDetails> result =
        await harness.Users.SetAsync(UserA, UserSecret, CancellationToken.None);

      result.IsT1.ShouldBeTrue();
      result.AsT1.Status.ShouldBe(400);
      result.AsT1.Detail.ShouldNotBeNull();
      result.AsT1.Detail!.ShouldNotContain(UserSecret);
      (await harness.Store.FindAsync(UserXaiApiKeyNames.NameFor(UserA), CancellationToken.None)).ShouldBeNull();
    }

    public static async Task Unreachable_Probe_Does_Not_Save()
    {
      Harness harness = await Harness.OpenAsync(UserA, hostKey: null, XaiByokMode.Allow);
      harness.Probe.Problem = UserXaiApiKeyProblems.ProviderUnreachable();

      OneOf<UserXaiApiKeyStatus, SharedProblemDetails> result =
        await harness.Users.SetAsync(UserA, UserSecret, CancellationToken.None);

      result.IsT1.ShouldBeTrue();
      result.AsT1.Status.ShouldBe(503);
      result.AsT1.Detail.ShouldBe("xAI could not be reached. Nothing was saved.");
      result.AsT1.Detail!.ShouldNotContain(UserSecret);
      (await harness.Store.FindAsync(UserXaiApiKeyNames.NameFor(UserA), CancellationToken.None)).ShouldBeNull();
    }

    public static async Task Delete_Removes_Only_That_Users_Row()
    {
      Harness harness = await Harness.OpenAsync(UserA, hostKey: null, XaiByokMode.Allow);
      (await harness.Users.SetAsync(UserA, UserSecret, CancellationToken.None)).IsT0.ShouldBeTrue();
      (await harness.Users.SetAsync(UserB, OtherSecret, CancellationToken.None)).IsT0.ShouldBeTrue();
      await harness.StoreSiteAsync(SiteSecret);

      await harness.Users.DeleteForPrincipalAsync(UserA, CancellationToken.None);

      (await harness.Store.FindAsync(UserXaiApiKeyNames.NameFor(UserA), CancellationToken.None)).ShouldBeNull();
      (await harness.Store.FindAsync(UserXaiApiKeyNames.NameFor(UserB), CancellationToken.None)).ShouldNotBeNull();
      (await harness.Store.FindAsync(SiteXaiApiKeyNames.Name, CancellationToken.None)).ShouldNotBeNull();
    }

    public static async Task Off_Hides_Status_And_Refuses_The_Write()
    {
      Harness harness = await Harness.OpenAsync(UserA, hostKey: null, XaiByokMode.Off);

      OneOf<UserXaiApiKeyStatus, SharedProblemDetails> status =
        await harness.Users.GetStatusAsync(UserA, CancellationToken.None);
      OneOf<UserXaiApiKeyStatus, SharedProblemDetails> write =
        await harness.Users.SetAsync(UserA, UserSecret, CancellationToken.None);

      status.IsT0.ShouldBeTrue();
      status.AsT0.Visible.ShouldBeFalse();
      status.AsT0.Suffix.ShouldBeNull();
      write.IsT1.ShouldBeTrue();
      write.AsT1.Status.ShouldBe(409);
      write.AsT1.Detail.ShouldBe("This site does not accept a personal xAI API key.");
      (await harness.Store.FindAsync(UserXaiApiKeyNames.NameFor(UserA), CancellationToken.None)).ShouldBeNull();
    }

    public static async Task Audit_Line_Omits_The_Key()
    {
      Harness harness = await Harness.OpenAsync(UserA, HostSecret, XaiByokMode.Allow);

      OneOf<UserXaiApiKeyStatus, SharedProblemDetails> saved =
        await harness.Users.SetAsync(UserA, UserSecret, CancellationToken.None);

      saved.IsT0.ShouldBeTrue();
      saved.AsT0.SupersededByHost.ShouldBeTrue();
      saved.AsT0.Suffix.ShouldBe("aaaa");
      string audit = string.Join('\n', harness.Logger.Lines);
      audit.ShouldContain("set");
      audit.ShouldNotContain(UserSecret);
    }

    public static async Task Unreadable_Mode_Is_Logged_And_Does_Not_Use_The_Site_Key()
    {
      MemoryStore store = new();
      await store.SetAsync(
        UserXaiApiKeyNames.NameFor(UserA),
        UserXaiApiKeyNames.PurposeFor(UserA),
        UserSecret,
        UserA,
        DateTimeOffset.UnixEpoch,
        CancellationToken.None);
      await store.SetAsync(
        SiteXaiApiKeyNames.Name,
        SiteXaiApiKeyNames.Purpose,
        SiteSecret,
        UserA,
        DateTimeOffset.UnixEpoch,
        CancellationToken.None);
      ListLogger<XaiApiKeyResolver> logger = new();
      XaiApiKeyResolver resolver = ResolverFor(store, hostKey: null, logger);

      ResolvedXaiApiKey resolved = await resolver.ResolveAsync(CancellationToken.None);

      resolved.ApiKey.ShouldBe(UserSecret);
      resolved.Mode.ShouldBe(XaiByokMode.Required);
      resolved.Origin.ShouldBe(XaiApiKeyOrigin.User);
      string log = string.Join('\n', logger.Lines);
      log.ShouldContain("could not be read");
      log.ShouldNotContain(UserSecret);
      log.ShouldNotContain(SiteSecret);

      MemoryStore siteOnly = new();
      await siteOnly.SetAsync(
        SiteXaiApiKeyNames.Name,
        SiteXaiApiKeyNames.Purpose,
        SiteSecret,
        UserA,
        DateTimeOffset.UnixEpoch,
        CancellationToken.None);
      XaiApiKeyResolver siteResolver = ResolverFor(siteOnly, hostKey: null, new ListLogger<XaiApiKeyResolver>());
      ResolvedXaiApiKey siteResolved = await siteResolver.ResolveAsync(CancellationToken.None);
      siteResolved.ApiKey.ShouldBeNull();
      siteResolved.Origin.ShouldBe(XaiApiKeyOrigin.None);
      siteResolved.Mode.ShouldBe(XaiByokMode.Required);

      ListLogger<XaiApiKeyResolver> hostLogger = new();
      XaiApiKeyResolver hostResolver = ResolverFor(store, HostSecret, hostLogger);
      ResolvedXaiApiKey hostResolved = await hostResolver.ResolveAsync(CancellationToken.None);
      hostResolved.ApiKey.ShouldBe(HostSecret);
      hostResolved.Origin.ShouldBe(XaiApiKeyOrigin.Host);
      string.Join('\n', hostLogger.Lines).ShouldContain("could not be read");
    }

    private static XaiApiKeyResolver ResolverFor(MemoryStore store, string? hostKey, ListLogger<XaiApiKeyResolver> logger)
    {
      FixedMonitor monitor = new(new XaiChatOptions { ApiKey = hostKey });
      NamedHostEnvironment environment = new("Production");
      UserXaiApiKey users = new(store, new ScriptedProbe(), monitor, environment, new ListLogger<UserXaiApiKey>());
      SiteXaiApiKey site = new(store, monitor, environment, new ListLogger<SiteXaiApiKey>());
      return new XaiApiKeyResolver(
        monitor,
        users,
        site,
        new ThrowingSiteSettings(),
        new FixedPrincipal(PrincipalId.From(UserA)),
        logger);
    }

    private sealed class ThrowingSiteSettings : ISiteSettingsStore
    {
      public Task<SiteSettings?> GetAsync(CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("settings unavailable");

      public Task AddAsync(SiteSettings siteSettings, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("settings unavailable");

      public Task UpdateAsync(SiteSettings siteSettings, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("settings unavailable");
    }

    private sealed class Harness
    {
      private Harness(
        UserXaiApiKey users,
        XaiApiKeyResolver resolver,
        MemoryStore store,
        ListLogger<UserXaiApiKey> logger,
        InMemorySiteSettingsStore settings,
        ScriptedProbe probe)
      {
        Users = users;
        Resolver = resolver;
        Store = store;
        Logger = logger;
        Settings = settings;
        Probe = probe;
      }

      public UserXaiApiKey Users { get; }

      public XaiApiKeyResolver Resolver { get; }

      public MemoryStore Store { get; }

      public ListLogger<UserXaiApiKey> Logger { get; }

      public ScriptedProbe Probe { get; }

      private InMemorySiteSettingsStore Settings { get; }

      public static async Task<Harness> OpenAsync(Guid? principalId, string? hostKey, XaiByokMode mode)
      {
        MemoryStore store = new();
        ScriptedProbe probe = new();
        ListLogger<UserXaiApiKey> logger = new();
        FixedMonitor monitor = new(new XaiChatOptions { ApiKey = hostKey });
        NamedHostEnvironment environment = new("Production");
        InMemorySiteSettingsStore settings = new();
        await settings.AddAsync(SiteSettings.Create(xaiByokMode: mode));
        UserXaiApiKey users = new(store, probe, monitor, environment, logger, settings);
        ListLogger<SiteXaiApiKey> siteLogger = new();
        SiteXaiApiKey site = new(store, monitor, environment, siteLogger);
        FixedPrincipal? principal = principalId is Guid id
          ? new FixedPrincipal(PrincipalId.From(id))
          : null;
        XaiApiKeyResolver resolver = new(monitor, users, site, settings, principal);
        return new Harness(users, resolver, store, logger, settings, probe);
      }

      public Task StoreUserAsync(Guid userId, string apiKey) =>
        Store.SetAsync(
          UserXaiApiKeyNames.NameFor(userId),
          UserXaiApiKeyNames.PurposeFor(userId),
          apiKey,
          userId,
          DateTimeOffset.UnixEpoch,
          CancellationToken.None);

      public Task StoreSiteAsync(string apiKey) =>
        Store.SetAsync(
          SiteXaiApiKeyNames.Name,
          SiteXaiApiKeyNames.Purpose,
          apiKey,
          UserA,
          DateTimeOffset.UnixEpoch,
          CancellationToken.None);

      public async Task SetModeAsync(XaiByokMode mode)
      {
        SiteSettings? current = await Settings.GetAsync();
        current.ShouldNotBeNull();
        current.SetXaiByokMode(mode);
        await Settings.UpdateAsync(current);
      }
    }

    private sealed class ScriptedProbe : IXaiApiKeyProbe
    {
      public SharedProblemDetails? Problem { get; set; }

      public Task<OneOf<bool, SharedProblemDetails>> ValidateAsync(
        string apiKey,
        CancellationToken cancellationToken)
      {
        _ = apiKey;
        cancellationToken.ThrowIfCancellationRequested();
        if (Problem is not null)
        {
          return Task.FromResult<OneOf<bool, SharedProblemDetails>>(Problem);
        }

        return Task.FromResult<OneOf<bool, SharedProblemDetails>>(true);
      }
    }

    private sealed class FixedPrincipal(PrincipalId id) : ICurrentPrincipalAccessor
    {
      public Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken) =>
        Task.FromResult<PrincipalId?>(id);
    }

    private sealed class FixedMonitor : IOptionsMonitor<XaiChatOptions>
    {
      public FixedMonitor(XaiChatOptions value)
      {
        CurrentValue = value;
      }

      public XaiChatOptions CurrentValue { get; }

      public XaiChatOptions Get(string? name) => CurrentValue;

      public IDisposable OnChange(Action<XaiChatOptions, string?> listener) => new Noop();

      private sealed class Noop : IDisposable
      {
        public void Dispose()
        {
        }
      }
    }

    private sealed class NamedHostEnvironment : IHostEnvironment
    {
      public NamedHostEnvironment(string environmentName)
      {
        EnvironmentName = environmentName;
      }

      public string EnvironmentName { get; set; }

      public string ApplicationName { get; set; } = "tests";

      public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

      public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
      public List<string> Lines { get; } = [];

      public IDisposable BeginScope<TState>(TState state) where TState : notnull => new Noop();

      public bool IsEnabled(LogLevel logLevel) => true;

      public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
      {
        _ = logLevel;
        _ = eventId;
        _ = exception;
        Lines.Add(formatter(state, exception));
      }

      private sealed class Noop : IDisposable
      {
        public void Dispose()
        {
        }
      }
    }

    private sealed class MemoryStore : IProtectedSecretStore
    {
      private readonly Dictionary<string, (string Plaintext, ProtectedSecretRecord Record)> Rows = [];

      public bool StorageAvailable => true;

      public Task<ProtectedSecretRecord?> FindAsync(string name, CancellationToken cancellationToken)
      {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Rows.TryGetValue(name, out (string Plaintext, ProtectedSecretRecord Record) row)
          ? row.Record
          : null);
      }

      public Task<ProtectedSecretWrite> SetAsync(
        string name,
        string purpose,
        string plaintext,
        Guid setByPrincipalId,
        DateTimeOffset setAt,
        CancellationToken cancellationToken)
      {
        cancellationToken.ThrowIfCancellationRequested();
        bool inserted = !Rows.ContainsKey(name);
        Rows[name] = (plaintext, new ProtectedSecretRecord(
          name,
          purpose,
          ProtectedSecretSuffix.From(plaintext),
          setAt,
          setByPrincipalId));
        return Task.FromResult(inserted ? ProtectedSecretWrite.Inserted : ProtectedSecretWrite.Replaced);
      }

      public Task<string?> UnprotectAsync(string name, string purpose, CancellationToken cancellationToken)
      {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Rows.TryGetValue(name, out (string Plaintext, ProtectedSecretRecord Record) row)
          || !string.Equals(row.Record.Purpose, purpose, StringComparison.Ordinal))
        {
          return Task.FromResult<string?>(null);
        }

        return Task.FromResult<string?>(row.Plaintext);
      }

      public Task<bool> ClearAsync(string name, CancellationToken cancellationToken)
      {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Rows.Remove(name));
      }
    }
  }
}
