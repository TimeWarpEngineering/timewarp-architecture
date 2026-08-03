#region Purpose
// Single registration site for SPA mock auth providers (task 145-009) — fail-closed runtime gate.
// TWA0021 requires MockAuthenticationStateProvider / MockAccessTokenProvider DI registrations
// to live only in this type so the environment+config gate cannot be bypassed by a casual
// AddScoped at a call site.
#endregion

#region Design
// Runtime replaces the former compile-time MOCK_AUTHENTICATION define: Development/Testing +
// Authentication:UseMock enables mocks; Production never does (even if the flag is true). Absent
// UseMock defaults false (fail-closed); appsettings.Development.json sets true so local template
// UX matches the prior always-mock default. <paramref name="environmentName"/> below must be the
// REAL host environment, not something read out of IConfiguration (task 145-009 R2-1: an earlier
// version of this comment claimed Web.Server's prerender call passed
// "ASPNETCORE_ENVIRONMENT from configuration" and that "the same gate applies" — that was false:
// IConfiguration content is attacker/config-provider-influenced and can diverge from the real
// environment on a Production-booted host, which round-2 review proved dynamically. Web.Server now
// resolves the true IHostEnvironment and threads its EnvironmentName through explicitly — see
// Web.Server.Program.ConfigureServices' Design region and ResolveRealEnvironmentName.
#endregion

namespace TimeWarp.Architecture.Services;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Sole allowed registration site for SPA mock authentication providers.
/// </summary>
public static class MockAuthenticationRegistration
{
  /// <summary>
  /// Registers mock SPA auth when environment+config allow; otherwise returns false without
  /// mutating the service collection (caller must register real auth).
  /// </summary>
  public static bool TryAddSpaMockAuthentication
  (
    IServiceCollection serviceCollection,
    IConfiguration configuration,
    string? environmentName
  )
  {
    if (!MockAuthenticationDefaults.IsMockAuthActive(environmentName, configuration[MockAuthenticationDefaults.UseMockKey]))
      return false;

    serviceCollection.AddScoped<AuthenticationStateProvider, MockAuthenticationStateProvider>();
    serviceCollection.AddScoped<IAccessTokenProvider, MockAccessTokenProvider>();
    return true;
  }
}
