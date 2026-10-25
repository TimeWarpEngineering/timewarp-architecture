#region Purpose
// First-run seed of site settings from the sign-in method list and Authentication:Entra, then drift warnings or Development reseed.
#endregion

#region Design
// When the store is empty, copy "Microsoft365 is listed" / AllowBootstrap once so existing
// `dev entra setup` secrets keep working, and copy a GUID Authentication:Entra:TenantId into the
// restricted accepted-tenant list so a home-tenant deployment keeps accepting that tenant.
// organizations / common are authorities, not tenant ids, so they seed an empty restricted list
// (refuse until an administrator chooses a list or Any). After that the list and AllowBootstrap
// are not consulted for policy — listing Microsoft365 remains the scheme-registration gate only.
// PasskeyPromptMode is not in configuration; seed uses Soft. A row with
// EntraTenantAcceptanceInitialized false (migrated before task 306) receives that same one-time
// copy. Concurrent first-seed Add races re-Get (boot, or concurrent reads through
// SeedOnReadSiteSettingsStore — task 254 seeds on first read too); a reseed or acceptance Update
// that loses the same race re-Gets the winner's row instead of surfacing ConcurrencyConflictException.
// Task 225 / 227: each later boot compares persisted offer/bootstrap with configuration.
// LoggerMessage.Define Warning when AllowBootstrap differs or when the listed Microsoft 365 method
// differs from EntraSignInEnabled — each line names /Admin/Authentication and `dev entra reseed`.
// Tenant acceptance is admin-owned, so there is no tenant-drift warning. ReseedSiteSettings
// overwrites the offer from the method list and AllowBootstrap from configuration only when
// isDevelopment is true (hosted service and SeedOnReadSiteSettingsStore pass
// IHostEnvironment.IsDevelopment()). PasskeyPromptMode, assistant name, tenant acceptance, and
// XaiByokMode stay. Reseed does not rewrite whose key Ask uses.
// IsUndefinedTable (Postgres 42P01) lives here so the boot retry and the read path share one
// string-based check (same convention as EfSiteSettingsStore.IsUniqueViolation) without an
// Npgsql package reference in application or server layers.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TimeWarp.Identity;

public sealed class SiteSettingsSeeder
{
  private static readonly Action<ILogger, bool, bool, Exception?> LogConfiguredVsEnabled =
    LoggerMessage.Define<bool, bool>
    (
      LogLevel.Warning,
      new EventId(1, nameof(LogConfiguredVsEnabled)),
      "SignInOptions lists Microsoft365 ({Microsoft365Listed}) but site settings EntraSignInEnabled is {SettingsEnabled}. Listing Microsoft365 registers the named entra scheme; settings decide whether users may sign in. Edit on /Admin/Authentication or run `dev entra reseed`."
    );

  private static readonly Action<ILogger, bool, bool, Exception?> LogConfiguredVsAllowBootstrap =
    LoggerMessage.Define<bool, bool>
    (
      LogLevel.Warning,
      new EventId(3, nameof(LogConfiguredVsAllowBootstrap)),
      "Authentication:Entra:AllowBootstrap is {ConfiguredAllowBootstrap} but site settings EntraAllowBootstrap is {SettingsAllowBootstrap}. Edit on /Admin/Authentication or run `dev entra reseed`."
    );

  private static readonly Action<ILogger, Exception?> LogReseedApplied =
    LoggerMessage.Define
    (
      LogLevel.Information,
      new EventId(4, nameof(LogReseedApplied)),
      "Overwrote EntraSignInEnabled and EntraAllowBootstrap from Authentication:Entra because ReseedSiteSettings is true (Development only). Run `dev entra reseed --clear` so later boots keep admin edits."
    );

  private static readonly Action<ILogger, Exception?> LogReseedIgnored =
    LoggerMessage.Define
    (
      LogLevel.Warning,
      new EventId(5, nameof(LogReseedIgnored)),
      "Authentication:Entra:ReseedSiteSettings is true but isDevelopment is false; the flag is honoured only in Development."
    );

  private static readonly Action<ILogger, int, Exception?> LogTenantAcceptanceInitialized =
    LoggerMessage.Define<int>
    (
      LogLevel.Information,
      new EventId(6, nameof(LogTenantAcceptanceInitialized)),
      "Initialized Entra tenant acceptance to Restricted with {AcceptedTenantCount} tenant id(s) from Authentication:Entra:TenantId. Edit the list or allow any tenant on /Admin/Authentication."
    );

  private readonly ISiteSettingsStore SiteSettingsStore;
  private readonly IOptions<EntraAuthenticationOptions> Options;
  private readonly IOptions<SignInOptions> SignInOptions;
  private readonly ILogger<SiteSettingsSeeder> Logger;

