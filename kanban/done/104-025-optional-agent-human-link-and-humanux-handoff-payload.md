# Optional agent-human link and humanUx handoff payload

## Parent

104

## Description

Optional link Agent ↔ Human and portable humanUx JSON for agent to present to its human. Not required for paid service (decision 3).

## Requirements

- Link/approve mechanism minimal
- humanUx schema documented in Design region / sample JSON

## Checklist

- [ ] Link model — lives on **205**, not this id
- [ ] Approve flow minimal — lives on **205**, not this id
- [ ] Sample humanUx payload — lives on **205**, not this id
- [x] Board close: superseded by **205**; `done` (not archived) so parent **104** can close

## Notes

Optional Wave 5. A2A-shaped handoff.

**2026-08-26:** Pulled off epic **104**. Work continues as independent to-do **205**
(progressive profile + agent-human handoff after more domain exists). Archived so
104 no longer owns this higher-level product.

### Depends on

104-004, 104-016

## Session

- Created: 2026-07-16
- 2026-08-26: pulled to **205**; archived so 104 no longer owns the product
- 2026-08-26: marked **done** (superseded stub) — parent-done treats archived as open (ganda 187)

## Results

Not implemented on this id. Agent–human link and humanUx handoff were pulled off epic **104** onto independent to-do **205**. This kitchen is a superseded stub: closed `done` so parent **104** can close (ganda parent-done: `Column != Done` includes archived). Product work stays on **205**. Do not revive this id.

### How to validate

**Smoke**

```bash
ganda kanban path 104-025
# Expect: …/kanban/done/104-025-optional-agent-human-link-and-humanux-handoff-payload.md

ganda kanban path 205
# Expect: …/kanban/to-do/205-progressive-profile-and-agent-human-handoff-after-more-domain-exists.md
```

**Expect**

- 104-025 is under `kanban/done/` (not archived, not in-progress).
- **205** remains to-do. Link/approve/humanUx are not on this id.
