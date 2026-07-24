# Rethink documentation strategy - is the docfx site worth publishing

## Description

Steve, 2026-07-24: "I don't even know if docfx site is worth publishing anymore... it's all
got to be dated by now anyway." Decide what the public documentation story should be before
spending any more effort on the publishing pipeline.

Current state (recorded while abandoning the deploy fix):

- The docfx deploy workflow (`timewarp-architecture-documentation.yml`) has failed on
  **every master push in visible history** — GitHub Pages was in legacy gh-pages-branch mode
  while the workflow uses the modern `actions/deploy-pages` flow ("Deployments are only
  allowed from gh-pages").
- During diagnosis the Pages **build_type was switched to `workflow`** (2026-07-24, via API).
  No workflow deploy has succeeded since, so
  https://timewarpengineering.github.io/timewarp-architecture/ still serves the **stale
  gh-pages branch content** (age unknown, predates recent architecture work by a long way).
- The docfx build itself reported `error_count: 10` in its last run — content/build health
  is also questionable.
- The workflow is still ENABLED, so every master push produces a red ✗ (a
  `gh workflow disable` was proposed but deliberately left to Steve).

## Decisions to make (with Steve)

1. Does a generated API/docfx site earn its keep at all, vs. the in-repo markdown
   (`documentation/` ADRs + how-tos) being the documentation? Agents consume skills and
   in-repo markdown; humans mostly arrive via the repo.
2. If some site should exist: keep docfx, or something lighter (plain markdown rendering,
   or fold into timewarp.software)? What content is actually worth publishing (ADRs?
   how-tos? not stale API dumps)?
3. Whatever the answer: either fix the pipeline end-to-end or delete the workflow +
   unpublish the stale site — a permanently-red deploy and a years-stale public site are
   both worse than no site.

## Checklist

- [ ] Interim: disable (or delete) the failing docs workflow to stop red noise on master
      (Steve's call — one `gh workflow disable timewarp-architecture-documentation.yml`)
- [ ] Decide 1–3 above with Steve
- [ ] Execute: fix-and-republish OR retire (remove workflow, unpublish/redirect Pages,
      update README links + AGENTS.md Documentation section)
- [ ] If retiring the site: scrub links to timewarpengineering.github.io/timewarp-architecture
      from the repo and template content

## Notes

Origin: "deploy failed" investigation 2026-07-24. The immediate 400 from Pages is FIXED
(build_type=workflow) — what remains is the strategic question, so no further pipeline work
until it's answered. AGENTS.md currently points to the stale site under "Documentation".
