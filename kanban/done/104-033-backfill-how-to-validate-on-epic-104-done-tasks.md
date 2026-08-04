# Backfill How to validate on epic 104 done tasks

## Parent

104

## Description

Add `### How to validate` to Wave 2–4 epic 104 done task Results so a human can re-prove each deliverable without reading the full diff.

## Checklist

- [x] Backfill 007–022, 030 Wave 2–4 overnight children
- [x] Task template documents How to validate stub

## Results

### Summary

Backfilled `### How to validate` on 17 done children (007–016, 017–022, 030). Updated `kanban/task-template.md`.

### How to validate

```bash
rg -l '### How to validate' kanban/done | rg '104-(007|008|009|010|011|012|013|014|015|016|017|018|019|020|021|022|030)'
# expect: 17 paths
```

## Session

- 2026-08-04 validation recipe process fix
