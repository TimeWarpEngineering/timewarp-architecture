#region Purpose
// Text the Agent links page gives a person to paste to their shell-capable agent.
#endregion

#region Design
// The recipe is the shipped client (task 428). A shell-capable agent finishes registration,
// token issuance, and POST api/agent-links with openssl and curl; tools/agent-identity-cli is
// maintainer-only and is not packed, and no AOT binary is required. The signed bytes are
// UTF8(prefix) ‖ raw challenge, and openssl dgst -sha256 -sign emits the DER sequence the
// server verifies (Rfc3279DerSequence). Wire fields are base64url. The page fills in the
// server origin and the human account id before the person copies the block.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks;

/// <summary>Paste-ready agent instructions for the optional human link ceremony.</summary>
public static class AgentLinkOnboardingPrompt
{
  /// <summary>Builds the prompt for one account id and server origin (no trailing slash).</summary>
  public static string Build(string accountId, string serverOrigin)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
    ArgumentException.ThrowIfNullOrWhiteSpace(serverOrigin);
    string origin = serverOrigin.TrimEnd('/');

    return $$"""
      Link this agent to my TimeWarp account. Use openssl and curl only. Do not install a .NET SDK, and do not look for a packed client.

      My account id is {{accountId}}.
      The server origin is {{origin}}.

      Run the bash below. It writes a P-256 private key on this machine (PKCS#8 PEM), registers the SPKI public key, mints a bearer whose scope is identity:read (that scope expands to AgentLinkManageSelf), and POSTs api/agent-links for this account. Tell me when the request is pending so I can approve or deny it on the Agent links page. After I approve, GET {{origin}}/api/agent-links/<linkId>/human-ux with the same bearer. That body is the timewarp.humanUx/v1 handoff document. Keep the private key; a later token is the same ceremony with the Token.v1 prefix. The signature openssl dgst -sha256 -sign writes is the DER sequence the server requires. Sign the prefix bytes immediately followed by the raw challenge bytes, not the base64url text.

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

      b64url() {
        openssl base64 -A -in "$1" | tr '+/' '-_' | tr -d '='
      }

      b64url_decode() {
        data=$1
        out=$2
        mod=$(( ${#data} % 4 ))
        pad=
        if [ "$mod" -eq 2 ]; then pad='=='; elif [ "$mod" -eq 3 ]; then pad='='; elif [ "$mod" -eq 1 ]; then echo 'challenge is not base64url' >&2; exit 1; fi
        printf '%s' "$data$pad" | tr '_-' '/+' | openssl base64 -d -A > "$out"
      }

      json_string() {
        printf '%s' "$1" | tr -d '\n' | sed -n "s/.*\"$2\"[[:space:]]*:[[:space:]]*\"\\([^\"]*\\)\".*/\\1/p"
      }

      sign_ceremony() {
        prefix=$1
        challenge_b64=$2
        sig=$3
        b64url_decode "$challenge_b64" challenge.bin
        { printf '%s' "$prefix"; cat challenge.bin; } > payload.bin
        openssl dgst -sha256 -sign agent.pem -out "$sig" payload.bin
      }

      pub=$(b64url agent.spki)
      reg_json=$(curl -fsS -X POST "$BASE/api/identity/agent/register/options" -H 'Content-Type: application/json' -d '{}')
      reg_challenge=$(json_string "$reg_json" challenge)
      test -n "$reg_challenge"
      sign_ceremony 'TimeWarp.Identity.AgentKey.Register.v1:' "$reg_challenge" register.sig
      reg_sig=$(b64url register.sig)
      registered=$(curl -fsS -X POST "$BASE/api/identity/agent/register" -H 'Content-Type: application/json' \
        -d "{\"publicKey\":\"$pub\",\"challenge\":\"$reg_challenge\",\"signature\":\"$reg_sig\",\"nickname\":\"linked-agent\"}")
      key_id=$(json_string "$registered" keyId)
      test -n "$key_id"
      printf 'KeyId %s\n' "$key_id"

      tok_json=$(curl -fsS -X POST "$BASE/api/identity/agent/token/options" -H 'Content-Type: application/json' -d '{}')
      tok_challenge=$(json_string "$tok_json" challenge)
      test -n "$tok_challenge"
      sign_ceremony 'TimeWarp.Identity.AgentKey.Token.v1:' "$tok_challenge" token.sig
      tok_sig=$(b64url token.sig)
      token_json=$(curl -fsS -X POST "$BASE/api/identity/agent/token" -H 'Content-Type: application/json' \
        -d "{\"keyId\":\"$key_id\",\"challenge\":\"$tok_challenge\",\"signature\":\"$tok_sig\",\"scopes\":[\"identity:read\"]}")
      access=$(json_string "$token_json" accessToken)
      test -n "$access"

      link_json=$(curl -fsS -X POST "$BASE/api/agent-links" \
        -H 'Content-Type: application/json' \
        -H "Authorization: Bearer $access" \
        -d "{\"humanPrincipalId\":\"$HUMAN\"}")
      printf '%s\n' "$link_json"
      ```
      """;
  }
}
