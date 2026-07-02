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

- [ ] Project scaffold (`web-contracts-tests.csproj`, `testing-convention.cs`, `global-usings.cs`);
      add to `timewarp-architecture.slnx` (inside the `#if (web)` region) and verify `dev test`
      picks it up.
- [ ] Round-trips for the roles feature (the clean anchor): `CreateRole.Command`/`Response`
      (ctor+Guard), `GetRole.Response` (`IRoleDetails` + ctor), `GetRoles.Response`
      (`ListResponse<RoleDto>` with ctor DTO — the highest-value target).
- [ ] Round-trips for envelope/edge shapes: a `SharedProblemDetails` payload, a query with
      generated route properties (`[ApiRoute]` params deserialization).
- [ ] Deliberately **skip** trivial POCO round-trips (document the skip in the test project README
      or convention comment) — Decision 3's prioritization, not laziness.
- [ ] Do NOT test validators in isolation here (skill rule; FluentValidation is integration-tested).

## Notes

- Skill spec: `skills/web-api-contracts/SKILL.md` §"Contract serialization tests" + Tier 4 in
  `references/examples.md` (Shouldly assertions).
- Rationale + testimony recorded in [[contract-conventions-rfc]] Decision 3.
- Copic's `Web.Contracts.Tests` (23 test files, FluentAssertions) is the read-only reference shape.
- Consider whether the existing round-trips in `web-spa-integration-tests/Serialization/` move
  here or stay (they run under the Docker-dependent Aspire host; moving them makes them host-free).
