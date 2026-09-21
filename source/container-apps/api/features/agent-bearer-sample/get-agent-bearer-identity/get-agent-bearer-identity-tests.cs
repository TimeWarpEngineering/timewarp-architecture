#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/api/projects/api-contracts/api-contracts.csproj
#:project $(TestsDirectory)common/timewarp-testing/timewarp-testing.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:package FluentValidation
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0007;IDE0008;IDE0161;IDE0021;IDE0058

// Co-located Jaribu in-proc Api tests (task 104-030): agent bearer validation + PascalCase
// string-enum wire through FastEndpoints on api-server.
// Run: dotnet run source/container-apps/api/features/agent-bearer-sample/get-agent-bearer-identity/get-agent-bearer-identity-tests.cs

#region Purpose
// In-proc api-server host: proves agent-token scheme + identity:read policy, insufficient-scope
// 403, and response JSON uses PascalCase enum names (not integers) via CommonServerModule HttpJson.
#endregion

#region Design
// Tokens are minted into THIS host's IAgentTokenStore (not via web's issuance ceremony): in-memory
// stores are process-local, so web-issued tokens would not validate here without a shared store.
// C-create: one HostGraph per class (CreateApiAsync). Isolated HttpClient per call so Authorization
// headers never leak across tests.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.AgentBearerSamples
{

  using System.Net;
  using System.Net.Http.Headers;
  using System.Text.Json;
  using Microsoft.Extensions.DependencyInjection;
  using Shouldly;
  using TimeWarp.Architecture.Testing;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.AgentBearerSamples.GetAgentBearerIdentity;

  [TestTag("Integration")]
  public class GetAgentBearerIdentityEndpoint_Given_
  {
    private static HostGraph? Graph;

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<GetAgentBearerIdentityEndpoint_Given_>();

    public static async Task SetupOnce()
    {
      Graph = await HostGraphFactory.CreateApiAsync();
    }

    public static async Task CleanUpOnce()
    {
      if (Graph is not null)
      {
        await Graph.DisposeAsync();
        Graph = null;
      }
    }

    public static async Task Ok_With_String_Enums_Given_Valid_IdentityRead_Token()
    {
      string accessToken = await SeedPrincipalAndIssueToken([AgentScopes.IdentityRead]);

      using HttpClient client = CreateIsolatedClient(accessToken);
      using HttpResponseMessage response = await client.GetAsync(Query.RouteTemplate);

      response.StatusCode.ShouldBe(HttpStatusCode.OK);
      string json = await response.Content.ReadAsStringAsync();
      // Task 108 follow-up (104-030): FastEndpoints must honor HttpJsonOptions string enums.
      json.ShouldContain("\"kind\":\"Agent\"");
      json.ShouldContain("\"trustTier\":\"Keyed\"");
      json.ShouldNotContain("\"kind\":2");
      json.ShouldNotContain("\"trustTier\":2");

      Response? parsed = JsonSerializer.Deserialize<Response>(json, ContractSerializationDefaults.Options);
      parsed.ShouldNotBeNull();
      parsed.Kind.ShouldBe(PrincipalKind.Agent);
      parsed.TrustTier.ShouldBe(TrustTier.Keyed);
      parsed.Scopes.ShouldContain(AgentScopes.IdentityRead);
    }

    public static async Task Unauthorized_Given_No_Header()
    {
      using HttpClient client = CreateIsolatedClient(accessToken: null);
      using HttpResponseMessage response = await client.GetAsync(Query.RouteTemplate);

      response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
      response.Headers.GetValues("WWW-Authenticate").Single().ShouldBe("Bearer");
      response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    public static async Task Unauthorized_InvalidToken_Given_Garbage_Bearer()
    {
      using HttpClient client = CreateIsolatedClient("not-a-real-token");
      using HttpResponseMessage response = await client.GetAsync(Query.RouteTemplate);

      response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
      response.Headers.GetValues("WWW-Authenticate").Single().ShouldBe("Bearer error=\"invalid_token\"");
    }

    public static async Task Forbidden_InsufficientScope_Given_DemoInvoke_Only_Token()
    {
      string accessToken = await SeedPrincipalAndIssueToken([AgentScopes.DemoInvoke]);

      using HttpClient client = CreateIsolatedClient(accessToken);
      using HttpResponseMessage response = await client.GetAsync(Query.RouteTemplate);

      response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      response.Headers.GetValues("WWW-Authenticate").Single().ShouldBe("Bearer error=\"insufficient_scope\"");
    }

    private static async Task<string> SeedPrincipalAndIssueToken(IReadOnlyCollection<string> scopes)
    {
      IServiceProvider services = Graph!.Api!.WebApplicationHost.ServiceProvider;
      IPrincipalStore principalStore = services.GetRequiredService<IPrincipalStore>();
      IAgentTokenStore tokenStore = services.GetRequiredService<IAgentTokenStore>();

      Principal principal = Principal.Create(PrincipalKind.Agent);
      principal.RecordCredentialAttached(); // Provisional → Keyed (matches web registration outcome)
      await principalStore.AddPrincipalAsync(principal);

      return tokenStore.Issue(principal.Id, scopes, TimeSpan.FromMinutes(15));
    }

    private static HttpClient CreateIsolatedClient(string? accessToken)
    {
      // Mirror TestServerApplication's loopback cert bypass so isolated clients work in CI.
      // Handler is owned by HttpClient (disposeHandler: true default) — CA2000 suppressed in preamble.
      HttpClientHandler handler = new()
      {
        ServerCertificateCustomValidationCallback = (message, _, _, errors) =>
          errors == System.Net.Security.SslPolicyErrors.None || message.RequestUri?.IsLoopback == true
      };
      HttpClient client = new(handler) { BaseAddress = Graph!.Api!.HttpClient.BaseAddress };
      if (accessToken is not null)
      {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
      }

      return client;
    }
  }
}
