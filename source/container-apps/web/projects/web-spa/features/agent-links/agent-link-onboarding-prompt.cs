#region Purpose
// Text the Agent links page gives a person to paste to their shell-capable agent.
#endregion

#region Design
// The recipe is the shipped client (task 428). A shell-capable agent finishes registration,
// token issuance, and POST of the agent-link command with openssl and curl; tools/agent-identity-cli
// is maintainer-only and is not packed, and no AOT binary is required. The signed bytes are
// UTF8(prefix) ‖ raw challenge. Prefixes come from AgentKeyProof.GetPrefix, the same strings
// Verify checks. openssl dgst -sha256 -sign emits the DER sequence the server verifies
// (Rfc3279DerSequence). Wire fields are base64url.
// Routes are the contract RouteTemplate constants. JSON member names are
// ContractSerializationDefaults' naming policy applied to nameof the contract property, so a
// rename such as Label → Nickname changes the recipe at compile time. The WASM prompt does not
// reflect JsonPropertyName: trimming would drop that lookup. The spa integration test serializes
// the contracts and fails if the recipe omits a wire name.
// The bearer lives only in the shell variable `access` and is unset before the script exits.
// Refresh is a new Token ceremony (no refresh token). The first script writes the key id and the
// link id into the directory it prints. After the person approves, the second script mints a
// fresh bearer from those files and GETs the human-ux route. The page fills in the server origin
// and the human account id before the person copies the block.
// OpenApiDocumentPath is the only OpenAPI path literal. The prompt appends it to the page origin
// and names /llms.txt and /auth.md beside it. The Playwright test reads that URL from the rendered
// prompt and requires HTTP 200 with an OpenAPI document. It also GETs the /llms.txt and /auth.md
// URLs that prompt names and requires HTTP 200. Later calls send Authorization: Bearer
// from a fresh token ceremony, with the scopes the operation needs.
// The blocks are bash and need openssl and curl. They do not run in PowerShell. Windows runs
// them in Git Bash or WSL. In Windows PowerShell, curl is an alias for Invoke-WebRequest.
// There is no second shell recipe.
// Helpers use POSIX sed (BRE groups, [[:space:]], no GNU-only flags), tr, and date -u with a
// POSIX strftime stamp. json_string drops CR as well as LF so a CRLF body still matches.
// b64url_decode writes a trailing newline before openssl base64 -d -A.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks;

using TimeWarp.Identity;

/// <summary>Paste-ready agent instructions for the optional human link ceremony.</summary>
public static class AgentLinkOnboardingPrompt
{
  /// <summary>Path of the OpenAPI document served for the page origin.</summary>
  public const string OpenApiDocumentPath = "/openapi/v1.json";

  private const string Helpers =
    """
    b64url() {
      openssl base64 -A -in "$1" | tr '+/' '-_' | tr -d '='
    }

    b64url_decode() {
      data=$1
      out=$2
      mod=$(( ${#data} % 4 ))
      pad=
      if [ "$mod" -eq 2 ]; then pad='=='; elif [ "$mod" -eq 3 ]; then pad='='; elif [ "$mod" -eq 1 ]; then echo 'challenge is not base64url' >&2; exit 1; fi
      printf '%s\n' "$data$pad" | tr '_-' '/+' | openssl base64 -d -A > "$out"
    }

    json_string() {
      printf '%s' "$1" | tr -d '\r\n' | sed -n "s/.*\"$2\"[[:space:]]*:[[:space:]]*\"\\([^\"]*\\)\".*/\\1/p"
    }

    sign_ceremony() {
      prefix=$1
      challenge_b64=$2
      sig=$3
      b64url_decode "$challenge_b64" challenge.bin
      { printf '%s' "$prefix"; cat challenge.bin; } > payload.bin
      openssl dgst -sha256 -sign agent.pem -out "$sig" payload.bin
    }
    """;

  /// <summary>Builds the prompt for one account id and server origin (no trailing slash).</summary>
  public static string Build(string accountId, string serverOrigin)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
    ArgumentException.ThrowIfNullOrWhiteSpace(serverOrigin);
    string origin = serverOrigin.TrimEnd('/');

    const string registerOptionsRoute = StartAgentKeyRegistration.Command.RouteTemplate;
    const string registerRoute = CompleteAgentKeyRegistration.Command.RouteTemplate;
    const string tokenOptionsRoute = StartAgentTokenIssuance.Command.RouteTemplate;
    const string tokenRoute = CompleteAgentTokenIssuance.Command.RouteTemplate;
    const string linkRoute = RequestAgentHumanLink.Command.RouteTemplate;
    string humanUxRoute = ShellRoute(GetHumanUx.Query.RouteTemplate, nameof(GetHumanUx.Query.LinkId), "link_id");

