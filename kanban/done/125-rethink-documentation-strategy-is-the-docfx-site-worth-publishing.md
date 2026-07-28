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

## Grounding (2026-07-24, after Steve reviewed the published site)

The staleness is in CURRENT source, not just the old published site — a fixed pipeline
would republish wrong content:

- `timewarp-templates/documentation/timewarp-architecture-template/Overview.md`: install
  instructions for **Project Tye** (dead upstream), links **MediatR** and **AutoMapper**
- `.../Features.md`: lists **Tye** as a feature
- `documentation/developer/conceptual/DirectoryStructure.md`: "the MediatR pipeline"
- Published site additionally references FluentAssertions (now repo-BANNED — commercial
  license) and Tailwind (dropped 059)

Checked clean: the template NUPKG does NOT ship the stale docfx tree, and its nuget.org
readme.md has zero dead-tech references — the package page is fine; only the Pages site and
the docfx source tree are rotten.

Interim action taken: the failing workflow file was RENAMED to
`.github/workflows/timewarp-architecture-documentation.yml.disabled` (Steve's call) so
master pushes stop going red; takes effect on master when the rename merges. Un-rename to
resurrect, pipeline itself now works (Pages build_type=workflow was fixed).

## Results

**Decision (Steve, 2026-07-28): Option C** — kill the publishing debris now; docs of record are
in-repo markdown (agents read skills/AGENTS.md/documentation/; template users receive the
documentation/ tree inside their generated app). Re-evaluate a public docs presence when the
repo gains an outward-facing audience (task 118's marketplace showcase is the trigger; GitHub
Pages vs timewarp.software decided then, with a real requirements owner).

**Evidence that settled it** (local run, 2026-07-28): docfx runs locally trivially
(`dotnet tool install -g docfx`; build = 15s, 0 errors — the historical error_count:10 did not
reproduce) but builds only **7 files**: the config was copy-paste archaeology from the
Blazor-State repo (metadata dest literally "Blazor-State/api", empty source list — API docs
never generated) and never pointed at the real documentation/ tree. The site could never have
been current; nobody noticed for years = no audience signal. The deploy workflow was already
disabled (.yml.disabled).

**Landed** (commit `9b8cdb27`, 492 deletions): docfx project deleted
(timewarp-templates/documentation/), disabled Pages workflow deleted, orphaned ADO pipeline yml
deleted; template nupkg `PackageProjectUrl` re-pointed from the stale site's Overview.html to
the GitHub repo (that URL ships on nuget.org); AGENTS.md + readme state the in-repo position
with the 118 re-evaluation hook. Zero `github.io/timewarp-architecture` references remain.
Diagnostic docfx tool uninstalled after use.

**Verification:** `dev build` 0/0; `dev template-smoke` both matrices SUCCEEDED (gates also
covered the concurrently-landed 129 stage-0 machinery present in the same tree).

**Residual (maintainer-side, one step):** GitHub Pages still serves the ancient gh-pages branch
content at the old URL until Pages is disabled in repo settings (or
`gh api -X DELETE repos/TimeWarpEngineering/timewarp-architecture/pages`). Repo-side, nothing
links to it anymore.

## Session

- Executed: 2026-07-28 — decision conversation + inline cleanup by orchestrator (Claude Fable);
  local docfx run as the deciding evidence.
