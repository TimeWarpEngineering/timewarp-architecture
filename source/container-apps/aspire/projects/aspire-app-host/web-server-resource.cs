#region Purpose
// Web.Server project-resource wiring that needs no other resource: run-mode exposure and the State
// DevTools inspector, the published Entra parameters, the mock-authentication opt-in, and its public
// exposure on Azure Container Apps when there is no YARP ingress.
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
// Exposure rule (task 527). Run mode: web-server is external on every target (the dashboard links
// straight to it). Publish mode with the yarp flag: never external; the ingress is the one public app.
// Publish mode without the yarp flag: external on aca and aca-neon only. ACA's managed ingress is
// the edge there, so a web-only app needs no YARP in front, and the container app is otherwise
// unreachable. Compose and Kubernetes stay without a host port or Ingress (the operator's edge
// proxy, or YARP, is the front door there). On ACA the public host is
// web-server.<environment default domain>, added to WebAuthnOptions:AllowedRpIds from the
// environment output (PublishTarget.ContainerAppPublicHost), the same way the ingress block adds
// ingress.<domain>; referencing web-server's own endpoint would make it reference itself.
// program.cs calls PublishAsContainerAppsEdge in the slot IngressResource.Add takes with yarp on,
// after AddEFMigrations, so ef-tool-web-migrations does not copy the RP-ID variable.
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
    IResourceBuilder<DotnetProjectResource> webServer
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
    IResourceBuilder<DotnetProjectResource> webServer
  )
  {
    // External in run mode here (dashboard links straight to Web.Server). Published, the ingress is
    // the single host-exposed service; an external web-server would publish its own host port. The
    // one published exception is aca/aca-neon without the yarp flag (PublishAsContainerAppsEdge).
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

  /// <summary>
  /// Without the yarp flag: on aca and aca-neon, makes web-server the one external container app and
  /// allows its public host as a passkey RP ID. Every other target and run mode are unchanged.
  /// </summary>
  internal static void PublishAsContainerAppsEdge
  (
    PublishTarget publishTarget,
    IResourceBuilder<DotnetProjectResource> webServer
  )
  {
    // Task 527: ContainerAppPublicHost is null outside aca/aca-neon, which also covers run mode
    // (PublishTarget reads Publish:Target in publish mode only).
    if (publishTarget.ContainerAppPublicHost(WebServerProjectResourceName) is not { } webServerHost)
    {
      return;
    }

    webServer
      .WithExternalHttpEndpoints()
      .WithEnvironment("WebAuthnOptions__AllowedRpIds__0", webServerHost);
  }

  private static void AddPublishedEntraSettings
  (
    IDistributedApplicationBuilder builder,
    IResourceBuilder<DotnetProjectResource> webServer
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
    // entra-client-id and entra-public-origin are empty while Entra is off, and empty is a valid
    // value for them. Aspire 17 rejects an empty value for a required parameter, so those two are
    // AsOptional (task 498). The others always carry a value and stay required.
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
      .WithEnvironment("Authentication__Entra__ClientId", builder.AddParameter(EntraClientIdParameterName, "", publishValueAsDefault: true).AsOptional())
      .WithEnvironment("Authentication__Entra__ClientSecret", builder.AddParameter(EntraClientSecretParameterName, entraClientSecret, secret: true))
      .WithEnvironment("Authentication__Entra__PublicOrigin", builder.AddParameter(EntraPublicOriginParameterName, "", publishValueAsDefault: true).AsOptional());
  }

  private static void ForwardMockAuthenticationOptIn
  (
    IDistributedApplicationBuilder builder,
    IResourceBuilder<DotnetProjectResource> webServer
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
