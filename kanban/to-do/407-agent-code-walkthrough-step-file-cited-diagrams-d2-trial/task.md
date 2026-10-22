# Agent code walkthrough step: file-cited diagrams (D2 trial)

## Description

Add an agent walkthrough step that outputs a diagram of the code where every node cites a real file. A check fails the walkthrough if any cited path does not exist.

Owner: Architecture bot.

### Source

- Ryan Lanciaux post: https://x.com/ryanlanciaux/status/2113251665407029323
- Eval: https://github.com/TimeWarpEngineering/timewarp-intel/blob/master/evaluations/2026-10-22-lanciaux-diagram-walkthrough.md (PRs #49, #50)
- Busara verdicts: B (file-cited walkthrough step) GREEN; E (D2 trial on one real module) GREEN.

### Design

- Renderer-agnostic contract: input = nodes with file citations + edges; output = diagram.
- The path-exists check is independent of the renderer.
- D2 is the first renderer.
- TimeWarp.Flexbox (our C# Yoga rewrite) could be a later renderer. Parked until the trial log shows D2 failures.

### Not in scope

- reladraw (KILL: second diagram language, pre-1.0).
- Building a Flexbox diagram tool (parked; edge routing, SVG, graph mapping etc. is a large job).

### Future

- Once a second repo wants this step, extract it as a TimeWarp Dev Tools plugin.

## Checklist

- [ ] Define the contract (nodes with file citations, edges) and document it
- [ ] Implement the path-exists check; walkthrough fails on any missing cited path
- [ ] D2 renderer for the contract
- [ ] D2-only trial on one real module of this repo
- [ ] Log every picture D2 draws badly (only evidence to reopen Flexbox)
- [ ] Report trial results and the bad-render log

## Session

- Created: 67572 (2026-10-22)

## Notes

Steven decided the task lives in timewarp-architecture for now; may later become a TimeWarp Dev Tools "Plugin".
