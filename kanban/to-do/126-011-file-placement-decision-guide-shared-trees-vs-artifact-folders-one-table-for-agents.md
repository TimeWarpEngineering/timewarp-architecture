# File-placement decision guide: shared trees vs artifact folders, one table for agents

## Description

Maintainer directive (2026-07-27, folder review): "the key thing we have to make clear for
agents using the architecture's file structure is where to put files." Revised 2026-07-27 after
a first-principles challenge from the maintainer ("don't be biased by what is — think what
should be"): the `web-application/abstractions/` folder was the last unprincipled placement —
an artifact of conflating *layer* with *folder*, which the filename grammar dissolved. This
task both completes the structure and documents the now-clean rule.

**Part 1 — move the four seam interfaces into their platform clusters (kill `abstractions/`).**

The compilation-unit split (interface → web-application assembly, impl → web-server assembly)
is carried by filename suffixes, not folders — so seam and implementation belong side by side
in their concern's cluster, exactly as contract sits beside handler in a use-case folder:

| Current (`web-application/abstractions/`) | New |
|---|---|
| `i-current-principal-accessor.cs` | `platform/identity-host/i-current-principal-accessor-application.cs` |
| `i-browser-session-service.cs` | `platform/identity-host/i-browser-session-service-application.cs` |
| `i-agent-caller-context.cs` | `platform/identity-host/i-agent-caller-context-application.cs` |
| `i-request-host-accessor.cs` | classify during execution: `platform/identity-host/` if its consumers are identity/session-flavored (check real usage — WebAuthn RP selection etc.), else its own or another cluster |

Namespaces unchanged (platform files, non-`Features.*` — same rule as 126-008). Verify each
file stays in web-application's compilation unit post-move (`-application` suffix glob;
`-getItem:Compile` spot check). Delete the emptied `abstractions/` folder. Purpose/Design
regions: reconcile any that narrate the old home; the impls' regions saying "seam/impl split"
stay true — only folder narration changes.

**Part 2 — document the rule the tree now actually obeys.**

The one-sentence rule:

> All logic lives in a concern folder under a shared tree — `features/` for product concerns,
> `platform/` for platform concerns — named by the filename grammar; artifact folders hold only
> the artifact definition (csproj, global-usings) and its entry-point bootstrap (program.cs,
> appsettings, launchSettings, host-config exemplars).

The litmus test for the fuzzy middle:

> **If this deployable were deleted, would the file still mean something?** Yes → shared tree
> (which concern folder?). No → it is bootstrap; it stays with the artifact.

(Validates every placement made in 126-001/002/008: seam interfaces pass → platform;
sample-options/sample-environment-check fail → host; postgres-db-environment-check passes →
platform/postgres.)

Where it lands:

- `skills/tw-feature-placement/SKILL.md` — rule + litmus test + a compact decision table as the
  skill's OPENING section (before grammar detail). Public-skill style. Fewer rows, more
  principle: concern trees (features/platform, with the operation/slice-shared/cluster
  sub-rules), artifact folders (definition + bootstrap only), and the litmus test.
- AGENTS.md Layout — one-paragraph compression + skill pointer (minimal diff).
- Consistency pass over `tw-slice-isolation` / `tw-aggregate-pattern` placement fragments
  (defer to tw-feature-placement, don't restate).

**Known judgment call to surface, not silently decide:** `web-infrastructure-module.cs` (thin
DI manifest for the assembly) — arguably artifact-definition material, arguably platform. Ask
the maintainer with a one-line recommendation during execution; do not guess.

## Checklist

- [ ] Classify `i-request-host-accessor` by real usage; record the rationale
- [ ] `git mv` + grammar-rename the four interfaces into their clusters; delete `abstractions/`
- [ ] Verify compilation units unchanged (`-getItem:Compile` on web-application); namespaces
      untouched; Purpose/Design regions reconciled
- [ ] Surface the `web-infrastructure-module.cs` judgment call to the maintainer
- [ ] Author the opening section of `tw-feature-placement` (rule + litmus + table); verify
      every claim against the post-move tree — no aspirational rows
- [ ] Compress into AGENTS.md Layout; consistency pass over the two sibling skills
- [ ] Sanity-test the rule against real placements: MockUserIds (slice root), chat-hub (slice
      root `-server`), postgres cluster, identity-host cluster incl. the newly moved seams,
      sample-options (host) — the rule must reproduce each decision
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke` both matrices (file moves are
      template content)

## Notes

- Parent: 126. Capstone of the folder program: 126-001 (use-case folders), 126-002 (category-4
  evacuation + namespace-declares-membership), 126-007 (generated artifact plumbing), 126-008
  (platform clusters), and this task (seams into clusters) each settled part of the rule; this
  task makes the synthesis explicit AND finishes the tree so the doc describes reality.
- Maintainer reasoning trail (2026-07-27): "why should abstractions go in a layer folder?" —
  no principled reason survived; the earlier "seam/impl split" defense conflated layer with
  folder. Two kinds of places only; the litmus test resolves edge cases.
- Prior spec draft had a six-row table and kept `abstractions/` as a home — superseded by this
  revision (structure first, then fewer rows, more principle).

## Session

- Created: 2026-07-27 — filed from maintainer directive during folder review.
- Revised: 2026-07-27 — folded in the seam-interface moves after the maintainer's
  first-principles challenge; rule collapsed to one sentence + litmus test.
