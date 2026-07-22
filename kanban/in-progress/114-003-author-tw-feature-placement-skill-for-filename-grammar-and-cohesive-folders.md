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

- [ ] SKILL.md under skills/tw-feature-placement/ following existing repo-skill conventions
      (frontmatter, WHEN triggers, kebab-case)
- [ ] Cross-links: tw-slice-isolation and tw-web-api-contracts reference it where placement
      comes up; AGENTS.md points to the skill for the workflow
- [ ] Skills-are-public rule respected (no client names/history — see memory)
- [ ] Fold into 114 (skill is a 114 deliverable alongside the ADR)

## Notes

Sequencing (Steve, 2026-07-22): task 115 (template restore breakage — external consumers broken today) runs FIRST; then this skill + the 114 ADR close out 114.
