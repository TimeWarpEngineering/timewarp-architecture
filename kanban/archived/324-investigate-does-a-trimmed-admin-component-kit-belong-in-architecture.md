# Investigate: does a trimmed admin component kit belong in Architecture?

Investigation only — open question, handled one at a time. Steven is not convinced this belongs in Architecture. Not approved to build, not scheduled. Source: CrunchIt-to-Architecture review, Busara (Wisdom) 2026-10-12.

## Question

Should Architecture provide a small admin component kit limited to what Fluent UI v5 lacks: PageHeader, SectionHeader, KpiCard, EmptyState, FilterBar, GroupedTable?

## Busara's reasoning

REWORK. The original kit is cut down to only the pieces Fluent UI v5 lacks, and each one must be checked against Fluent first; anything Fluent already covers is dropped.

## Steven's skepticism

Steven is not convinced this belongs in Architecture. It came out of one solution (CrunchIt); being useful there does not make it a template item.

## What the investigation should answer

- Does it belong in Architecture: yes or no.
- Evidence: which existing or likely solutions would use it, what Architecture or Fluent UI v5 already provides, and the cost of carrying it in the template.
- If no: where it belongs instead (per-solution code, docs only, or nowhere).
- For each of the six components: does Fluent UI v5 already cover it?

## Notes

- Review notes: /workspace/ideas/2026-10-12-crunchit-to-architecture-review.md (v2 section), on the Grok Bot box.
- Investigation item, not a build task. Do not run `ganda task work` on it; take it up only when Steven picks it.

## Learnings

