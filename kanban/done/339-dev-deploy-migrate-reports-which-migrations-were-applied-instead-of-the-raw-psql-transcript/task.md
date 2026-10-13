# `dev deploy migrate` reports which migrations were applied, instead of the raw psql transcript

## Problem

Steve ran `dev deploy migrate --target kubernetes` on 2026-10-13 against the live kind deployment.
The output was psql's raw transcript: `NOTICE: relation "__EFMigrationsHistory" already exists`,
`CREATE TABLE`, then dozens of `DO` lines and a `START TRANSACTION` / `COMMIT` per migration,
ending with "applied web-migrations.sql with psql". Steve: "This output looks like missing stuff
either way, not very useful as is."

The published script is the **idempotent** EF script, so every migration's guarded block runs and
skips itself if its id is already in `__EFMigrationsHistory`. The transcript looks the same whether
10 migrations applied or none. It never says what changed or where the database ended up.

## Requirements (compose + kubernetes: the psql path in `services/deploy-operate.cs`)

Wrap the tool and report from documented state: EF's `__EFMigrationsHistory` table. Never parse
psql's `DO` lines or reconstruct EF internals (070-006 / 284 rule).

1. **Before:** read the applied ids with `psql -At -c 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId"'`,
   using the same exec path and credentials as the run. A missing table, on a fresh database,
   means none are applied.
2. **Run the script quietly:** `psql -q -v ON_ERROR_STOP=1`. Pass through errors and real
   WARNINGs. Suppress the expected `__EFMigrationsHistory already exists, skipping` NOTICE and the
   statement tags.
3. **After:** read the ids again and print a summary:
   - something applied: `Applied N migration(s):` with one id per line, then
     `Already applied: M · Database now at <latest id>`;
   - nothing applied: `Up to date: no pending migrations (M applied, latest <id>)`;
   - failure: the psql error, plus `Failed while applying migrations after <last applied id>`
     (from the after-read, if it succeeds), and a non-zero exit with psql's code.

   Keep the existing header (target, context, namespace, script path, the "Runs:" line) and the
   confirmation prompt.
4. **`--verbose`** keeps today's full transcript for debugging.
5. **ACA** (EF bundle path): the bundle already prints `Applying migration '…'`. Add the same final
   summary line, using the bundle's own output or a before/after history read through the same
   connection. Don't change the firewall-rule handling.

## Tests (`tests/tools/dev-cli-tests`)

- Summary formatting for applied / none / fresh database (no table) / failure.
- The diff of the before and after id lists.
- That the history query and `-q` are part of the built commands.
- No real cluster, container or database. Use the existing command-construction style with fake
  outputs.

## Docs

- `skills/tw-deploy` (+ references): example output in the migrate section, and `--verbose`.

## Checklist

- [x] Before/after `__EFMigrationsHistory` read via the same exec path; quiet psql run
- [x] Summary (applied / up to date / failure) + `--verbose`
- [x] ACA summary line
- [x] Tests + skill update
- [x] Gates: `dev build` 0/0, dev-cli-tests, `ganda repo audit`

## Notes

- Workers must not touch the maintainer's kind cluster or any database. The maintainer re-runs
  `dev deploy migrate --target kubernetes` after merge. It should print "Up to date" now that
  everything is applied.

## Results

- `tools/dev-cli/services/deploy-operate.cs`: `PsqlQuietCommand` (`-q`, `PGOPTIONS='-c client_min_messages=warning'`),
  `PsqlTranscriptCommand` (today's command, `--verbose`), `PsqlHistoryCommand`
  (`psql -At -v VERBOSITY=sqlstate -c 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId"'`).
  `BuildKubectlMigrateArguments` / `BuildComposeMigrateArguments` take the psql command, so the history read uses the
  run's exec path. `ParseMigrationHistory` maps SQLSTATE 42P01 (missing table) to "none applied" and other failures to
  unreadable; `NewlyAppliedMigrations` diffs; `BuildMigrationReport` prints applied / up to date / failure lines;
  `BuildBundleReport` does the ACA summary from the bundle's `Applying migration '…'` lines.
- `tools/dev-cli/endpoints/deploy-migrate-command.cs`: `--verbose` option; history read before and after the quiet
  run; report before the existing summary line. The header and the prompt are unchanged. ACA: the bundle step streams and
  captures (`RunAndCaptureAsync` instead of a TTY) to build the report. `--verbose` passes the bundle's `--verbose`.
  The firewall-rule handling is unchanged.
- Tests (`deploy-operate-tests.cs`): history parsing (ids / empty / 42P01 / unreadable), diff, reports for applied, up to
  date, fresh database and failure, the ACA bundle report, and the built quiet, transcript and history commands.
- Skill: `tw-deploy` SKILL.md (migrate bullet with example output, `--verbose`, the history query note), and both references.
- Gates: dev-cli-tests 218/0, `dev build` 0 warnings / 0 errors, the dev CLI compiles (`deploy migrate --help` lists
  `--verbose`), `ganda repo audit` passes, and `dev check-version` passes (2.0.0-beta.20 is ahead of nuget).
- Not run live. Workers must not touch the kind cluster or a database. Psql's `-q` / `client_min_messages` behavior
  against a real script is still unverified.

### Review

- Implementation review (`tw-implementation-review`): effort 2, roster general (review oracle, claude-opus-5-5), 2 rounds.
- Final counts: bug 0, suggestion 1 fixed, nit 0. Open 0, wontfix 0.
- M1 (suggestion, fixed): the ACA failure report now lists the migrations that committed before the failing one, then
  `Failed while applying <id>`. Tests were added. dev-cli-tests 218/0.
- Disposition: **clean**. Paths: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`.

### How to validate

Smoke:
```bash
dotnet run --project tests/tools/dev-cli-tests        # Migrate_Given_ report/history tests pass
dotnet run --file tools/dev-cli/dev.cs -- deploy migrate --help   # lists --verbose
# Maintainer, after merge, against the live kind deployment:
dev deploy migrate --target kubernetes --yes
dev deploy migrate --target kubernetes --yes --verbose
```

Expect:
- Tests: 0 failed.
- On the already-migrated kind deployment, the header and `Runs:` line, then no `DO`, `COMMIT` or NOTICE lines, then
  `Up to date: no pending migrations (N applied, latest <id>)` and the green summary line. Exit code 0.
- `--verbose`: today's full psql transcript, then the same `Up to date` report.

## Session

- Created: 2026-10-13 (cockpit, per Steve)
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-13T04:12:48Z
- Review oracle: claude (claude-opus-5-5), effort 2, disposition clean — 2026-10-13
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-13T04:15:22Z
