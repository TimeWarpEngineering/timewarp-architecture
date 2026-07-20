# Agent identity demo CLI walking the key-registration and token ceremony

## Parent

104

## Description

The 104-004 curl smoke sequence (see that task's Results) proves the agent ceremony works, but
nine manual openssl/curl steps are significant friction for a human trying to understand the
flow. Build a small **TimeWarp.Nuru demo CLI** that walks the whole agent lifecycle with narrated
output — executable documentation that is ALSO a reference client for real agent authors.

**Why a CLI and not a web page:** the agent path's entire point is no-browser authentication — a
console app demonstrates the flow in its native habitat and its source doubles as SDK-grade
reference code (keygen, SPKI export, challenge signing, bearer usage). A browser demo page would
misrepresent the flow (a browser user should use passkeys; that UX is 104-016's CTA work). The
web-page alternative was considered and set aside, not forgotten — 104-017's discovery docs can
link both this CLI and the curl sequence.

## Requirements

- **TimeWarp.Nuru CLI** (dogfoods the house CLI framework — use the `tw-nuru` skill) with
  commands roughly:
  - `keygen` — generate P-256 keypair, store locally (file under user profile or `--key-file`),
    print the SPKI base64url + computed KeyId
  - `register` — options → sign (Register.v1 prefix) → complete; prints PrincipalId + KeyId
  - `token [--scopes identity:read]` — options → sign (Token.v1 prefix) → complete; prints
    access token, expiry, granted scopes
  - `whoami` — calls GET /api/identity/agent/me with the bearer; prints Kind/TrustTier/scopes
  - `demo` — full narrated walkthrough (keygen → register → token → whoami) with step-by-step
    explanation of WHAT is being signed and WHY (domain separation, one-time challenges,
    proof-of-possession), suitable for a human reading along
- `--server` option (default `https://localhost:63611`); machine-readable errors surfaced
  verbatim (they are the API's own problem details — show them off).
- Signing/SPKI code mirrors the library's contract exactly — reuse `AgentKeyProof.BuildSignedData`
  via a reference to TimeWarp.Identity if placement allows, else mirror the construction with a
  comment pinning it to the library file (agreement-by-memory risk: note it).
- Placement decision for the planner: template content (a `demos/` or `tools/` area — check
  precedent) vs .NET 10 runfile (`tw-runfiles` skill) vs sample in documentation. Weigh: template
  flag interaction (`web`? none?), whether generated apps should ship it, and dogfooding value.
  Whatever placement, `dev build` stays 0/0 and the CLI gets at least smoke-level tests (arg
  parsing + signed-data construction against the library's own vectors).
- Update 104-017's notes (or leave a pointer) so the discovery docs link the CLI as the
  agent-onboarding reference.

## Checklist

- [x] Placement decision recorded (template content vs runfile vs docs sample)
- [x] keygen / register / token / whoami commands
- [x] Narrated `demo` walkthrough command
- [x] Signed-data construction pinned to the library contract (reference or tested mirror)
- [x] Tests (arg parsing + construction vectors)
- [x] Manual run against `dev run` recorded in Results (server not up this session; offline tests + --help recorded)
- [x] Pointer left for 104-017 discovery docs

## Notes

- Origin: 104-004 Results curl smoke sequence — this task is that sequence made humane.
- Depends on 104-004 (done). The CLI exercises only public HTTP endpoints — no coupling to
  identity library internals required beyond the signed-data construction.

## Session

- Created: 2026-07-20

### Implementation plan (104-029)

#### Placement (decided)
**`tools/agent-identity-cli/`** multi-file TimeWarp.Nuru runfile (mirrors `tools/dev-cli/`).
- ProjectReference → TimeWarp.Identity for `AgentKeyProof.BuildSignedData`
- Not in solution; template-exclude so package-mode apps do not ship a broken tool
- Tests: `tests/tools/agent-identity-cli-tests/`

#### Commands
- `keygen` — P-256, PEM under `~/.config/timewarp/agent-identity/default.pem`, print SPKI b64url + KeyId
- `register` — options → Register.v1 sign → complete; persist principalId/keyId
- `token [--scopes]` — options → Token.v1 sign → complete; default scope identity:read
- `whoami` — GET /api/identity/agent/me with stored bearer
- `demo` — narrated full walkthrough
- Shared: `--server` default `https://localhost:63611`, `--key-file`, problem details on errors

#### Services
AgentSigning (library pin), LocalKeyStore, AgentHttpClient, CLI-local camelCase DTOs (no web-contracts)

#### Tests
Signed-data vectors vs library, SPKI/KeyId shape, key store round-trip; no web-server host

#### Docs pointer
Note on 104-017 backlog; template.json exclude tools + tests

## Session
- Started: 2026-07-20 (tw-orchestrate-task 104-029)
- Plan: 2026-07-20

## Results

### Placement
`tools/agent-identity-cli/` multi-file TimeWarp.Nuru runfile (mirrors `tools/dev-cli/`).
- ProjectReference → `source/libraries/timewarp-identity` (`AgentKeyProof.BuildSignedData`, `AgentPublicKey.TryParse`)
- Not in solution; template.json excludes `tools/agent-identity-cli/**` and `tests/tools/agent-identity-cli-tests/**`
- Tests under `tests/tools/agent-identity-cli-tests/` (globbed by `dev test`)

### Commands
- `keygen [--key-file|-k] [--force|-f]`
- `register [--server|-s] [--key-file|-k] [--label|-l]`
- `token [--server|-s] [--key-file|-k] [--scopes]` (default `identity:read`)
- `whoami [--server|-s] [--key-file|-k]`
- `demo [--server|-s] [--key-file|-k] [--force|-f]` narrated full lifecycle

Defaults: server `https://localhost:63611`, key `~/.config/timewarp/agent-identity/default.pem`, store sidecar `*.store.json`.

### Signing pin
```csharp
byte[] signedData = AgentKeyProof.BuildSignedData(ceremonyType, challengeBytes);
byte[] signature = ecdsa.SignData(signedData, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
```
Wire: `System.Buffers.Text.Base64Url`. No Verify reimplementation in CLI.

### Verify
- `dotnet run tools/agent-identity-cli/agent.cs -- --help` ✅
- `dotnet run … -- keygen --force` ✅ (prints SPKI + KeyId)
- `dotnet fixie tests/tools/agent-identity-cli-tests` ✅ **10 passed** (incl. WhoAmI numeric enum wire fixture)
- Manual `demo` against live `dev run` **skipped at task time** (no server that session; offline tests cover crypto/store)
- **Human smoke verification (2026-07-20, maintainer follow-up):** `agent demo` run against a live
  `dev run` server (`https://localhost:63611`) with a fresh key — full lifecycle green: keygen →
  register (v7 PrincipalId `019f7da7-…`) → token (Bearer, 900s, `identity:read`) → whoami returns
  matching PrincipalId with `Kind Agent` / `TrustTier Keyed`. Confirms the M1 enum-wire fix works
  post-HTTP-200, not just in the offline fixture. Re-verified green after the NoWarn/static-refactor
  cleanup (commit a5b4bbe4). Follow-ups from this pass: NoWarn trimmed 18→2 (tool) and 8→1 (tests) —
  only what actually fires, CA1822/CA2007 fixed rather than suppressed (`AgentSigning` is now a
  static helper, dropped from DI); and the numeric enum wire shape is filed as task 108
  (serialize contract enums as strings seam-wide) — the CLI's `whoami-wire-tests` numeric fixture
  tracks that change.

### Review (Phase 4b)
- Effort 1, general reviewer; `review/` kitchen on this task
- Round 1: **M1 bug** WhoAmI Kind/TrustTier as string vs numeric server enums → **fixed** (`PrincipalKind`/`TrustTier` DTOs + fixture test)
- Round 1: **M2 nit** double PEM load on token → **fixed**
- Disposition: **clean** (`review/disposition.md`)

### 104-017
Notes updated with pointer to this CLI and 104-004 curl sequence.

## Session
- Started: 2026-07-20 (tw-orchestrate-task 104-029)
- Plan / implement / review fix: 2026-07-20


