#region Purpose
// Web.Server project-resource wiring that needs no other resource: run-mode exposure and the State
// DevTools inspector, the published Entra parameters, and the mock-authentication opt-in.
#endregion

#region Design
// TimeWarp.State DevTools (task 335-001): AddTimeWarpStateDevTools (package TimeWarp.State.DevTools.Hosting)
// adds the packaged inspector (TimeWarp.State.DevTools.App, a framework-dependent dll the package targets
// stamp onto this assembly) as the timewarp-devtools executable resource, the package default. The
// dashboard links it as "TimeWarp DevTools". WithTimeWarpStateDevTools
// sets TIMEWARP_STATE_DEVTOOLS_ENDPOINT on web-server and waits for the inspector; web-server
// (Development only) exports its State action spans there. Both calls sit in the IsRunMode block:
// the package already keeps the inspector out of the manifest, and the
// explicit gate keeps publish models from ever resolving the inspector path. compose/kubernetes/aca
// publish suites assert neither the resource nor the variable appears. Inside the web block: no
// web-server, no TimeWarp.State app to inspect. Server-side stores (prerender, InteractiveServer) export
// from web-server. Task 335-002: browser (WASM) stores export from web-spa (Debug builds, Development)
// to the inspector's POST /ingest/spans, which allows only listed CORS origins. WithTimeWarpStateDevTools
// lists web-server's endpoints; with yarp on, the run-mode ingress block adds the ingress origins through
// TimeWarp__State__DevTools__CorsOrigins on the inspector, because the browser loads the SPA from there.
// Order matters to callers: program.cs calls Configure before the api, grpc and Postgres references,
// so the environment AddEFMigrations snapshots onto ef-tool-web-migrations is the one it always saw.
#endregion

namespace TimeWarp.Architecture.Aspire;

/// <summary>Self-contained wiring on the web-server project resource.</summary>
internal static class WebServerResource
{
  /// <summary>Applies run-mode tooling, published Entra settings and the mock-auth opt-in.</summary>
  /// <returns>The State DevTools inspector in run mode; null in publish mode.</returns>
  internal static IResourceBuilder<ExecutableResource>? Configure
  (
    IDistributedApplicationBuilder builder,
    IResourceBuilder<ProjectResource> webServer
  )
  {
    IResourceBuilder<ExecutableResource>? stateDevTools = AddRunModeTooling(builder, webServer);
    AddPublishedEntraSettings(builder, webServer);
    ForwardMockAuthenticationOptIn(builder, webServer);
    return stateDevTools;
  }

  private static IResourceBuilder<ExecutableResource>? AddRunModeTooling
  (
    IDistributedApplicationBuilder builder,
    IResourceBuilder<ProjectResource> webServer
  )
  {
    // External in run mode only (dashboard links straight to Web.Server). Published, the ingress is
    // the single host-exposed service; an external web-server would publish its own host port.
    // Task 335-002: null in publish mode; the ingress block adds its origins to the inspector's CORS list.
    if (!builder.ExecutionContext.IsRunMode)
    {
      return null;
    }

    webServer.WithExternalHttpEndpoints();

    // Task 335-001: the packaged TimeWarp.State DevTools inspector, run mode only (see Design).
    // web-server gets TIMEWARP_STATE_DEVTOOLS_ENDPOINT and waits for the inspector.
    IResourceBuilder<ExecutableResource> stateDevTools = builder.AddTimeWarpStateDevTools();
    webServer.WithTimeWarpStateDevTools(stateDevTools);
    return stateDevTools;
  }

  private static void AddPublishedEntraSettings
  (
    IDistributedApplicationBuilder builder,
    IResourceBuilder<ProjectResource> webServer
  )
  {
    // Task 070-003: published Entra settings are parameters, never literals. Defaults keep Entra off.
    // This block runs for every publish target, not only Azure Container Apps.
    // entra-enabled stays a deploy-time parameter. It is published as
    // SignInOptions__IncludeMicrosoft365, which asks web-server to append Microsoft365 to the
    // checked-in Methods list. The AppHost does not write SignInOptions__Methods__0, so a
    // checked-in Microsoft365-only list is not replaced with Passkey.
    // AddParameter(name, value, secret: true) ignores configuration (its value callback is constant).
    // Capture Parameters:entra-client-secret here. When it is unset, use "unused" (not a credential).
    // Azure Container Apps rejects an empty container-app secret; the same placeholder keeps the
    // other targets' secret non-empty. With Entra enabled and this placeholder still set, sign-in
    // fails with AADSTS invalid_client. aspire deploy runs as Production and does not load user
    // secrets; dev deploy copies a set user secret into the process environment.
    if (!builder.ExecutionContext.IsPublishMode)
    {
      return;
    }

    const string unsetEntraClientSecret = "unused";
    string? configuredEntraClientSecret = builder.Configuration[$"Parameters:{EntraClientSecretParameterName}"];
    string entraClientSecret = string.IsNullOrWhiteSpace(configuredEntraClientSecret)
      ? unsetEntraClientSecret
      : configuredEntraClientSecret;
    IResourceBuilder<ParameterResource> entraEnabledParameter =
      builder.AddParameter(EntraEnabledParameterName, "false", publishValueAsDefault: true);
    webServer
      .WithEnvironment("SignInOptions__IncludeMicrosoft365", entraEnabledParameter)
      // TenantId is the OIDC authority (organizations = any work/school tenant), not the trust list.
      // Acceptance is SiteSettings on /Admin/Authentication. A GUID here limits OIDC to that tenant.
      .WithEnvironment("Authentication__Entra__TenantId", builder.AddParameter(EntraTenantIdParameterName, "organizations", publishValueAsDefault: true))
      .WithEnvironment("Authentication__Entra__ClientId", builder.AddParameter(EntraClientIdParameterName, "", publishValueAsDefault: true))
      .WithEnvironment("Authentication__Entra__ClientSecret", builder.AddParameter(EntraClientSecretParameterName, entraClientSecret, secret: true))
      .WithEnvironment("Authentication__Entra__PublicOrigin", builder.AddParameter(EntraPublicOriginParameterName, "", publishValueAsDefault: true));
  }

  private static void ForwardMockAuthenticationOptIn
  (
    IDistributedApplicationBuilder builder,
    IResourceBuilder<ProjectResource> webServer
  )
  {
    // Task 145-009 / dogfood: mock auth is OPT-IN only. Do not force Authentication:UseMock=true
    // for every Development AppHost run — that overrode appsettings (false) and turned on SPA/BFF
    // mock auth for local passkey dogfood. Forward the flag only when AppHost configuration
    // explicitly sets Authentication:UseMock (e.g. aspire-tests: --Authentication:UseMock=true).
    // Production never needs this injection; env vars still cannot enable mock outside
    // Development/Testing (handler/registration fail-closed gates). Run mode only (task 070-003):
    // published output must never carry the flag, whatever configuration the publish ran with.
    string? useMock = builder.Configuration["Authentication:UseMock"];
    if (builder.ExecutionContext.IsRunMode
      && (string.Equals(useMock, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(useMock, "1", StringComparison.OrdinalIgnoreCase)))
    {
      webServer.WithEnvironment("Authentication__UseMock", "true");
    }
  }
}
