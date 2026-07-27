# Convert aggregate golden-pattern doc to tw-aggregate-pattern skill; self-directed foundation messages with in-package examples

## Description

From the 126 folder review (maintainer decisions, 2026-07-27). Two coupled fixes, one story:
the aggregate golden-pattern doc is agent instructions wearing an index filename, and foundation
runtime messages hardcode a template-relative path to it. Fix the genre and eliminate the
fragile pointer direction in one pass.

**Context:** `source/container-apps/web/web-domain/aggregates/overview.md` (37 lines) is the
golden aggregate pattern — typed id, `Entity<TId>` base, fail-closed `Create`, named mutations,
private nested `Invariants` (TWA0011/0012), save-time `DomainInvariantsGuard`. Post-126-002 its
folder holds no aggregates (they live in `features/*/…-domain.cs`), so `overview.md` indexes
nothing — and its content was never an overview; it is a prescriptive "do it this way" contract,
the same genre as `tw-feature-placement`/`tw-web-api-contracts`/`tw-slice-isolation`. Meanwhile
`missing-invariants-validator-exception.cs` bakes `web-domain/aggregates/overview.md` into TWO
runtime exception strings and `aggregate-db-context.cs:289` into one — published foundation
packages depending on one particular consumer's disk layout (maintainer: "referencing a file
location from foundation seems fragile").

**Part 1 — the skill:**

- Create `skills/tw-aggregate-pattern/SKILL.md` from the doc's content: the pattern bullets,
  the placement rule (an aggregate is `<name>-domain.cs` in its owning slice under the filename
  grammar; typed id file likewise), the enforcement map (TWA0011 nested Invariants required,
  TWA0012 must be private, save-time guard via `AggregateDbContext`), and the exemplar pointer
  (`source/container-apps/web/features/profile/profile-domain.cs` + `profile-id-domain.cs`).
  Frontmatter triggers: "add an aggregate", "IAggregateRoot", "aggregate root", "TWA0011",
  "TWA0012", "Invariants validator", "typed id".
- PUBLIC SKILL RULES: present tense only, no task numbers (strip "task 106"), no history
  narration.
- Delete `web-domain/aggregates/overview.md` and the now-empty `aggregates/` folder —
  `web-domain/` becomes csproj + `global-usings.cs` only (pure artifact shell).
- `documentation/developer/how-to-guides/HowToAddYourAggregate.md` stays as the human
  step-by-step walkthrough but defers to the skill as pattern SSOT — update its 3 references
  (lines ~10, 58, 224) accordingly; also update ADR-0009's pointer (line ~155).
- AGENTS.md: add the skill to whatever skill-index/table mentions the convention skills, if any.

**Part 2 — self-directed foundation messages (maintainer-decided layering):**

1. **The message IS the fix** (required layer, works offline in any consumer): e.g. "declare a
   `private sealed class Invariants : AbstractValidator<YourAggregate>` nested in the aggregate
   (rule TWA0011)". Rewrite both strings in
   `foundation-application/exceptions/missing-invariants-validator-exception.cs` and the one in
   `foundation-infrastructure/persistence/aggregate-db-context.cs` this way. NO file paths.
2. **The example ships in-package**: XML `<example>` blocks on `IAggregateRoot` and
   `Entity<TId>` (foundation-domain) carrying the minimal correct aggregate shape (private ctor
   + static Create + named mutation + private Invariants). IntelliSense becomes the example
   channel; it versions with the package and cannot dangle.
3. **Docs-site URL as trailing depth link only** (never load-bearing):
   `https://timewarpengineering.github.io/timewarp-architecture/` page for the aggregate
   pattern/how-to — verify the actual published URL before embedding; if no stable page exists
   yet, prefer omitting the URL over inventing one.

Template-relative paths must appear NOWHERE in package content when done.

## Checklist

- [ ] Author `skills/tw-aggregate-pattern/SKILL.md` (public style; content parity with the doc
      — nothing lost in conversion; exemplar + enforcement map + placement rule)
- [ ] Delete `web-domain/aggregates/` (doc + folder); confirm web-domain is csproj +
      global-usings only
- [ ] Rewrite the 3 foundation message strings per the three-layer rule (fix inline, no paths;
      optional verified docs-site URL)
- [ ] Add XML `<example>` docs on `IAggregateRoot` and `Entity<TId>`; reconcile their existing
      XML docs/Design regions
- [ ] Sweep referrers: HowToAddYourAggregate.md (×3), ADR-0009 (~line 155), any comment in
      `missing-invariants-validator-exception.cs` header, repo-wide grep for
      `aggregates/overview` — zero hits outside kanban history when done
- [ ] Verify TWA0004/regions on touched files; skills are excluded from that rule (confirm)
- [ ] Gates: `dev build` 0/0, `dev test` (foundation tests assert on exception messages? grep
      test tree for the old message fragments and update any assertions), `dev template-smoke`
      both matrices via current-code path
- [ ] Note in Results: message changes ship at the next foundation release (124 policy bundles
      packages + template; already-published beta.7 messages keep the old path — acceptable,
      beta channel)

## Notes

- Parent: 126. Origin: web-domain folder review conversation (2026-07-27). Maintainer
  decisions: (a) the doc is agent instructions → skill, not a relocated markdown; (b) foundation
  messages must be self-directed — inline fix first, in-package XML `<example>` as the example
  channel, URL only as non-load-bearing depth link; (c) do not preserve anything on
  "avoid churn" grounds — this repo is the clean greenfield exemplar, correctness outranks
  move-cost.
- Skill/doc split going forward: skill = agent contract + pattern SSOT;
  HowToAddYourAggregate.md = human walkthrough that defers to it.
- Related: 121 (IsConcurrencyToken enforcement follow-on) touches the same golden-path surface —
  do not absorb it here.

## Session

- Created: 2026-07-27 — filed from maintainer-approved bundle (skill conversion + message
  self-direction).
