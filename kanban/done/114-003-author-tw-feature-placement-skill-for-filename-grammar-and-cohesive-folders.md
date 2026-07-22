# Author tw-feature-placement skill for filename grammar and cohesive folders

## Description

Agents' primary convention source is skills/ (Steve, 2026-07-22): the axis-1 filename grammar
and feature-cohesive folder layout (decided in 114, shipped in 114-002) need a dedicated skill
alongside tw-slice-isolation (TWA0009) and tw-web-api-contracts — "where does this file live,
what is it named, what happens when I get it wrong" is the most common agent decision in this
repo. AGENTS.md carries the summary; the skill carries the workflow.

Content (derive from 114 axis-decisions.md, 114-001 findings, 114-002 implementation):
grammar `<name>[-<function>]-<layer>.cs` with worked examples per archetype; contracts drop the
function segment; escape hatch; the registry SSOT (feature-filename-grammar.json) and how to
extend it (registry edit ⇒ full rebuild caveat); the membership guard + TWA0015/0016 errors and
what each means when hit; spa exception (stays conventional); slice-promotion note (per-module
assembly split is a glob operation, axis 2); WHEN-triggers for file creation/moving/naming.

## Checklist

- [x] SKILL.md under skills/tw-feature-placement/ following existing repo-skill conventions
      (frontmatter, WHEN triggers, kebab-case)
- [x] Cross-links: tw-slice-isolation and tw-web-api-contracts reference it where placement
      comes up; AGENTS.md points to the skill for the workflow
- [x] Skills-are-public rule respected (no client names/history — see memory)
- [x] Fold into 114 (skill is a 114 deliverable alongside the ADR)

## Notes

Sequencing (Steve, 2026-07-22): task 115 (template restore breakage — external consumers broken today) runs FIRST; then this skill + the 114 ADR close out 114.

## Session

- Implementer: authored `skills/tw-feature-placement/SKILL.md` by reading 114's
  `axis-decisions.md` (Axis 1 + Axis 1 addendum, Axis 2), 114-001's spike Results (findings 1-6),
  and 114-002's Requirements/Results, plus the shipped registry (`feature-filename-grammar.json`),
  analyzer source (`feature-filename-grammar-analyzer.cs`), membership guard
  (`feature-membership.targets`), and analyzer tests — to ground every worked example and
  diagnostic message in the actual shipped implementation rather than the planning docs.

## Results

### What was implemented

`skills/tw-feature-placement/SKILL.md` — a dedicated skill covering:

- The grammar `<name>[-<function>]-<layer>.cs` with a segment table and worked examples for
  each registered archetype (`handler`→application via `create-role-handler-application.cs`,
  `feature-annotations`→server via `hello-feature-annotations-server.cs`; `endpoint`→server
  noted as registered-but-not-yet-hand-authored since the template generates FastEndpoints
  rather than hand-writing them).
- Contracts dropping the function segment (`<name>-contracts.cs`) and the escape hatch
  (`<name>-<layer>.cs`, e.g. `role-store-application.cs`).
- The registry SSOT (`feature-filename-grammar.json`), what it generates
  (`feature-filename-grammar.g.cs` + `feature-filename-grammar.g.props`), the extension
  workflow (edit JSON → build → full rebuild), and the registry-edit-requires-full-rebuild
  caveat (analyzer DLL incremental staleness).
- The membership guard's zero-match build error (exact message) and why dual-match can't occur
  structurally.
- TWA0015 (registered function paired with wrong layer) and TWA0016 (unregistered/misspelled/
  incomplete function segment) — trigger and fix for each, verified against the analyzer's
  actual diagnostic messages and test cases (including the `feature-only-annotations-server.cs`
  incomplete-multi-segment case).
- The SPA exception (`web-spa/features/` stays conventional, no glob/grammar).
- The axis-2 per-module-assembly-split note (a csproj/glob change, files never move).
- An agent workflow checklist for create/move/build-error/registry-extension cases.

### Cross-links

- `skills/tw-slice-isolation/SKILL.md`: added a Related-skills row pointing to
  `tw-feature-placement`; replaced its inline grammar description in the greenfield scaffold
  workflow (step 2) with a pointer to the new skill.
- `skills/tw-web-api-contracts/SKILL.md`: added a Related-skills row pointing to
  `tw-feature-placement`.
- `AGENTS.md`: the axis-1 filename-grammar paragraph now points to
  `skills/tw-feature-placement/SKILL.md` for the full workflow.

### Skills-are-public compliance

Grepped the new SKILL.md for dates, "spike"/"migration"/history phrasing, and names — zero
hits. Content is phrased as present-tense fact/workflow, matching `tw-slice-isolation`'s style.

### Verification

Read-through only (documentation task, no build/test surface); content checked line-by-line
against the shipped registry JSON, analyzer source, membership-guard `.targets` file, and
analyzer test file rather than trusting the planning-doc prose, since 114-002's Results noted
the shipped implementation deviated in details (e.g. exact diagnostic wording, path-normalization
scoping) from the spike.