    string registerOptionsVerb = HttpMethod<StartAgentKeyRegistration.Command>();
    string registerVerb = HttpMethod<CompleteAgentKeyRegistration.Command>();
    string tokenOptionsVerb = HttpMethod<StartAgentTokenIssuance.Command>();
    string tokenVerb = HttpMethod<CompleteAgentTokenIssuance.Command>();
    string linkVerb = HttpMethod<RequestAgentHumanLink.Command>();
    string humanUxVerb = HttpMethod<GetHumanUx.Query>();

    string optionsChallenge = JsonMember(nameof(StartAgentKeyRegistration.Response.Challenge));
    string publicKeyName = JsonMember(nameof(CompleteAgentKeyRegistration.Command.PublicKey));
    string registerChallenge = JsonMember(nameof(CompleteAgentKeyRegistration.Command.Challenge));
    string registerSignature = JsonMember(nameof(CompleteAgentKeyRegistration.Command.Signature));
    string nicknameName = JsonMember(nameof(CompleteAgentKeyRegistration.Command.Nickname));
    string keyIdName = JsonMember(nameof(CompleteAgentKeyRegistration.Response.KeyId));
    string tokenOptionsChallenge = JsonMember(nameof(StartAgentTokenIssuance.Response.Challenge));
    string tokenKeyId = JsonMember(nameof(CompleteAgentTokenIssuance.Command.KeyId));
    string tokenChallenge = JsonMember(nameof(CompleteAgentTokenIssuance.Command.Challenge));
    string tokenSignature = JsonMember(nameof(CompleteAgentTokenIssuance.Command.Signature));
    string scopesName = JsonMember(nameof(CompleteAgentTokenIssuance.Command.Scopes));
    string accessTokenName = JsonMember(nameof(CompleteAgentTokenIssuance.Response.AccessToken));
    string humanPrincipalIdName = JsonMember(nameof(RequestAgentHumanLink.Command.HumanPrincipalId));
    string linkIdName = JsonMember(nameof(RequestAgentHumanLink.Response.LinkId));

    string registerPrefix = AgentKeyProof.GetPrefix(AgentKeyCeremonyType.Registration);
    string tokenPrefix = AgentKeyProof.GetPrefix(AgentKeyCeremonyType.TokenIssuance);
    const string identityRead = AgentScopes.IdentityRead;
    const string humanUxSpec = GetHumanUx.Response.SpecId;

