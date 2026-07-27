# File-placement decision guide: shared trees vs artifact folders, one table for agents

## Description

Maintainer directive (2026-07-27, folder review): "the key thing we have to make clear for
agents using the architecture's file structure is where to put files. What goes at top and is
cherry-picked into the artifact (csproj) vs what goes in the layer/artifact/deployable
folders."

The 126 program established the answer across many decisions; no single place states it as one
decision procedure. `tw-feature-placement` covers the grammar and (post platform-clusters) the
features-vs-platform-vs-host distinction, but an agent still has to synthesize the top-level
rule. Make the decision table explicit and put it where agents will hit it first.

**The rule to document (current truth, refine wording during authoring):**

| A new file is… | Home | Named | Namespace |
|---|---|---|---|
| Operation-specific product code (contract, handler, operation helper) | `web/features/<area>/<slice>/<use-case>/` | grammar `<name>[-<function>]-<layer>.cs` | `…Features.<Id>[.Layer]` (namespace declares slice membership) |
| Slice-shared product code (shared DTO/details, store, entity-type config) | slice root `web/features/…/<slice>/` | grammar | `…Features.<Id>[.Layer]` |
| Cross-slice platform concern with cohesion (a cluster: postgres, identity-host) | `web/platform/<cluster>/` | grammar | non-Features (platform, not slice) |
| Platform seam interface (contract between layers) | layer folder `abstractions/` (e.g. `web-application/abstractions/`) | conventional | non-Features |
| Host/deployable bootstrap (program.cs, appsettings, host defaults, environment checks, exemplar options) | the artifact folder (`web-server/` etc.) | conventional | non-Features |
| Artifact definition (csproj, global-usings; markers/IVT are generated) | the artifact folder | n/a | n/a |

Core principle to state plainly: **artifact folders contain the deployable and its definition —
nothing else.** All product and platform logic lives in the shared trees and is cherry-picked
into compilation units by the suffix globs. If a file is neither bootstrap nor artifact
definition and it's sitting in a layer folder, it's in the wrong place.

**Where it lands:**

- `skills/tw-feature-placement/SKILL.md` — the decision table as the skill's opening section
  (agents invoke this skill before creating files; the table is the first thing they should
  see, before the grammar detail). Public-skill style.
- AGENTS.md Layout section — one-paragraph compression of the same rule with a pointer to the
  skill (AGENTS.md is always in context; the full table lives in the skill).
- Check `tw-slice-isolation` and `tw-aggregate-pattern` skills for consistency — they each
  state fragments of placement; they should defer to tw-feature-placement's table rather than
  restate it.

## Checklist

- [ ] Author the decision table in `tw-feature-placement` (opening section, before grammar
      detail); verify every row against the actual tree (features/, platform/, abstractions/,
      web-server/) — no aspirational rows
- [ ] Compress into AGENTS.md Layout (short paragraph + skill pointer, minimal diff)
- [ ] Consistency pass over `tw-slice-isolation` / `tw-aggregate-pattern` placement fragments
      (defer, don't restate)
- [ ] Sanity-test the table against recent real placements: MockUserIds (slice root),
      chat-hub (slice root -server), postgres cluster (platform), cookie-browser-session-service
      (platform/identity-host), sample-options (host) — the table must reproduce each decision
- [ ] Docs only — no build impact expected; `dev build` as smoke, template-smoke NOT required
      unless skill files ship in template content (verify whether skills/ ships; if yes, run it)

## Notes

- Parent: 126. This is the capstone doc of the whole folder program: 126-001 (use-case
  folders), 126-002 (category-4 evacuation + namespace-declares-membership), 126-008 (platform
  clusters), 126-007 (generated artifact plumbing) each settled a row of the table; this task
  makes the synthesis explicit so agents don't re-derive it.
- Placement fragments already exist in: tw-feature-placement (grammar + features/platform/host
  table from 126-008), tw-slice-isolation (SliceRoot semantics), AGENTS.md Layout. The gap is
  the single decision-procedure entry point.

## Session

- Created: 2026-07-27 — filed from maintainer directive during folder review.
