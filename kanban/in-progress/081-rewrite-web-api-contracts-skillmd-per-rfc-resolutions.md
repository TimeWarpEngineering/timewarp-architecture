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

- [ ] Resolve gate 1: canonical skill location / sync direction (timewarp-flow vs this repo).
- [ ] Get maintainer ruling on Decisions 3, 6, 8; record in the RFC vote table.
- [ ] Fix RFC §4 objective bugs (vote-independent):
      Tier-2 empty `Command` + `I*Details` won't compile; "MediatR" → TimeWarp.Mediator (incl. the
      *Detection* table at `SKILL.md:36`); folder-rule self-contradiction; case-sensitive
      `**/Features/**` discovery globs (TWA is kebab `features/`).
- [ ] Apply the 3–0 decisions: discover-first casing (kebab canonical, Pascal = 1-line mirror note);
      plural folders (delete the singular-folder narrative, `SKILL.md:46` + `examples.md:185-188`);
      Shouldly in TWA examples (FA = copic dialect footnote; note FA v8 commercial license);
      Create-Response mixed with GLM's axis (**"has invariants" → ctor+Guard**, not "trivial id-only").
- [ ] Fold in §3.5 findings: mock-pattern **detection** (contract-local `GetMockResponseFactory()`
      vs copic's SPA `*MockFactory` classes); `[IOpenDataQueryParametersMixin]`; the two
      **non-equivalent** auth forms + trigger (attribute ⇒ query-string synthesis, manual ⇒ POST/by-id);
      mock factory "required" → "when SPA mock mode needs the endpoint".
- [ ] Fold in §3.6 findings (tasks 078/080): binding is **validation-library-dependent**
      (Blazilla explicit-instance ✅ / Morris runtime-type ✗ / Blazored deprecated); nullability rule
      with GLM's forbidden-vs-discouraged split + `TWPA0002/0003` as the enforcement mechanism +
      severity note; analyzer-vs-generator **separate assemblies** rule.
- [ ] Re-anchor examples to TWA's real `admin/roles` (cite the `update-role.cs` route wart until 079
      fixes it) and cite `todo-items` as the anti-pattern.
- [ ] Align TWA docs (`documentation/developer/how-to-guides/web-api-contracts/`): source-generator
      layer, casing, and the 3 concrete doc bugs (broken `UpdateUser` brace, "mutability and
      nullability" copy-paste, `IReadonlyList<t>` typo).
- [ ] Run the skill's evals (`skills/web-api-contracts/evals/`) if runnable; update them to the new
      spec.

## Notes

- Supporting docs: `analysis/contract-conventions-rfc.md` (the spec-to-be, §3.6 = newest evidence),
  `analysis/composer-skill-analysis.md`, `analysis/glm52-review.md` (all three read-only inputs).
- Related: [[079-implement-server-side-createrole-endpoint--backend-validation-roles-contract]]
  (route wart), [[082-broaden-contract-nullability-analyzer-to-api-grpc-foundation-contracts]]
  (enforcement breadth), task 053-002 (rename — Decision 8 decides whether it gates anything).
- Remaining §7 cleanup slices (`sealed` shells, `init`→`set`, empty stubs, `BaseResponse`,
  `todo-items` disposition) are **not** this task — they get their own tasks after the skill is the
  settled spec, one decision per task.