    return $$"""
      Link this agent to my TimeWarp account. These blocks need bash, openssl, and curl. On Windows, run them in Git Bash or WSL. They do not run in PowerShell, and in Windows PowerShell curl is an alias for Invoke-WebRequest. Do not install a .NET SDK, and do not look for a packed client.

      My account id is {{accountId}}.
      The server origin is {{origin}}.

      Run the first bash block. It writes a P-256 private key on this machine (PKCS#8 PEM), registers the SPKI public key, mints a bearer in memory only (scope {{identityRead}}, which expands to AgentLinkManageSelf), and POSTs {{linkRoute}} for this account. It writes the key id and the link id as files in that directory and prints the directory. It does not save the bearer. Tell me when the request is pending so I can approve or deny it on the Agent links page. The signature openssl dgst -sha256 -sign writes is the DER sequence the server requires. Sign the prefix bytes immediately followed by the raw challenge bytes, not the base64url text.

      ```bash
      set -euo pipefail
      BASE='{{origin}}'
      HUMAN='{{accountId}}'
      dir="${HOME}/.timewarp-agent-link/$(date -u +%Y%m%dT%H%M%SZ)"
      mkdir -p "$dir"
      chmod 700 "$dir"
      cd "$dir"
      openssl genpkey -algorithm EC -pkeyopt ec_paramgen_curve:P-256 -out agent.pem
      chmod 600 agent.pem
      openssl pkey -in agent.pem -pubout -outform DER -out agent.spki
      printf 'key %s\n' "$dir/agent.pem"

      {{Helpers}}

      pub=$(b64url agent.spki)
      reg_json=$(curl -fsS -X {{registerOptionsVerb}} "$BASE/{{registerOptionsRoute}}" -H 'Content-Type: application/json' -d '{}')
      reg_challenge=$(json_string "$reg_json" '{{optionsChallenge}}')
      test -n "$reg_challenge"
      sign_ceremony '{{registerPrefix}}' "$reg_challenge" register.sig
      reg_sig=$(b64url register.sig)
      registered=$(curl -fsS -X {{registerVerb}} "$BASE/{{registerRoute}}" -H 'Content-Type: application/json' \
        -d "{\"{{publicKeyName}}\":\"$pub\",\"{{registerChallenge}}\":\"$reg_challenge\",\"{{registerSignature}}\":\"$reg_sig\",\"{{nicknameName}}\":\"linked-agent\"}")
      key_id=$(json_string "$registered" '{{keyIdName}}')
      test -n "$key_id"
      printf '%s\n' "$key_id" > '{{keyIdName}}'
      chmod 600 '{{keyIdName}}'
      printf 'KeyId %s\n' "$key_id"

      tok_json=$(curl -fsS -X {{tokenOptionsVerb}} "$BASE/{{tokenOptionsRoute}}" -H 'Content-Type: application/json' -d '{}')
      tok_challenge=$(json_string "$tok_json" '{{tokenOptionsChallenge}}')
      test -n "$tok_challenge"
      sign_ceremony '{{tokenPrefix}}' "$tok_challenge" token.sig
      tok_sig=$(b64url token.sig)
      token_json=$(curl -fsS -X {{tokenVerb}} "$BASE/{{tokenRoute}}" -H 'Content-Type: application/json' \
        -d "{\"{{tokenKeyId}}\":\"$key_id\",\"{{tokenChallenge}}\":\"$tok_challenge\",\"{{tokenSignature}}\":\"$tok_sig\",\"{{scopesName}}\":[\"{{identityRead}}\"]}")
      access=$(json_string "$token_json" '{{accessTokenName}}')
      test -n "$access"
      printf 'token minted\n'

      link_json=$(curl -fsS -X {{linkVerb}} "$BASE/{{linkRoute}}" \
        -H 'Content-Type: application/json' \
        -H "Authorization: Bearer $access" \
        -d "{\"{{humanPrincipalIdName}}\":\"$HUMAN\"}")
      link_id=$(json_string "$link_json" '{{linkIdName}}')
      test -n "$link_id"
      printf '%s\n' "$link_id" > '{{linkIdName}}'
      chmod 600 '{{linkIdName}}'
      printf '%s\n' "$link_json"
      printf 'dir %s\n' "$dir"
      unset access
      ```

      After I approve, export DIR to the directory the first block printed and run the second bash block. It reads the saved key id, mints a fresh token with the prefix {{tokenPrefix}} using the saved key, then GETs {{humanUxRoute}} with that new bearer. The body is the {{humanUxSpec}} handoff (GetHumanUx). Do not write the bearer to a file. A later call mints another token the same way.

      ```bash
      set -euo pipefail
      BASE='{{origin}}'
      DIR="${DIR:?set DIR to the directory the first script printed}"
      cd "$DIR"
      key_id=$(cat '{{keyIdName}}')
      link_id=$(cat '{{linkIdName}}')
      test -n "$key_id"
      test -n "$link_id"

      {{Helpers}}

      tok_json=$(curl -fsS -X {{tokenOptionsVerb}} "$BASE/{{tokenOptionsRoute}}" -H 'Content-Type: application/json' -d '{}')
      tok_challenge=$(json_string "$tok_json" '{{tokenOptionsChallenge}}')
      test -n "$tok_challenge"
      sign_ceremony '{{tokenPrefix}}' "$tok_challenge" token.sig
      tok_sig=$(b64url token.sig)
      token_json=$(curl -fsS -X {{tokenVerb}} "$BASE/{{tokenRoute}}" -H 'Content-Type: application/json' \
        -d "{\"{{tokenKeyId}}\":\"$key_id\",\"{{tokenChallenge}}\":\"$tok_challenge\",\"{{tokenSignature}}\":\"$tok_sig\",\"{{scopesName}}\":[\"{{identityRead}}\"]}")
      access=$(json_string "$token_json" '{{accessTokenName}}')
      test -n "$access"
      ux_json=$(curl -fsS -X {{humanUxVerb}} "$BASE/{{humanUxRoute}}" \
        -H "Authorization: Bearer $access")
      printf '%s\n' "$ux_json"
      unset access
      ```

      The OpenAPI document at {{origin}}{{OpenApiDocumentPath}} describes every endpoint, request, and response. Call an operation with Authorization: Bearer and a token from a fresh token ceremony (the second block), using the scopes that operation requires. Discovery notes are {{origin}}/llms.txt and {{origin}}/auth.md.
      """;
  }

  private static string HttpMethod<TRequest>() where TRequest : IApiRequest, new()
  {
    return new TRequest().GetHttpVerb().ToString().ToUpperInvariant();
  }

  private static string JsonMember(string propertyName)
  {
    JsonNamingPolicy? namingPolicy = ContractSerializationDefaults.Options.PropertyNamingPolicy;
    if (namingPolicy is null)
    {
      throw new InvalidOperationException("Contract serialization has no property naming policy.");
    }

    return namingPolicy.ConvertName(propertyName);
  }

  private static string ShellRoute(string routeTemplate, string parameterName, string shellVariable)
  {
    string opening = "{" + parameterName;
    int start = routeTemplate.IndexOf(opening, StringComparison.Ordinal);
    if (start < 0)
    {
      throw new InvalidOperationException($"Route '{routeTemplate}' has no {{{parameterName}}} parameter.");
    }

    int end = routeTemplate.IndexOf('}', start, StringComparison.Ordinal);
    if (end < start)
    {
      throw new InvalidOperationException($"Route '{routeTemplate}' has an unclosed parameter.");
    }

    return string.Concat(
      routeTemplate[..start],
      "${",
      shellVariable,
      "}",
      routeTemplate[(end + 1)..]);
  }
}
