# Serialize contract enums as strings across the web-contracts seam

## Description

`ContractSerializationDefaults` has no `JsonStringEnumConverter`, so every enum crossing the
contract seam serializes as a raw integer. `GetAgentIdentity.Response` emits `Kind`/`TrustTier`
(PrincipalKind/TrustTier) as numbers today; the 104-029 demo CLI had to model them as the enum
types and rely on numeric wire parsing (a review caught it modeling them as strings, which would
have broken after HTTP 200 — evidence the numeric shape is a footgun for external consumers).

Numeric enums as a public API commitment are fragile: any renumbering silently breaks every
consumer, and `2` is not self-describing to an agent author reading discovery docs (104-017).
Decide the seam's enum representation now, while the only external consumer is our own CLI.

## Requirements

- **Decision: strings, not integers** (already leaned; confirm at plan). Add
  `JsonStringEnumConverter` to `ContractSerializationDefaults` (the single seam-options source —
  never declare seam options inline, per AGENTS.md) so enums serialize as member names
  (`"Agent"`, `"Keyed"`, `"Passkey"`).
- **Keep them PLAIN C# enums** — do NOT adopt the `foundation-domain/enumeration/enumeration.cs`
  Bogard class for these. Rationale (same as the session's earlier Enumeration review): they are
  pure discriminators with no per-member behavior/data; adopting Enumeration would pull the
  foundation dependency into identity's public contract AND reintroduce a serialization problem
  (Enumeration has no STJ converter either). The Enumeration class is for members that carry
  behavior/data (e.g. CorsPolicy) — see task 105.
- **Fail closed on unknown values.** Reserved-zero `None` posture already exists on these enums;
  deserializing an unknown string must not silently map to `None` or throw an unhandled 500 —
  reject as a validation-shaped error. Confirm `JsonStringEnumConverter` behavior on unknown
  input and add a converter/guard if the default is too permissive.
- **Round-trip integrity across the seam**: web-contracts-tests must prove `"Agent"` ⇄
  PrincipalKind.Agent both directions for every contract-facing enum (PrincipalKind, TrustTier,
  CredentialType if it ever surfaces in a response).
- **Update the 104-029 CLI** wire DTOs/tests if the string switch changes what it parses (its
  `whoami-wire-tests` fixture pins the numeric shape today — it must track this change).
- **Blast radius check**: grep every enum that appears in a web-contracts request/response
  (roles, profile, etc.), not just identity — this is a seam-wide format change. Verify no
  existing SPA/client code depends on the numeric form.
- **Prefer-analyzers follow-on (evaluate, don't necessarily build here):** if the team instead
  wanted to KEEP numeric, the safe version needs a TWA rule forbidding enum-member renumbering in
  web-contracts (an API-stability guard). Since we're going strings, note this as the rejected
  alternative's cost rather than implementing it.

## Checklist

- [x] Confirm strings-over-integers at plan; record rejected numeric+renumber-guard alternative
- [x] JsonStringEnumConverter in ContractSerializationDefaults (seam-wide)
- [x] Fail-closed unknown-value handling (verify default, guard if needed)
- [x] web-contracts-tests round-trips for every contract-facing enum
- [x] Update 104-029 CLI wire DTOs + whoami-wire-tests to the string shape
- [x] Blast-radius grep (all web-contracts enums + SPA consumers); no numeric dependency remains
- [x] dev build 0/0; dev test

## Notes

- Origin: 104-029 review (numeric wire shape) + this session's Enumeration discussion (why NOT
  the Enumeration class). Related: task 105 (Enumeration hardening — the legitimate home for the
  Bogard pattern), task 104-017 (discovery docs, which want self-describing enum values).
- Keep it a pure format decision on the seam — no domain enum changes.

## Session

- Created: 2026-07-20

### Implementation plan (108)

#### Decision
- Wire: **PascalCase strings** (`"Agent"`, `"Keyed"`) via `JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false)`
- Plain C# enums only (not Bogard Enumeration)
- Rejected: keep integers + TWA renumber analyzer

#### Critical
Server currently does NOT use ContractSerializationDefaults — must Apply on MVC JsonOptions + HttpJsonOptions in CommonServerModule or client-only change splits wire.

#### Steps
1. ContractSerializationDefaults.Apply — converter + Design
2. CommonServerModule — Configure JsonOptions + ConfigureHttpJsonOptions
3. web-contracts-tests identity serialization — wire text + fail-closed + CredentialType
4. Agent_Protected_Endpoint_Tests raw JSON assert strings
5. CLI CliJson + whoami-wire-tests string fixture
6. Optional skill one-liner
7. dev build + targeted tests

## Session
- Started: 2026-07-20 (tw-orchestrate-task 108)
- Plan: 2026-07-20

## Results

### Summary
Contract enums now serialize as PascalCase member-name strings across the seam (`JsonStringEnumConverter`, `allowIntegerValues: false`). Server hosts Apply the same options (MVC + HttpJson). CLI and tests updated. Integers and unknown names fail closed with `JsonException`.

### Files changed
| Path | Change |
|------|--------|
| `foundation-contracts/.../contract-serialization-defaults.cs` | converter + Design |
| `foundation-server/common-server-module.cs` | Apply on JsonOptions + HttpJsonOptions |
| `web-contracts-tests/.../identity-contracts-serialization-tests.cs` | wire text + fail-closed + CredentialType |
| `web-server-integration-tests/.../Agent_Protected_Endpoint_Tests.cs` | raw JSON string asserts |
| `tools/agent-identity-cli/services/cli-json.cs` | same converter |
| `tools/agent-identity-cli/services/agent-wire-dtos.cs` | Design: strings |
| `tests/tools/.../whoami-wire-tests.cs` | string fixture + numeric reject |
| `skills/web-api-contracts/SKILL.md` | one-line note |

### Key decisions
- Strings over integers; PascalCase names; plain enums (not Bogard Enumeration)
- Rejected: keep integers + TWA renumber analyzer
- Server must Apply — client-only would split wire
- STJ unknown strings → JsonException; case-insensitive read is STJ default (documented)

### Build / tests
- `dev build`: 0/0 (implementer)
- web-contracts-tests: 26 passed
- agent-identity-cli-tests: 11 passed
- Agent protected endpoint tests: 5 passed

### Review
- Effort 1 general; disposition **clean**
- Paths: `review/review-framework.md`, `review/round-1/{general,merged}.md`, `review/disposition.md`

### Follow-up (not this task)
String-enum Apply lives in `CommonServerModule` → web-server (MVC) is covered and integration-tested.
FastEndpoints on **api-server** is expected to inherit `ConfigureHttpJsonOptions`, but nothing proves
it and no enum currently crosses that seam. Captured on **104-030**: when api-server gets agent
bearer validation, also verify PascalCase string-enum wire shape through FastEndpoints.

## Session
- Started / plan / implement / review: 2026-07-20 (tw-orchestrate-task 108)
- Follow-up 104-030 noted: 2026-07-20
