# Kebab-case rename cleanup for repo audit debt

## Description

Eliminate the pre-existing `kebab-path-names` audit debt so `ganda repo audit` (the blocking
tw-pr gate) passes on dev. With ganda ≥1.0.0-beta.22 (ganda task 189: tool-required-casing
exemptions landed, e.g. `appsettings.<Environment>.json`, MSBuild well-knowns), the check
reports **80 remaining paths** — the genuinely renameable debt. Full captured list:
`failure-list.txt` in this folder (re-run `ganda repo audit` for the live list).

Policy (Steve, 2026-07-29, memory + ganda 189): NO exemptions for conventional
SCREAMING/Pascal names — `LICENSE` → `license.md`, `Overview.md` → `overview.md`,
`Logo.png` → `logo.png` are all wanted. Exempt only what tooling genuinely requires — and if
such a case is found among the 80 (candidates below), it's a ganda 189 follow-up gap to
report, not a local workaround.

**Renames without reference updates are worse than the debt.** Every rename must carry its
references: markdown links, `.razor`/CSS/HTML image references, csproj/solution includes,
template.json sources, CI/workflow paths, Aspire resource references, code string literals.

## Requirements

1. **Rename all 80 paths to kebab-case** (files AND the flagged directories, e.g.
   `images/TheFreezeTeam/`, `images/TimeWarp/`), with `git mv` so history follows, updating
   all references in the same commit series. Batch commits logically (docs, images, kanban,
   templates assets, …) so review is tractable.
2. **Investigate before renaming (tool-required candidates among the 80):**
   - `source/container-apps/grpc/projects/grpc-server/Dockerfile` — `docker build` defaults
     to exactly `Dockerfile`; find every reference (Aspire `AddDockerfile`/`WithDockerfile`,
     CI, compose) — rename only if all call sites pass an explicit path; otherwise report as
     a ganda-189 exemption gap and leave in place (documented).
   - `timewarp-templates/testEnvironments.json` — VS well-known name; check whether it's
     used at all (may be deletable legacy — prefer delete over rename if dead).
   - `evals/contracts/fixtures/Web.Contracts.csproj` — fixture whose NAME may be load-bearing
     for the eval (assembly/project naming under test); verify eval scripts before renaming.
   - `timewarp-templates/NuGet.config` — SKIP: task 137 deletes it; coordinate, don't rename.
3. **Files with spaces** (`TIMEWARP ENTERPRISES.png/.svg`) and underscore names
   (`SOLID_*.png`, `*_512x512.png`, `.agent/workspace/*_*.md`) → kebab; update image
   references in web-spa (`.razor`, CSS, any manifest).
4. **Kanban slug fixes** (double-hyphen `--` names in done/archived/to-do) — plain `git mv`;
   also fix any inbound links from other kanban/docs files. Do NOT renumber tasks.
5. **Case-only renames** (`Overview.md` → `overview.md`): use two-step `git mv` if needed so
   the rename is recorded correctly for case-insensitive checkouts.
6. **LICENSE → license.md** (timewarp-templates): confirm the template pack references it
   (`timewarp-templates/*.csproj` PackageLicenseFile or similar) and update.
7. **Gates:** `ganda repo audit` **clean** (the point of the task); `dev build` 0/0;
   `dev template-smoke` green (template content paths change — logos/assets are pack
   assets); link integrity: grep for every old name post-rename → zero hits outside
   kanban history notes/this task's artifacts.

Out of scope: exempting anything for convention's sake; renaming files ganda now exempts;
task 137's NuGet.config deletion.

## Checklist

- [ ] Tool-required candidates investigated (Dockerfile, testEnvironments.json,
      Web.Contracts.csproj fixture); outcomes documented; any ganda-189 gaps reported
- [ ] All remaining paths renamed via git mv with references updated, batched commits
- [ ] Case-only renames verified two-step
- [ ] Old-name grep sweep: zero live references
- [ ] `ganda repo audit` CLEAN
- [ ] `dev build` 0/0; `dev template-smoke` green
- [ ] Kanban mutations committed

## Notes

- Origin: task 135 pre-PR gate → ganda task 189 (check hardening, shipped in
  1.0.0-beta.22) → this task eliminates the repo-side debt. Unblocks the 135 PR (and all
  future PRs) without gate waivers.
- Coordinate with task 137 (NuGet.config deletion) — whoever lands second rebases trivially.

## Session

- Created: c6f1a13b-487f-4085-bf61-ba4761e8579e (2026-07-30)
