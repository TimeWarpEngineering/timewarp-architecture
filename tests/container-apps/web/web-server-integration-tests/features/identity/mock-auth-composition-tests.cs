#region Purpose
// Composition-path regression test for task 145-009 R2-1 (round-2 review, CRITICAL): proves the
// fail-closed mock-auth gate cannot be spoofed via IConfiguration content when the REAL host
// environment is Production.
#endregion

#region Design
// Every prior fail-closed test (MockAuthenticationDefaults_.IsMockAuthActive_Given_,
// foundation-contracts-tests) called the pure predicate directly with string arguments — none
// exercised the actual composition root, so a wiring bug at that seam was invisible to the suite.
// This test calls Web.Server.Program.ConfigureServices(IServiceCollection, IConfiguration) directly
// — the IModule-required 2-arg overload, the SAME path WebApplicationHost&lt;TProgram&gt; (the
// generic test harness) and Program.Main both ultimately go through — and inspects the resulting
// IServiceCollection.
//
// Round-2 review reproduced this exact shape dynamically: a genuinely Production-booted host
// (WebApplicationOptions.EnvironmentName = Production, so builder.Environment.EnvironmentName stays
// Production) with a LATER config source setting ASPNETCORE_ENVIRONMENT=Development and
// Authentication:UseMock=true used to activate mock auth anyway — the old 2-arg
// Web.Spa.Program.ConfigureServices overload re-derived the environment from
// configuration["ASPNETCORE_ENVIRONMENT"] instead of the real IHostEnvironment, and IConfiguration
// content added after host-builder creation can diverge from it freely. The fix makes
// Web.Server.Program.ConfigureServices resolve the REAL environment from the singleton
// IHostEnvironment WebApplicationBuilder pre-registers into Services before Build() (see
// Web.Server.Program.ResolveRealEnvironmentName's Design region) — later config additions can no
// longer move it.
//
// Only IServiceCollection is inspected (no ServiceProvider is built): the full web-server
// composition also registers FastEndpoints/SignalR/EF/Mediator, none of which need to resolve for
// this assertion, and building a real provider would make the test needlessly fragile. Secrets.json
// sources are stripped for the same hermeticity reason as WebApplicationHost (task 104-031) — this
// builder shares Web.Server's ApplicationName/ContentRootPath, so a developer's own Web.Server user
// secrets must not silently affect the assertion.
#endregion

namespace MockAuthComposition_;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TimeWarp.Architecture.Services;
using WebServerProgram = TimeWarp.Architecture.Web.Server.Program;

public class ConfigureServices_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ConfigureServices_Given_>();

  private static WebApplicationBuilder CreateBuilder(string environmentName)
  {
    WebApplicationBuilder builder =
      WebApplication.CreateBuilder
      (
        new WebApplicationOptions
        {
          ApplicationName = typeof(TimeWarp.Architecture.Web.Server.IAssemblyMarker).Assembly.GetName().Name,
          EnvironmentName = environmentName,
          ContentRootPath = ProjectContentRoot.Resolve(typeof(TimeWarp.Architecture.Web.Server.IAssemblyMarker).Assembly),
        }
      );

    // Hermeticity (mirrors WebApplicationHost, task 104-031): this builder shares Web.Server's
    // ApplicationName/ContentRootPath, so strip any secrets.json source before it can pull in a
    // developer's own user secrets.
    IList<IConfigurationSource> configurationSources = ((IConfigurationBuilder)builder.Configuration).Sources;
    for (int index = configurationSources.Count - 1; index >= 0; index--)
    {
      if (configurationSources[index] is JsonConfigurationSource jsonSource && jsonSource.Path == "secrets.json")
        configurationSources.RemoveAt(index);
    }

    return builder;
  }

  private static bool HasMockAuthenticationStateProviderRegistration(IServiceCollection serviceCollection) =>
    serviceCollection.Any
    (
      descriptor =>
        descriptor.ServiceType == typeof(AuthenticationStateProvider)
        && descriptor.ImplementationType == typeof(MockAuthenticationStateProvider)
    );

  public static Task Real_Production_Environment_Should_Not_Activate_Mock_Auth_Even_When_Config_Claims_Development()
  {
    WebApplicationBuilder builder = CreateBuilder(Environments.Production);

    // Reproduce round-2's exact repro shape: config content ALONE claims Development + UseMock=true,
    // layered on AFTER host-builder creation — exactly what a later-loaded provider (appsettings,
    // CLI args, an untrusted/attacker-influenced source) could do to a real Production host.
    builder.Configuration.AddInMemoryCollection
    (
      new Dictionary<string, string?>
      {
        ["ASPNETCORE_ENVIRONMENT"] = Environments.Development,
        ["DOTNET_ENVIRONMENT"] = Environments.Development,
        [MockAuthenticationDefaults.UseMockKey] = "true"
      }
    );

    // The IModule-required 2-arg overload — the path every generic caller (WebApplicationHost<TProgram>)
    // must use; Program.Main passes environmentName explicitly instead (belt-and-suspenders).
    WebServerProgram.ConfigureServices(builder.Services, builder.Configuration);

    HasMockAuthenticationStateProviderRegistration(builder.Services).ShouldBeFalse
    (
      "A Production-booted host must never activate mock auth, even when IConfiguration content " +
      "alone claims Development + Authentication:UseMock=true (task 145-009 R2-1)."
    );

    return Task.CompletedTask;
  }

  public static Task Real_Development_Environment_With_UseMock_Should_Activate_Mock_Auth()
  {
    WebApplicationBuilder builder = CreateBuilder(Environments.Development);
    builder.Configuration.AddInMemoryCollection
    (
      new Dictionary<string, string?> { [MockAuthenticationDefaults.UseMockKey] = "true" }
    );

    WebServerProgram.ConfigureServices(builder.Services, builder.Configuration);

    HasMockAuthenticationStateProviderRegistration(builder.Services).ShouldBeTrue
    (
      "A genuinely Development-booted host with Authentication:UseMock=true should activate mock auth."
    );

    return Task.CompletedTask;
  }
}
