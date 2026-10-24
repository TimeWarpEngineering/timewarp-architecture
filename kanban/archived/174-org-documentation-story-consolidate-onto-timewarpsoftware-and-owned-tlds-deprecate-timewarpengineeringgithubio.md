# Org documentation story: consolidate onto timewarp.software and owned TLDs; deprecate timewarpengineering.github.io

## Description

Successor to task 125 (done — "Rethink documentation strategy: is the docfx
site worth publishing"), broadened to the ORG level with operator direction
from 2026-08-08:

- `timewarpengineering.github.io` (the org Pages repo, last pushed 2024-10) is
  **legacy for sure** — it drives https://timewarpengineering.github.io/index.html,
  a docfx-generated site. It will most likely be **deprecated when the new
  documentation story rolls out**; do NOT invest in its workflow (a 458-era
  workflow-consolidation task filed there was withdrawn for this reason).
- The org now has **https://timewarp.software/** live, and owns a number of
  timewarp TLDs that can be better utilized.
- Task 125's grounding still applies: docfx content is stale at the SOURCE
  (Tye/MediatR/AutoMapper references), agents consume skills + in-repo
  markdown, and a permanently-red deploy or years-stale public site is worse
  than no site.

## Checklist

- [ ] Decide the documentation story: what content is published (ADRs?
      how-tos? per-repo guides like nuru's releasing.md?), where it lives
      (timewarp.software? per-product TLDs?), and what generates it (docfx is
      presumed dead — pick lighter)
- [ ] Inventory the owned timewarp TLDs and map products/docs to them
- [ ] Deprecate `timewarpengineering.github.io`: archive the repo, unpublish
      or redirect the Pages site to the new home
- [ ] Scrub links to timewarpengineering.github.io from repos/templates
      (task 125 checklist carried forward)
- [ ] Fold per-repo documentation conventions into the org convention (458
      program, convention.md rule 10) once decided

## Notes

Created 2026-08-08 from the timewarp-nuru 458 rollout session, on operator
direction, when the org-wide workflow audit surfaced the forgotten Pages repo.
