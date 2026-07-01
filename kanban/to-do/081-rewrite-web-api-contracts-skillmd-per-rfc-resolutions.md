# Rewrite web-api-contracts SKILL.md per RFC resolutions

## Description

The deliverable the whole RFC exercise exists to produce: rewrite
`skills/web-api-contracts/SKILL.md` (+ `references/`) into the single corrected spec, per
[[contract-conventions-rfc]] (`skills/web-api-contracts/analysis/`). The RFC has 3 reviewer ballots
(Author/Opus, Composer/Grok, GLM 5.2) plus post-RFC empirical results (§3.6, tasks 078/080).

**Two gates before editing a line:**

1. **Skill sync path (RFC §6 caveat).** Task 053-002 refers to the skill as
   `timewarp-flow/master/skills/webapi-contracts` (no hyphen) while this repo has
   `skills/web-api-contracts` (hyphenated). Determine which is canonical and whether one syncs over
   the other (cf. how `ganda skills sync` clobbered the kanban-skill copy) — otherwise the corrected
   spec can be silently overwritten.
2. **Maintainer ruling on the 3 contested ballots (2–1):**
   - **Decision 3** (contract serialization tests): dedicated `web-contracts-tests` project
     (Author+Composer) vs GLM's "only when the contract uses `required`/`init`/custom
     converters/non-default ctors — round-tripping auto-property POCOs is tautological."
   - **Decision 6** (`IAuthApiRequest`): promote to canonical (Author+Composer) vs GLM's "document
     both forms + trigger, hold 'canonical' until post-rename" (copic's server-side derivation is a
     valid competing design; TWA itself is split attribute-vs-manual).
   - **Decision 8** (053-002 rename sequencing): rename-first (Author+Composer) vs GLM's third
     option — **rewrite the skill against the target name `[Route]` with a migration note, clean
     contracts against current `[RouteMixin]`, don't gate anything on the rename.** Note: session
     evidence (078/080 landed cleanly against `[RouteMixin]`) has strengthened GLM's position, and
     Decisions 6+8 are coupled (the auth attribute is renamed by 053-002).

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
