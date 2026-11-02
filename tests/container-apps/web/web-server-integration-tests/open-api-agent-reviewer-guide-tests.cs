#region Purpose
// The generated OpenAPI document names how an agent obtains a token and how a reviewer marks feedback processed.
#endregion

#region Design
// The full web host, not the probe host beside this file. The probe host has no ceremony
// endpoints and does not bind AgentTokenOptions, so it cannot prove the lifetime or the
// list-feedback description. This host's appsettings sets a lifetime other than the C#
// default, and the assertion reads that configured value. Ceremony descriptions come from
// the contract remarks. The signing prefixes are read from AgentKeyProof so a copied
// string that drifts fails here.
#endregion

namespace OpenApiAgentReviewerGuide_;

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TimeWarp.Identity;

[TestTag("OpenApi")]
public class Document_Given_
{
  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Document_Given_>();

  public static async Task SetupOnce()
  {
#if(api)
    Graph = await HostGraphFactory.CreateWebWithApiAsync();
#else
    Graph = await HostGraphFactory.CreateWebAsync();
#endif
  }

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  public static async Task Scheme_Should_NameIssuanceStepsAndTheConfiguredLifetime()
  {
    using JsonDocument document = await DocumentAsync();
    string description = document.RootElement
      .GetProperty("components")
      .GetProperty("securitySchemes")
      .GetProperty("JWTBearerAuth")
      .GetProperty("description")
      .GetString()!;

    int at = -1;
    foreach (string step in new[]
    {
      "start-agent-key-registration",
      "complete-agent-key-registration",
      "start-agent-token-issuance",
      "complete-agent-token-issuance",
    })
    {
      int next = description.IndexOf(step, StringComparison.Ordinal);
      next.ShouldBeGreaterThan(at);
      at = next;
    }

    int lifetimeMinutes = Web.WebApplicationHost.ServiceProvider
      .GetRequiredService<IOptions<AgentTokenOptions>>()
      .Value
      .TokenLifetimeMinutes;
    description.ShouldContain($"{lifetimeMinutes} minutes");
    description.ShouldContain("no refresh token");
    description.ShouldContain("Reissue with the agent key before the token expires, or after a 401 invalid_token.");
  }

  public static async Task Issuance_Should_SayWhatTheClientSignsAndReturns()
  {
    using JsonDocument document = await DocumentAsync();
    JsonElement paths = document.RootElement.GetProperty("paths");
    string registration = AgentKeyProof.GetPrefix(AgentKeyCeremonyType.Registration);
    string issuance = AgentKeyProof.GetPrefix(AgentKeyCeremonyType.TokenIssuance);

    string startRegistration = Description(paths, "/api/identity/agent/register/options", "post");
    startRegistration.ShouldContain(registration);
    startRegistration.ShouldContain("ECDSA P-256");
    startRegistration.ShouldContain("sends back");

    string completeRegistration = Description(paths, "/api/identity/agent/register", "post");
    completeRegistration.ShouldContain(registration);
    completeRegistration.ShouldContain("PublicKey");
    completeRegistration.ShouldContain("Signature");
    completeRegistration.ShouldContain("KeyId");

    string startIssuance = Description(paths, "/api/identity/agent/token/options", "post");
    startIssuance.ShouldContain(issuance);
    startIssuance.ShouldContain("ECDSA P-256");
    startIssuance.ShouldContain("sends back");

    string completeIssuance = Description(paths, "/api/identity/agent/token", "post");
    completeIssuance.ShouldContain(issuance);
    completeIssuance.ShouldContain("Signature");
    completeIssuance.ShouldContain("AccessToken");
    completeIssuance.ShouldContain("no refresh token");
  }

  public static async Task ListFeedback_Should_NameNewAndTheProcessedMarker()
  {
    using JsonDocument document = await DocumentAsync();
    string description = Description(document.RootElement.GetProperty("paths"), "/api/admin/feedback", "get");
    description.ShouldContain("Status = New");
    description.ShouldContain("processed marker");
    description.ShouldContain("off New");
  }

  private static string Description(JsonElement paths, string path, string method) =>
    paths.GetProperty(path).GetProperty(method).GetProperty("description").GetString()!;

  private static async Task<JsonDocument> DocumentAsync()
  {
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    using HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    return JsonDocument.Parse(json);
  }
}