  public SiteSettingsSeeder(
    ISiteSettingsStore siteSettingsStore,
    IOptions<EntraAuthenticationOptions> options,
    IOptions<SignInOptions> signInOptions,
    ILogger<SiteSettingsSeeder> logger)
  {
    SiteSettingsStore = siteSettingsStore;
    Options = options;
    SignInOptions = signInOptions;
    Logger = logger;
  }

  public Task<SiteSettings> GetOrSeedAsync(CancellationToken cancellationToken = default) =>
    GetOrSeedAsync(isDevelopment: false, cancellationToken);

  public async Task<SiteSettings> GetOrSeedAsync(bool isDevelopment, CancellationToken cancellationToken = default)
  {
    SiteSettings? existing = await SiteSettingsStore.GetAsync(cancellationToken).ConfigureAwait(false);
    if (existing is null)
    {
      EntraAuthenticationOptions options = Options.Value;
      IReadOnlyList<EntraAcceptedTenant> homeTenant = ConfiguredHomeTenant(options);
      var created = SiteSettings.Create(
        entraSignInEnabled: SignInOptions.Value.Lists(SignInMethod.Microsoft365),
        entraAllowBootstrap: options.AllowBootstrap,
        passkeyPromptMode: PasskeyPromptMode.Soft,
        entraAcceptedTenants: homeTenant,
        entraTenantAcceptanceInitialized: true);

      try
      {
        await SiteSettingsStore.AddAsync(created, cancellationToken).ConfigureAwait(false);
        existing = created;
      }
      catch (InvalidOperationException)
      {
        existing = await SiteSettingsStore.GetAsync(cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
          throw new InvalidOperationException("Site settings seed raced but re-get returned null.");
        }
      }
    }

    EntraAuthenticationOptions configured = Options.Value;
    if (!existing.EntraTenantAcceptanceInitialized)
    {
      IReadOnlyList<EntraAcceptedTenant> homeTenant = ConfiguredHomeTenant(configured);
      existing.ReplaceEntraTenantAcceptance(EntraTenantAcceptanceMode.Restricted, homeTenant);
      try
      {
        await SiteSettingsStore.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        LogTenantAcceptanceInitialized(Logger, homeTenant.Count, null);
      }
      catch (ConcurrencyConflictException)
      {
        // A concurrent seeder already initialized this row.
      }

      existing = await SiteSettingsStore.GetAsync(cancellationToken).ConfigureAwait(false) ?? existing;
    }

    if (configured.ReseedSiteSettings)
    {
      if (isDevelopment)
      {
        existing.ReplacePolicy(
          SignInOptions.Value.Lists(SignInMethod.Microsoft365),
          configured.AllowBootstrap,
          existing.PasskeyPromptMode);
        try
        {
          await SiteSettingsStore.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
          LogReseedApplied(Logger, null);
        }
        catch (ConcurrencyConflictException)
        {
          // A concurrent seeder already reseeded this row; its values are the same configuration.
        }

        existing = await SiteSettingsStore.GetAsync(cancellationToken).ConfigureAwait(false) ?? existing;
      }
      else
      {
        LogReseedIgnored(Logger, null);
      }
    }

    LogDrift(configured, existing);
    return existing;
  }

  /// <summary>True when <paramref name="exception"/> (or an inner one) is Postgres 42P01 undefined_table.</summary>
  public static bool IsUndefinedTable(Exception exception)
  {
    Exception? current = exception;
    while (current is not null)
    {
      string text = current.GetType().FullName + " " + current.Message;
      if (text.Contains("42P01", StringComparison.Ordinal))
      {
        return true;
      }

      current = current.InnerException;
    }

    return false;
  }

  private static IReadOnlyList<EntraAcceptedTenant> ConfiguredHomeTenant(EntraAuthenticationOptions options)
  {
    if (Guid.TryParse(options.TenantId, out Guid tenantId) && tenantId != Guid.Empty)
    {
      return [EntraAcceptedTenant.IdOnly(tenantId)];
    }

    return [];
  }

  private void LogDrift(EntraAuthenticationOptions configured, SiteSettings existing)
  {
    bool microsoft365Listed = SignInOptions.Value.Lists(SignInMethod.Microsoft365);
    if (existing.EntraSignInEnabled != microsoft365Listed)
    {
      LogConfiguredVsEnabled(Logger, microsoft365Listed, existing.EntraSignInEnabled, null);
    }

    if (existing.EntraAllowBootstrap != configured.AllowBootstrap)
    {
      LogConfiguredVsAllowBootstrap(Logger, configured.AllowBootstrap, existing.EntraAllowBootstrap, null);
    }
  }
}
