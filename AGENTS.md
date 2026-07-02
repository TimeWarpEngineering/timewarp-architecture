# AGENTS.md

Guidance for all coding agents working in this repository (included by CLAUDE.md).

## Agent Context Regions — maintenance rule

Every source file carries a `#region Purpose` block (enforced at build time by **TWPA0004**);
files with design decisions also carry `#region Design`, and optionally `#region Open Questions`.
These are part of the code, not decoration:

- **When you edit a file that has regions, reconcile them with your change before finishing.**
  A Design region describing the old approach is a bug you just introduced.
- **When you create a source file, add `#region Purpose`** (one honest line minimum) at the top,
  before the namespace — plus `Design` where there are genuine decisions to record.
- **When you read an unanswered question in `#region Open Questions` that you can answer,**
  answer it (or implement the answer and remove the pair).

Formats, lifecycle, and what counts as trivial: see the `agent-context-regions` skill.
