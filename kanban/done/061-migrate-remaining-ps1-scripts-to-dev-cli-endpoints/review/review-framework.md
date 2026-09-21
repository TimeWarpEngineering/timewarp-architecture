# Review framework — task 061

**Date:** 2026-09-21
**Host task:** kanban/in-progress/061-migrate-remaining-ps1-scripts-to-dev-cli-endpoints/
**Diff scope:** branch `task/061-migrate-remaining-ps1-scripts-to-dev-cli-endpoints` vs `origin/master` (commits `9372fcb9`, `9b643cc5`)
**Plan / brief:** Replace leftover `.ps1` with Nuru `dev` endpoints or delete dead scripts. Port `add-migration.ps1` → `dev db add-migration` and `BuildAndInstallTemplate.ps1` → `dev template-install`; delete the rest of `scripts/` plus two template `.ps1` files.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok review 01a0c229-02a2-75d2-a91b-3a1c31d377f6 (2026-09-21)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Product files in scope

- `tools/dev-cli/services/db-ef.cs`
- `tools/dev-cli/endpoints/db-add-migration-command.cs`
- `tools/dev-cli/endpoints/db-group.cs`
- `tools/dev-cli/services/template-nupkg.cs`
- `tools/dev-cli/endpoints/template-install-command.cs`
- `tests/tools/dev-cli-tests/db-ef-tests.cs`
- `tests/tools/dev-cli-tests/template-nupkg-tests.cs`
- `tests/tools/dev-cli-tests/dev-cli-tests.csproj`
- `skills/tw-aggregate-pattern/SKILL.md`
- `.gitignore`
- deleted `scripts/**` and two template `.ps1` files

## Brief claims to re-verify

- Zero `.ps1` files remain in the worktree (except `.git`).
- `dev db add-migration` is design-time `dotnet ef` on kebab `source/container-apps/...` paths, not Aspire `web-migrations`.
- Migration name must be a C# identifier; `--dry-run` prints the exact invocation without running it.
- Apply remains `dev db update` against a running AppHost.
- `dev template-install` packs to `artifacts/template-install/` and installs the architecture template nupkg (not Analyzers/Generators/Attributes).
- Nupkg selection must not let `TimeWarp.Architecture.Analyzers.*.nupkg` win the glob.
- Endpoints use ITerminal + TimeWarp.Amuru (BannedSymbols: `System.Console`, `ProcessStartInfo`).
- Dead scripts were deleted with rationale; live AppHost verbs (`db update` / `drop` / `reset` / `status`) were not duplicated.
