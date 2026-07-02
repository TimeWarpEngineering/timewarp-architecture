# AGENTS.md

Guidance for all coding agents working in this repository (included by CLAUDE.md).

## Agent Context Regions — maintenance rule

Non-trivial source files carry embedded context regions (`#region Purpose`, `#region Design`,
optionally `#region Open Questions`). These are part of the code, not decoration:

- **When you edit a file that has regions, reconcile them with your change before finishing.**
  A Design region describing the old approach is a bug you just introduced.
- **When you create a non-trivial source file, add `Purpose` and `Design` regions** at the top,
  before the namespace.
- **When you read an unanswered question in `#region Open Questions` that you can answer,**
  answer it (or implement the answer and remove the pair).

Formats, lifecycle, and what counts as trivial: see the `agent-context-regions` skill.
