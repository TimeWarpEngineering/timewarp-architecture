# Spike Jaribu co-located integration tests

## Description

Prove that TimeWarp.Jaribu can serve as this repo's test framework for **co-located tests**
(test runfiles living inside `features/` slices next to the code they test) before committing
to the convention repo-wide. The direction is decided: Jaribu is the destination; Fixie remains
only in not-yet-migrated projects. This spike de-risks the two hardest claims on one or two
real files each — it does NOT migrate the existing test suite.

**Testing philosophy (constraint, not preference):** this repo prefers integration tests over
unit tests. "Don't mock your friends" — never mock code we control; mock only externalities we
don't control (third-party services, clocks, external APIs). A spike outcome that only proves
host-free unit-style tests is a FAILURE for this task: the integration-test case is the primary
case. Playwright e2e tests are a third tier that stays; the spike should place Jaribu relative
to them, not replace them.

Origin: brainstorm on co-locating tests next to source (2026-07-29 session). Key insight from
that discussion: Jaribu runfiles declare their own dependencies (`#:project`), making isolation
a per-file structural fact and collapsing the per-layer test-csproj routing question — the
axis-1 grammar would need only a flat `tests` suffix carved out of the membership guard.

## Requirements

1. **Contracts round-trip proof (easy case):** one Jaribu runfile co-located in a web slice
   (e.g. `features/counter/`), grammar-conformant name (`<name>-tests.cs` shape), referencing
   `web-contracts` via `#:project`, running standalone (`./file.cs`) — physically host-free.
2. **Integration-test proof (primary case):** one Jaribu runfile that spins a real server host
   and exercises an endpoint end-to-end through the mediator pipeline (happy path + validation
   rejection), honoring the fixed-port / serialized-execution constraint. Must evaluate whether
   the existing integration-test infrastructure's **flexible test DI container** abilities
   (service overrides for mocking externalities only) carry over to a runfile, and document
   what carries, what needs porting into a shared helper, and what Jaribu itself lacks.
3. **Aspire testing research:** survey current Aspire testing support (e.g.
   `Aspire.Hosting.Testing` / `DistributedApplicationTestingBuilder` and whatever is newest as
   of the spike) and assess whether it supersedes or complements the hand-rolled test-host
   approach for integration tests. We may be behind on what Aspire now offers — check latest.
4. **Multi-mode / IDE proof:** the same runfiles discovered via a `JARIBU_MULTI` project so
   `dotnet test` / IDE test explorers see them. If MTP discovery has rough edges, file the gap
   upstream in Jaribu (fix upstream, never work around — house rule).
5. **Membership-guard carve-out sketch:** identify (not necessarily implement) what the
   `feature-filename-grammar.json` registry and `feature-membership.targets` guard need so a
   `-tests.cs` file under `features/` is accepted but globbed by no layer project. Note
   interactions: TWA0004 `#region Purpose` after the shebang line, TWA0008/template
   conditionals now applying to test files, TWA0009 namespaces
   (`…Features.<Id>.<Sut>_.<Action>_Given_` nesting), TW0001 kebab naming vs Jaribu's
   `{sut}.{action}.cs` convention (grammar wins inside feature trees).
6. **Findings report:** write spike findings to this folder (`findings.md`) covering: what
   worked, upstream Jaribu gaps filed, Aspire recommendation, proposed test-tier map
   (co-located Jaribu integration/contract tests → `tests/` host-level tests → Playwright
   e2e), and the follow-up task list for actual adoption (grammar change, `dev test` glob
   update, migration order — integration tests under `tests/` migrate last or never).

Out of scope: migrating existing Fixie projects; changing `dev test`; landing the grammar
change. Spike artifacts may live on a branch that never merges — findings.md is the deliverable
of record.

## Checklist

- [ ] Contracts round-trip runfile co-located in a slice, runs standalone
- [ ] Integration-test runfile: real host, endpoint happy path + validation rejection
- [ ] Test DI container flexibility evaluated; carry/port/gap documented
- [ ] Aspire testing features surveyed (latest); recommendation written
- [ ] JARIBU_MULTI project: `dotnet test` / IDE discovery of the same files
- [ ] Upstream Jaribu gaps filed (if any)
- [ ] Membership-guard + analyzer interaction sketch written
- [ ] findings.md written with adoption follow-up task list
- [ ] Kanban mutations committed

## Notes

- "Don't mock your friends": mock ONLY externalities we don't control. Test through real
  layers we own. Unit-testing code we control behind mocks is considered wasted effort here.
- Existing integration tests (`tests/container-apps/**-integration-tests`) predate this and
  have battle-tested test-DI-container flexibility — treat them as the bar to clear, not
  legacy to discard.
- Sequencing rationale: framework decision precedes co-location convention (avoids building
  per-layer test-csproj routing that Jaribu obsoletes). Co-location grammar work is a
  follow-up task gated on this spike's findings.

## Session

- Created: c6f1a13b-487f-4085-bf61-ba4761e8579e (2026-07-29)
