#region Purpose
// The agent-link paste recipe must keep agreeing with the contracts, and the follow-up
// human-ux step must mint a fresh bearer instead of reusing one the first script discarded.
#endregion

#region Design
// No host. Build() is pure. Wire names come from serializing the contract types with
// ContractSerializationDefaults, not from literals, so a rename such as Label → Nickname fails
// this test until the recipe follows. Routes and ceremony prefixes are the contract and
// AgentKeyProof surfaces. The bearer is checked by absence: the recipe never redirects it
// into a file and it unsets the shell variable before each block exits.
// The OpenAPI paragraph uses AgentLinkOnboardingPrompt.OpenApiDocumentPath, names bearer calls,
// and names /llms.txt and /auth.md. The shell requirement says the blocks are bash, need
// openssl and curl, run on Windows in Git Bash or WSL, and do not run in PowerShell. The curl
// alias remark is limited to Windows PowerShell. b64url_decode is pinned by its full line.
#endregion

namespace AgentLinkOnboardingPrompt_;

using System.Text.Json;
using TimeWarp.Architecture.Features.AgentLinks;
using TimeWarp.Foundation.Features;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Foundation.Types;
using TimeWarp.Identity;

public class Build_Should
{
  private const string Account = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
  private const string Origin = "https://links.example";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Build_Should>();

  public static Task FollowTheContracts_And_MintAFreshTokenAfterApproval()
  {
    string prompt = AgentLinkOnboardingPrompt.Build(Account, Origin);

    prompt.ShouldContain(Account);
    prompt.ShouldContain(Origin);
    prompt.ShouldContain("openssl genpkey -algorithm EC -pkeyopt ec_paramgen_curve:P-256");
    prompt.ShouldContain("openssl dgst -sha256 -sign");
    prompt.ShouldNotContain("tools/");
    prompt.ShouldNotContain("dotnet");
    prompt.ShouldNotContain("same bearer");

    prompt.ShouldContain(AgentKeyProof.GetPrefix(AgentKeyCeremonyType.Registration));
    prompt.ShouldContain(AgentKeyProof.GetPrefix(AgentKeyCeremonyType.TokenIssuance));
    prompt.ShouldContain(AgentScopes.IdentityRead);
    prompt.ShouldContain(GetHumanUx.Response.SpecId);

    AssertRoute(prompt, new StartAgentKeyRegistration.Command(), StartAgentKeyRegistration.Command.RouteTemplate, occurrences: 1);
    AssertRoute(prompt, new CompleteAgentKeyRegistration.Command(), CompleteAgentKeyRegistration.Command.RouteTemplate, occurrences: 1);
    AssertRoute(prompt, new StartAgentTokenIssuance.Command(), StartAgentTokenIssuance.Command.RouteTemplate, occurrences: 2);
    AssertRoute(prompt, new CompleteAgentTokenIssuance.Command(), CompleteAgentTokenIssuance.Command.RouteTemplate, occurrences: 2);
    AssertRoute(prompt, new RequestAgentHumanLink.Command(), RequestAgentHumanLink.Command.RouteTemplate, occurrences: 1);

    const string humanUx = GetHumanUx.Query.RouteTemplate;
    int open = humanUx.IndexOf('{', StringComparison.Ordinal);
    int close = humanUx.IndexOf('}', StringComparison.Ordinal);
    open.ShouldBeGreaterThanOrEqualTo(0);
    close.ShouldBeGreaterThan(open);
    string shellPath = string.Concat(humanUx.AsSpan(0, open), "${link_id}", humanUx.AsSpan(close + 1));
    Count(prompt, $"-X {new GetHumanUx.Query().GetHttpVerb().ToString().ToUpperInvariant()} \"$BASE/{shellPath}\"").ShouldBe(1);
    prompt.ShouldNotContain("{LinkId");

    AssertCommandMembers(prompt, new CompleteAgentKeyRegistration.Command
    {
      PublicKey = "public-key",
      Challenge = "register-challenge",
      Signature = "register-signature",
      Nickname = "linked-agent",
    });
    AssertCommandMembers(prompt, new CompleteAgentTokenIssuance.Command
    {
      KeyId = "key-id",
      Challenge = "token-challenge",
      Signature = "token-signature",
      Scopes = [AgentScopes.IdentityRead],
    });
    AssertCommandMembers(prompt, new RequestAgentHumanLink.Command
    {
      HumanPrincipalId = Guid.Parse(Account),
    });

    string keyIdName = WireName(
      new CompleteAgentKeyRegistration.Response(PrincipalId.New(), "key-id-sentinel"),
      "key-id-sentinel");
    string linkIdName = WireName(
      new RequestAgentHumanLink.Response(Guid.Parse("11111111-2222-3333-4444-555555555555"), "Pending"),
      "11111111-2222-3333-4444-555555555555");
    string accessTokenName = WireName(
      new CompleteAgentTokenIssuance.Response("access-sentinel", 60, [AgentScopes.IdentityRead], PrincipalId.New()),
      "access-sentinel");

    prompt.ShouldContain($"> '{keyIdName}'");
    prompt.ShouldContain($"> '{linkIdName}'");
    prompt.ShouldContain($"cat '{keyIdName}'");
    prompt.ShouldContain($"cat '{linkIdName}'");
    prompt.ShouldContain($"'{accessTokenName}'");
    prompt.ShouldNotContain($"> '{accessTokenName}'");
    prompt.ShouldContain("mints a fresh token");
    Count(prompt, "unset access").ShouldBe(2);
    prompt.ShouldContain("These blocks need bash, openssl, and curl.");
    prompt.ShouldContain("On Windows, run them in Git Bash or WSL.");
    prompt.ShouldContain("They do not run in PowerShell");
    prompt.ShouldContain("in Windows PowerShell curl is an alias for Invoke-WebRequest");
    prompt.ShouldContain(Origin + AgentLinkOnboardingPrompt.OpenApiDocumentPath);
    prompt.ShouldContain("describes every endpoint, request, and response");
    prompt.ShouldContain("Authorization: Bearer");
    prompt.ShouldContain("fresh token ceremony");
    prompt.ShouldContain(Origin + "/llms.txt");
    prompt.ShouldContain(Origin + "/auth.md");
    prompt.ShouldContain("tr -d '\\r\\n'");
    prompt.ShouldContain("printf '%s\\n' \"$data$pad\" | tr '_-' '/+' | openssl base64 -d -A");

    return Task.CompletedTask;
  }

