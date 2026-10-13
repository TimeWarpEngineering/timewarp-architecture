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

- [ ] Before/after `__EFMigrationsHistory` read via the same exec path; quiet psql run
- [ ] Summary (applied / up to date / failure) + `--verbose`
- [ ] ACA summary line
- [ ] Tests + skill update
- [ ] Gates: `dev build` 0/0, dev-cli-tests, `ganda repo audit`

## Notes

- Workers must not touch the maintainer's kind cluster or any database. The maintainer re-runs
  `dev deploy migrate --target kubernetes` after merge. It should print "Up to date" now that
  everything is applied.

## Session

- Created: 2026-10-13 (cockpit, per Steve)
