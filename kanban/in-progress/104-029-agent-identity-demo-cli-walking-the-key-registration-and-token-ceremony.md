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

- [ ] Placement decision recorded (template content vs runfile vs docs sample)
- [ ] keygen / register / token / whoami commands
- [ ] Narrated `demo` walkthrough command
- [ ] Signed-data construction pinned to the library contract (reference or tested mirror)
- [ ] Tests (arg parsing + construction vectors)
- [ ] Manual run against `dev run` recorded in Results
- [ ] Pointer left for 104-017 discovery docs

## Notes

- Origin: 104-004 Results curl smoke sequence — this task is that sequence made humane.
- Depends on 104-004 (done). The CLI exercises only public HTTP endpoints — no coupling to
  identity library internals required beyond the signed-data construction.

## Session

- Created: 2026-07-20
