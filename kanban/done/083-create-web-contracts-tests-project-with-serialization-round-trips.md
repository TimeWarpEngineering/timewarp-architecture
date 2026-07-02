# Create web-contracts-tests project with serialization round-trips

## Description

Build-out of **RFC Decision 3 (maintainer-resolved)**: a dedicated, host-free contracts test
project so serialization is checkable in the contract-first window (contracts are authored before
the server exists in the BFF workflow — maintainer testimony from copic: the contract tests were
the only seam check until backend integration tests arrived later).

Create `tests/container-apps/web/web-contracts-tests/` (Fixie + Shouldly + TimeWarp.Fixie —
mirror `timewarp-architecture-analyzers-tests` wiring) with `SerializeAndDeserialize` round-trips
using camelCase `JsonSerializerOptions`.

**Prioritize by GLM's trigger list** (per the resolved decision — this is what to test, not a gate):
contracts using `required`/`init` members, non-default constructors (all the ctor+`Guard`
Responses!), custom converters, `OneOf`/`SharedProblemDetails` envelopes, `ListResponse<T>`.
Plain auto-property POCOs are low-priority.

## Checklist

- [x] Project scaffold — host-free: references **web-contracts only** (no server, no host); added
      to `.slnx` `#if (web)` region. `dev test` discovery confirmed: it globs `tests/**/*.csproj`,
      so no wiring needed (and the glob survives template feature-flag exclusion).
- [x] Roles round-trips: `CreateRole.Command`/`Response`, `GetRole.Response`, `GetRoles.Response`
      (`ListResponse<RoleDto>` envelope with ctor+Guard DTOs).
- [x] Edge shapes: `SharedProblemDetails` losslessness (validation `errors` + `traceId` survive the
      Extensions catch-all in both directions); `GetRole.Query` round-trips its **source-generated**
      `RoleId` route property. **Bonus test:** Guard invariants run *during deserialization* — a
      `Guid.Empty` `roleId` payload throws rather than materializing an invalid `Response`.
- [x] Trivial-POCO skip documented in `contract-serialization.cs` Design region (single authority
      for options + RoundTrip helper).
- [x] No validator tests (skill rule).

## Results

**7 tests, all passing in ~0.07s** — genuinely host-free; runs in the contract-first window.

**Inference-removal candidate surfaced (per standing directive):** the canonical
`JsonSerializerOptions` (CamelCase) now exists in **three places by convention** — web-spa
`program.cs` DI config, the old web-spa-integration-tests serialization test, and
`contract-serialization.cs` here. Candidate: hoist one canonical options declaration into
foundation-contracts and reference it everywhere. Recorded in the test file's Design region.

## Notes

- Skill spec: `skills/web-api-contracts/SKILL.md` §"Contract serialization tests" + Tier 4 in
  `references/examples.md` (Shouldly assertions).
- Rationale + testimony recorded in [[contract-conventions-rfc]] Decision 3.
- Copic's `Web.Contracts.Tests` (23 test files, FluentAssertions) is the read-only reference shape.
- Consider whether the existing round-trips in `web-spa-integration-tests/Serialization/` move
  here or stay (they run under the Docker-dependent Aspire host; moving them makes them host-free).
