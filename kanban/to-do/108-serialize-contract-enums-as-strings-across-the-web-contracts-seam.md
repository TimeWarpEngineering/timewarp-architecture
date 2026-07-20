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

- [ ] Confirm strings-over-integers at plan; record rejected numeric+renumber-guard alternative
- [ ] JsonStringEnumConverter in ContractSerializationDefaults (seam-wide)
- [ ] Fail-closed unknown-value handling (verify default, guard if needed)
- [ ] web-contracts-tests round-trips for every contract-facing enum
- [ ] Update 104-029 CLI wire DTOs + whoami-wire-tests to the string shape
- [ ] Blast-radius grep (all web-contracts enums + SPA consumers); no numeric dependency remains
- [ ] dev build 0/0; dev test

## Notes

- Origin: 104-029 review (numeric wire shape) + this session's Enumeration discussion (why NOT
  the Enumeration class). Related: task 105 (Enumeration hardening — the legitimate home for the
  Bogard pattern), task 104-017 (discovery docs, which want self-describing enum values).
- Keep it a pure format decision on the seam — no domain enum changes.

## Session

- Created: 2026-07-20
