#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0007;IDE0008

#region Purpose
// Site xAI API key: set, replace, clear, host precedence, and an audit line that omits the secret.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.AgentChats
{

  using Microsoft.Extensions.FileProviders;
  using OneOf;
  using Microsoft.Extensions.Hosting;
  using Microsoft.Extensions.Logging;
  using Microsoft.Extensions.Options;
  using Shouldly;
  using TimeWarp.Architecture.Features.AgentChats.Application;
  using TimeWarp.Architecture.Secrets;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Unit")]
  public class SiteXaiApiKey_Given_
  {
    private const string Secret = "xai-test-key-398-aaaa";
    private const string Replacement = "xai-test-key-398-bbbb";
    private const string HostSecret = "xai-host-key-398-hhhh";
    private static readonly Guid Principal = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<SiteXaiApiKey_Given_>();

    public static async Task Set_Status_And_Resolve_Should_RoundTrip_And_Audit_Omits_The_Key()
    {
      MemoryStore store = new();
      ListLogger<SiteXaiApiKey> logger = new();
      SiteXaiApiKey keys = Create(store, logger, hostKey: null, development: true);

      OneOf<SiteXaiApiKeyStatus, SharedProblemDetails> set = await keys.SetAsync(Secret, Principal, CancellationToken.None);
      set.IsT0.ShouldBeTrue();
      set.AsT0.Configured.ShouldBeTrue();
      set.AsT0.CanChange.ShouldBeTrue();
      set.AsT0.ManagedByHost.ShouldBeFalse();
      set.AsT0.Suffix.ShouldBe("aaaa");
      set.AsT0.SetByPrincipalId.ShouldBe(Principal);
      (await keys.ResolveApiKeyAsync(CancellationToken.None)).ShouldBe(Secret);

      OneOf<SiteXaiApiKeyStatus, SharedProblemDetails> replaced =
        await keys.SetAsync(Replacement, Principal, CancellationToken.None);
      replaced.IsT0.ShouldBeTrue();
      replaced.AsT0.Suffix.ShouldBe("bbbb");
      (await keys.ResolveApiKeyAsync(CancellationToken.None)).ShouldBe(Replacement);

      OneOf<SiteXaiApiKeyStatus, SharedProblemDetails> cleared =
        await keys.ClearAsync(Principal, CancellationToken.None);
      cleared.IsT0.ShouldBeTrue();
      cleared.AsT0.Configured.ShouldBeFalse();
      cleared.AsT0.Suffix.ShouldBeNull();
      (await keys.ResolveApiKeyAsync(CancellationToken.None)).ShouldBeNull();

      string audit = string.Join('\n', logger.Lines);
      audit.ShouldContain("set");
      audit.ShouldContain("replace");
      audit.ShouldContain("clear");
      audit.ShouldContain(Principal.ToString("D"));
      audit.ShouldNotContain(Secret);
      audit.ShouldNotContain(Replacement);
      audit.ShouldNotContain("aaaa");
      audit.ShouldNotContain("bbbb");
    }

    public static async Task Host_Key_Should_Win_And_Refuse_Writes()
    {
      MemoryStore store = new();
      await store.SetAsync(
        SiteXaiApiKeyNames.Name,
        SiteXaiApiKeyNames.Purpose,
        Secret,
        Principal,
        DateTimeOffset.UnixEpoch,
        CancellationToken.None);
      ListLogger<SiteXaiApiKey> logger = new();
      SiteXaiApiKey keys = Create(store, logger, HostSecret, development: false);

      (await keys.ResolveApiKeyAsync(CancellationToken.None)).ShouldBe(HostSecret);
      OneOf<SiteXaiApiKeyStatus, SharedProblemDetails> statusResult =
        await keys.GetStatusAsync(CancellationToken.None);
      statusResult.IsT0.ShouldBeTrue();
      SiteXaiApiKeyStatus status = statusResult.AsT0;
      status.ManagedByHost.ShouldBeTrue();
      status.Configured.ShouldBeTrue();
      status.CanChange.ShouldBeFalse();
      status.Suffix.ShouldBeNull();

      OneOf<SiteXaiApiKeyStatus, SharedProblemDetails> set =
        await keys.SetAsync(Replacement, Principal, CancellationToken.None);
      set.IsT1.ShouldBeTrue();
      set.AsT1.Status.ShouldBe(409);
      set.AsT1.Detail!.ShouldNotContain(Replacement);
      (await store.UnprotectAsync(SiteXaiApiKeyNames.Name, SiteXaiApiKeyNames.Purpose, CancellationToken.None))
        .ShouldBe(Secret);

      OneOf<SiteXaiApiKeyStatus, SharedProblemDetails> cleared =
        await keys.ClearAsync(Principal, CancellationToken.None);
      cleared.IsT1.ShouldBeTrue();
      cleared.AsT1.Status.ShouldBe(409);
      logger.Lines.ShouldBeEmpty();
    }

    public static async Task Missing_Storage_Should_Explain_The_Host_And_Not_Store()
    {
      ListLogger<SiteXaiApiKey> logger = new();
      SiteXaiApiKey keys = Create(new UnavailableProtectedSecretStore(), logger, hostKey: null, development: false);

      OneOf<SiteXaiApiKeyStatus, SharedProblemDetails> statusResult =
        await keys.GetStatusAsync(CancellationToken.None);
      statusResult.IsT0.ShouldBeTrue();
      SiteXaiApiKeyStatus status = statusResult.AsT0;
      status.StorageAvailable.ShouldBeFalse();
      status.CanChange.ShouldBeFalse();
      status.Configured.ShouldBeFalse();

      OneOf<SiteXaiApiKeyStatus, SharedProblemDetails> set =
        await keys.SetAsync(Secret, Principal, CancellationToken.None);
      set.IsT1.ShouldBeTrue();
      set.AsT1.Status.ShouldBe(503);
      set.AsT1.Detail.ShouldNotBeNull();
      set.AsT1.Detail!.ShouldBe(XaiChatDefaults.DeployedSetupHint);
      set.AsT1.Detail.ShouldNotContain("Admin → AI");
      set.AsT1.Detail.ShouldContain("XAI__ApiKey");
      set.AsT1.Detail.ShouldNotContain(Secret);

      OneOf<SiteXaiApiKeyStatus, SharedProblemDetails> cleared =
        await keys.ClearAsync(Principal, CancellationToken.None);
      cleared.IsT1.ShouldBeTrue();
      cleared.AsT1.Status.ShouldBe(503);
      cleared.AsT1.Detail.ShouldNotBeNull();
      cleared.AsT1.Detail!.ShouldNotContain("Admin → AI");
      logger.Lines.ShouldBeEmpty();

      SiteXaiApiKey development = Create(new UnavailableProtectedSecretStore(), new ListLogger<SiteXaiApiKey>(), hostKey: null, development: true);
      OneOf<SiteXaiApiKeyStatus, SharedProblemDetails> developmentSet =
        await development.SetAsync(Secret, Principal, CancellationToken.None);
      developmentSet.IsT1.ShouldBeTrue();
      developmentSet.AsT1.Detail.ShouldBe(XaiChatDefaults.SetupCommand);
      developmentSet.AsT1.Detail!.ShouldNotContain("Admin");
    }

    public static async Task Unreadable_Stored_Key_Should_Need_Replacement()
    {
      MemoryStore store = new();
      ListLogger<SiteXaiApiKey> logger = new();
      SiteXaiApiKey keys = Create(store, logger, hostKey: null, development: true);
      (await keys.SetAsync(Secret, Principal, CancellationToken.None)).IsT0.ShouldBeTrue();
      store.FailUnprotect = true;

      OneOf<SiteXaiApiKeyStatus, SharedProblemDetails> statusResult =
        await keys.GetStatusAsync(CancellationToken.None);
      statusResult.IsT0.ShouldBeTrue();
      statusResult.AsT0.Configured.ShouldBeFalse();
      statusResult.AsT0.NeedsReplacement.ShouldBeTrue();
      statusResult.AsT0.Suffix.ShouldBe("aaaa");
      statusResult.AsT0.CanChange.ShouldBeTrue();
      (await keys.ResolveApiKeyAsync(CancellationToken.None)).ShouldBeNull();
    }

    public static async Task Storage_Failure_Should_Degrade_Ask_And_Return_503()
    {
      ThrowingStore store = new();
      ListLogger<SiteXaiApiKey> logger = new();
      SiteXaiApiKey keys = Create(store, logger, hostKey: null, development: false);

      (await keys.ResolveApiKeyAsync(CancellationToken.None)).ShouldBeNull();
      OneOf<SiteXaiApiKeyStatus, SharedProblemDetails> status =
        await keys.GetStatusAsync(CancellationToken.None);
      status.IsT1.ShouldBeTrue();
      status.AsT1.Status.ShouldBe(503);
      status.AsT1.Detail.ShouldNotBeNull();
      status.AsT1.Detail!.ShouldNotContain("Admin → AI");
      status.AsT1.Detail.ShouldNotContain(Secret);

      OneOf<SiteXaiApiKeyStatus, SharedProblemDetails> set =
        await keys.SetAsync(Secret, Principal, CancellationToken.None);
      set.IsT1.ShouldBeTrue();
      set.AsT1.Status.ShouldBe(503);
      set.AsT1.Detail!.ShouldNotContain(Secret);

      OneOf<SiteXaiApiKeyStatus, SharedProblemDetails> cleared =
        await keys.ClearAsync(Principal, CancellationToken.None);
      cleared.IsT1.ShouldBeTrue();
      cleared.AsT1.Status.ShouldBe(503);

      string audit = string.Join('\n', logger.Lines);
      audit.ShouldContain(nameof(InvalidOperationException));
      audit.ShouldContain(nameof(TimeoutException));
      audit.ShouldNotContain(Secret);
      audit.ShouldNotContain("key set by");
      audit.ShouldNotContain("key clear by");

      SiteXaiApiKey hosted = Create(store, new ListLogger<SiteXaiApiKey>(), HostSecret, development: false);
      (await hosted.ResolveApiKeyAsync(CancellationToken.None)).ShouldBe(HostSecret);
      OneOf<SiteXaiApiKeyStatus, SharedProblemDetails> hostStatus =
        await hosted.GetStatusAsync(CancellationToken.None);
      hostStatus.IsT0.ShouldBeTrue();
      hostStatus.AsT0.Configured.ShouldBeTrue();
      hostStatus.AsT0.ManagedByHost.ShouldBeTrue();
    }

    public static async Task Audit_Label_Should_Follow_The_Store_Write()
    {
      MemoryStore store = new() { Report = ProtectedSecretWrite.Replaced };
      ListLogger<SiteXaiApiKey> logger = new();
      SiteXaiApiKey keys = Create(store, logger, hostKey: null, development: true);

      (await keys.SetAsync(Secret, Principal, CancellationToken.None)).IsT0.ShouldBeTrue();

      string audit = string.Join('\n', logger.Lines);
      audit.ShouldContain("key replace by");
      audit.ShouldNotContain("key set by");
      audit.ShouldNotContain(Secret);
    }

    private static SiteXaiApiKey Create(
      IProtectedSecretStore store,
      ListLogger<SiteXaiApiKey> logger,
      string? hostKey,
      bool development) =>
      new(
        store,
        new FixedMonitor(new XaiChatOptions { ApiKey = hostKey }),
        new NamedHostEnvironment(development ? "Development" : "Production"),
        logger);

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

    private sealed class ThrowingStore : IProtectedSecretStore
    {
      public bool StorageAvailable => true;

      public Task<ProtectedSecretRecord?> FindAsync(string name, CancellationToken cancellationToken)
      {
        _ = name;
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException("42P01: relation \"secrets.protected_secrets\" does not exist");
      }

      public Task<ProtectedSecretWrite> SetAsync(
        string name,
        string purpose,
        string plaintext,
        Guid setByPrincipalId,
        DateTimeOffset setAt,
        CancellationToken cancellationToken)
      {
        _ = name;
        _ = purpose;
        _ = plaintext;
        _ = setByPrincipalId;
        _ = setAt;
        cancellationToken.ThrowIfCancellationRequested();
        throw new TimeoutException("Npgsql timeout");
      }

      public Task<string?> UnprotectAsync(string name, string purpose, CancellationToken cancellationToken)
      {
        _ = name;
        _ = purpose;
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException("42P01: relation \"secrets.protected_secrets\" does not exist");
      }

      public Task<bool> ClearAsync(string name, CancellationToken cancellationToken)
      {
        _ = name;
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException("DbException: connection refused");
      }
    }

    private sealed class MemoryStore : IProtectedSecretStore
    {
      private readonly Dictionary<string, (string Plaintext, ProtectedSecretRecord Record)> Rows = [];

      public bool StorageAvailable => true;

      public bool FailUnprotect { get; set; }

      public ProtectedSecretWrite? Report { get; set; }

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
        return Task.FromResult(Report ?? (inserted ? ProtectedSecretWrite.Inserted : ProtectedSecretWrite.Replaced));
      }

      public Task<string?> UnprotectAsync(string name, string purpose, CancellationToken cancellationToken)
      {
        cancellationToken.ThrowIfCancellationRequested();
        if (FailUnprotect
          || !Rows.TryGetValue(name, out (string Plaintext, ProtectedSecretRecord Record) row)
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
