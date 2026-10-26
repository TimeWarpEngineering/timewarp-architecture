# Round 1 — general
**Date:** 2026-10-26
**Scope reviewed:** `task/419-update-aspire-to-1364` vs `master` (`c9f5011f9..396f8a2bb`). Product files: `Directory.Packages.props`, `skills/tw-deploy/references/azure-container-apps-neon.md`, `source/container-apps/aspire/projects/aspire-app-host/program.cs`.

## Summary

The change moves the template from Aspire 13.6.2 to 13.6.4 by setting the single `<AspireVersion>` property. The two preview packages move to `-preview.1.26523.11`. The aca-neon reference and two AppHost comments now cite 13.6.4. This is a low-risk patch bump. It matches the task and the implementer restatement. No issues were found.

## Verification (re-run by the reviewer, not taken from Results)

- nuget.org flat-container index lists `13.6.4-preview.1.26523.11` for both `Aspire.Hosting.Kubernetes` and `Aspire.Hosting.EntityFrameworkCore`. Both are also in the local NuGet cache.
- `rg '13\.6\.2'` outside `kanban/` matches only `tests/tools/dev-cli-tests/aspire-run-tests.cs`. The task says to keep those fixed inputs. `rg '26519'` outside `kanban/` matches nothing.
- `.github/workflows/workflow.yml` installs `Aspire.Cli` from the `<AspireVersion>` value read with `sed`, so there is no CI literal. There is no `dotnet-tools.json` Aspire pin.
- Tag `v13.6.4` in microsoft/aspire resolves to commit `c89352703f78ae46de90fbf751ed8b52b77b89bf`, the commit the reference now cites. The four cited source files have the same git blob SHAs at `af07e33` (13.6.2) and `c893527` (13.6.4), so the "unchanged publish behavior" claim holds at the source level.
- The local `aspire --version` is `13.6.4+c89352703f…`.
- `./bin/dev build`: 0 warnings, 0 errors.
- `./bin/dev check-version`: source `2.0.0-beta.27` is one prerelease increment ahead of `2.0.0-beta.26`. Not bumping `<Version>` is correct.
- The goldens tree is clean. `master` has gained only kanban files since the merge-base, so the branch will not conflict.

## Goal restatement check

The restatement matches the task's Description and Requirements, with no misread. The program.cs comment updates follow the restatement's rule to move comments that name the current pin. They are the only product edits beyond the requirements list.

## Issues

None.
