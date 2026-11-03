#region Purpose
// Shared fakes and the harness for the per-user xAI API key runfile: in-memory secret store, scripted probe, and log capture.
#endregion

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

  internal sealed class XaiKeyTestHarness
  {
    private XaiKeyTestHarness(
      UserXaiApiKey users,
      XaiApiKeyResolver resolver,
      XaiKeyTestStore store,
      XaiKeyTestLogger<UserXaiApiKey> logger,
      InMemorySiteSettingsStore settings,
      XaiKeyTestProbe probe)
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

    public XaiKeyTestStore Store { get; }

    public XaiKeyTestLogger<UserXaiApiKey> Logger { get; }

    public XaiKeyTestProbe Probe { get; }

    private InMemorySiteSettingsStore Settings { get; }

    /// <summary>The test user. It also sets the site key.</summary>
    public static readonly Guid UserA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public static async Task<XaiKeyTestHarness> OpenAsync(Guid? principalId, string? hostKey, XaiByokMode mode)
    {
      XaiKeyTestStore store = new();
      XaiKeyTestProbe probe = new();
      XaiKeyTestLogger<UserXaiApiKey> logger = new();
      XaiKeyTestMonitor monitor = new(new XaiChatOptions { ApiKey = hostKey });
      XaiKeyTestHostEnvironment environment = new("Production");
      InMemorySiteSettingsStore settings = new();
      await settings.AddAsync(SiteSettings.Create(xaiByokMode: mode));
      UserXaiApiKey users = new(store, probe, monitor, environment, logger, settings);
      XaiKeyTestLogger<SiteXaiApiKey> siteLogger = new();
      SiteXaiApiKey site = new(store, monitor, environment, siteLogger);
      XaiKeyTestPrincipal? principal = principalId is Guid id
        ? new XaiKeyTestPrincipal(PrincipalId.From(id))
        : null;
      XaiApiKeyResolver resolver = new(monitor, users, site, settings, principal);
      return new XaiKeyTestHarness(users, resolver, store, logger, settings, probe);
    }

    public Task<ProtectedSecretWrite> StoreUserAsync(Guid userId, string apiKey) =>
      Store.SetAsync(
        UserXaiApiKeyNames.NameFor(userId),
        UserXaiApiKeyNames.PurposeFor(userId),
        apiKey,
        userId,
        DateTimeOffset.UnixEpoch,
        CancellationToken.None);

    public Task<ProtectedSecretWrite> StoreSiteAsync(string apiKey) =>
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

  internal sealed class XaiKeyTestProbe : IXaiApiKeyProbe
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

  internal sealed class XaiKeyTestPrincipal(PrincipalId id) : ICurrentPrincipalAccessor
  {
    public Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken) =>
      Task.FromResult<PrincipalId?>(id);
  }

  internal sealed class XaiKeyTestMonitor : IOptionsMonitor<XaiChatOptions>
  {
    public XaiKeyTestMonitor(XaiChatOptions value)
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

  internal sealed class XaiKeyTestHostEnvironment : IHostEnvironment
  {
    public XaiKeyTestHostEnvironment(string environmentName)
    {
      EnvironmentName = environmentName;
    }

    public string EnvironmentName { get; set; }

    public string ApplicationName { get; set; } = "tests";

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
  }

  internal sealed class XaiKeyTestLogger<T> : ILogger<T>
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

  internal sealed class XaiKeyTestStore : IProtectedSecretStore
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