  private static void AssertRoute<TRequest>(string prompt, TRequest request, string routeTemplate, int occurrences)
    where TRequest : IApiRequest
  {
    string verb = request.GetHttpVerb().ToString().ToUpperInvariant();
    Count(prompt, $"-X {verb} \"$BASE/{routeTemplate}\"").ShouldBe(occurrences);
  }

  private static void AssertCommandMembers<TCommand>(string prompt, TCommand command)
  {
    foreach (string name in JsonNames(command))
    {
      prompt.ShouldContain(name);
    }
  }

  private static string WireName<T>(T value, string sentinel)
  {
    using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(value, ContractSerializationDefaults.Options));
    foreach (JsonProperty property in document.RootElement.EnumerateObject())
    {
      if (property.Value.ValueKind == JsonValueKind.String && property.Value.GetString() == sentinel)
      {
        return property.Name;
      }
    }

    throw new InvalidOperationException($"Serialized {typeof(T).Name} has no string member equal to the sentinel.");
  }

  private static List<string> JsonNames<T>(T value)
  {
    using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(value, ContractSerializationDefaults.Options));
    List<string> names = [];
    foreach (JsonProperty property in document.RootElement.EnumerateObject())
    {
      names.Add(property.Name);
    }

    names.Count.ShouldBeGreaterThan(0);
    return names;
  }

  private static int Count(string text, string value)
  {
    int count = 0;
    int start = 0;
    while (start < text.Length)
    {
      int found = text.IndexOf(value, start, StringComparison.Ordinal);
      if (found < 0)
      {
        return count;
      }

      count++;
      start = found + value.Length;
    }

    return count;
  }
}
