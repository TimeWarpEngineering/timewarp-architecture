# Round 1 — general
**Date:** 2026-07-20
**Scope reviewed:** tools/agent-identity-cli + tests

## Summary

The CLI meets the structural requirements: Nuru commands `keygen` / `register` / `token` / `whoami` / `demo`; default server `https://localhost:63611`; signing is pinned to `AgentKeyProof.BuildSignedData` + SHA-256 DER (`Rfc3279DerSequence`) with no private signed-data mirror; wire bytes use `System.Buffers.Text.Base64Url`; non-2xx responses print status + raw body (problem details); private key PEM is never printed or stored in the sidecar; only `TimeWarp.Identity` is referenced (no web-contracts); template.json excludes both tool and test trees; unit tests cover domain-separated prefixes, CLI `Sign` → `AgentKeyProof.Verify` success, and cross-ceremony verify failure.

One wire-shape defect will break `whoami` / `demo` step 4 after a successful HTTP 200: `WhoAmIResponse` models `Kind` and `TrustTier` as `string`, while the server emits numeric enums under the same camelCase options used everywhere else on the contract seam.

## Issues

### Issue 1 — Severity: bug
- File: tools/agent-identity-cli/services/agent-wire-dtos.cs:49-55
- Description: `WhoAmIResponse` declares `Kind` and `TrustTier` as `string`. Server `GetAgentIdentity.Response` exposes `PrincipalKind` / `TrustTier` enums; ASP.NET Core STJ (and `ContractSerializationDefaults`, which only set camelCase) serialize enums as numbers. Example body: `{"principalId":"…","kind":2,"trustTier":2,"scopes":["identity:read"]}`. `JsonSerializer.Deserialize<WhoAmIResponse>` throws `JsonException` on number→string, so `AgentHttpClient.ParseResult` never returns success and `whoami` / `demo` surface a deserializer error after a valid bearer response. Integration tests already prove the wire shape by deserializing into the real contract types (`Agent_Protected_Endpoint_Tests`).
- Suggestion: Align with the wire types already available via the Identity reference — use `PrincipalKind` and `TrustTier` (or `int` / `JsonElement` if keeping DTOs free of domain enums) — and print them with `.ToString()`. Add a small offline round-trip test that deserializes a numeric-kind fixture JSON through `CliJson` into `WhoAmIResponse`.
- Status: open

### Issue 2 — Severity: nit
- File: tools/agent-identity-cli/endpoints/token-command.cs:48-58
- Description: When the sidecar lacks `KeyId`, the handler calls `Signing.LoadKey` once for the fallback id, then loads the same PEM again for signing.
- Suggestion: Load once, compute `keyId` from that instance, and reuse it for `Sign`.
- Status: open
