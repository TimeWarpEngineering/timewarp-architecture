# Rewrite web-api-contracts SKILL.md per RFC resolutions

## Description

The deliverable the whole RFC exercise exists to produce: rewrite
`skills/web-api-contracts/SKILL.md` (+ `references/`) into the single corrected spec, per
[[contract-conventions-rfc]] (`skills/web-api-contracts/analysis/`). The RFC has 3 reviewer ballots
(Author/Opus, Composer/Grok, GLM 5.2) plus post-RFC empirical results (§3.6, tasks 078/080).

**All RFC ballots are now resolved (2026-07-02, recorded in the RFC vote table + decision
sections).** The rulings that shape this rewrite:

- **Decision 3 — RESOLVED**: dedicated `web-contracts-tests` project; GLM's trigger list
  (`required`/`init`/custom converters/non-default ctors, camelCase, `OneOf` envelopes) demoted to
  test-prioritization guidance. Basis: maintainer testimony — in copic the contracts were
  frontend-authored, the backend was another developer, integration tests came *after*; contract
  tests were the only seam check in the contract-first window the BFF workflow creates. The rewrite
  teaches this shape; creating TWA's actual `web-contracts-tests` project is a §7 cleanup slice.
- **Decision 6 — RESOLVED via Decision 8's sequencing**: document **both** auth forms + the trigger
  (attribute ⇒ query-string list queries; manual interface ⇒ POST/by-id), the mock-mode rationale,
  the "server never trusts client-sent `UserId`" rule, and copic's server-side derivation as the
  valid alternative — all under the **post-rename names**.
- **Decision 8 — RESOLVED: rename first; compatibility is a NON-factor.** Maintainer: "we want the
  best solution, we don't want tech debt because of 'compatibility' or previous bad decisions."

**Two gates before editing a line:**

1. **053-002 must be decided and implemented first** (should we rename, to what, and is there a
   consistent source-gen attribute convention). This task then writes against the final names, once.
2. **Skill sync path (RFC §6 caveat).** Task 053-002 refers to the skill as
   `timewarp-flow/master/skills/webapi-contracts` (no hyphen) while this repo has
   `skills/web-api-contracts` (hyphenated). Determine which is canonical and whether one syncs over
   the other (cf. how `ganda skills sync` clobbered the kanban-skill copy) — otherwise the corrected
   spec can be silently overwritten.

## Checklist

- [x] Resolve gate 1: 053-002 decided + implemented (commit `9fd133b1`) — skill written once
      against `[ApiRoute]`/`[AuthApiRequest]`/`[OpenDataQueryParameters]`/`[StateAccess]`.
- [x] Resolve gate 2: **this repo's `skills/web-api-contracts` is canonical** (maintainer,
      2026-07-02). Registered as a ganda skill source
      (`worktree://…/timewarp-architecture/master/skills/web-api-contracts`) and synced — tool
      copies (claude/grok/opencode) now flow FROM here; the rewrite distributes when dev merges to
      master. 053-002's `timewarp-flow/…/webapi-contracts` path was stale.
- [x] Maintainer rulings on Decisions 3, 6, 8 recorded in the RFC (all 8 ballots resolved).
- [x] RFC §4 objective bugs fixed: Tier-2 command declares interface props (+ found & fixed a 2nd
      compile bug — initializers on interface properties); MediatR → TimeWarp.Mediator incl.
      Detection table; folder rule; case-insensitive `[Ff]eatures` discovery globs.
- [x] 3–0 decisions applied: discover-first casing (kebab canonical + Pascal mirror one-liner);
      plural folders (singular narrative deleted); Shouldly (FA = copic dialect + v8 license note);
      Response discriminator = "has invariants" → ctor+Guard (`Guid.Empty` hole documented).
- [x] §3.5 folded: contract-attributes table incl. `[OpenDataQueryParameters]`; both auth forms +
      trigger + security rule + copic server-side alternative; mock-pattern detection; mock factory
      "required" → "when SPA mock mode needs the endpoint".
- [x] §3.6 folded: validation-library dependence (Blazilla ✅ / Morris ✗ / Blazored deprecated,
      RoleForm as living anchor); TWA0002/0003 forbidden-vs-discouraged split + `.editorconfig`
      downgrade; legacy attribute names documented for recognition only.
- [x] Examples re-anchored: TWA `admin/roles` as living anchor (update-role wart cited),
      `todo-items` as anti-pattern.
- [x] TWA docs aligned: `HowToWrite_BFF_API_Contracts.md` gained the "Route Attributes and Source
      Generation" section (+ kebab casing note, `[ApiRoute]`d samples, `SetValidator` composition,
      pointer to the skill); the 3 concrete doc bugs fixed (UpdateUser brace, nullability-doc
      copy-paste, `IReadonlyList<t>`); nullability doc gained a TWA0002/0003 enforcement section.
- [x] Evals reviewed: `evals/eval.yaml` is a routing smoke keyed on the skill name (unchanged) —
      still valid as-is.

## Results

- Skill rewrite: commit `42bb1175`. The skill is now the single corrected spec; the RFC remains
  as the decision record under `analysis/`.
- Follow-on created: [[083-create-web-contracts-tests-project-with-serialization-round-trips]]
  (Decision 3's build-out — the skill *teaches* the test project; 083 creates TWA's actual one).

## Notes

- Supporting docs: `analysis/contract-conventions-rfc.md` (the spec-to-be, §3.6 = newest evidence),
  `analysis/composer-skill-analysis.md`, `analysis/glm52-review.md` (all three read-only inputs).
- Related: [[079-implement-server-side-createrole-endpoint-backend-validation-roles-contract]]
  (route wart), [[082-broaden-contract-nullability-analyzer-to-api-grpc-foundation-contracts]]
  (enforcement breadth), task 053-002 (rename — Decision 8 decides whether it gates anything).
- Remaining §7 cleanup slices (`sealed` shells, `init`→`set`, empty stubs, `BaseResponse`,
  `todo-items` disposition) are **not** this task — they get their own tasks after the skill is the
  settled spec, one decision per task.
