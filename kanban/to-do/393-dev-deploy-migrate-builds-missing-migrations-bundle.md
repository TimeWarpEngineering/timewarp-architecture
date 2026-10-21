# One-command deploy: publish, deploy, migrate, smoke, open

## Description

A generated app should go from `dotnet new` to a live, migrated deployment with one command.
This is a template: the pit of success is the default path.

Today a first deploy is four verbs in a fixed order (`dev publish`, `dev deploy`,
`dev deploy migrate`, `dev open`). Skipping `dev publish` makes `dev deploy migrate` refuse
after a 4.5 minute deploy has already succeeded:

```
No published migrations for aca-neon: .../artifacts/aspire-output/aca-neon/efmigrations/web-migrations
does not exist. Publish them first (the verb never publishes for you), then re-run: dev publish aca-neon
```

Steve hit this on 2026-10-21 testing `aca-neon` (task 317).

Make `dev deploy --target <t>` the all-in-one path: publish (so the migrations bundle exists),
deploy, migrate (targets with a database), smoke the ingress, and print the URL. The separate
verbs stay for operators who want one step.

## Checklist

- [ ] `dev deploy --target <t>` runs publish, deploy, migrate, smoke, and prints the ingress URL, in that order, stopping at the first failure with the verb to re-run
- [ ] Migrate runs only for targets that have a database; a target without one skips it and says so
- [ ] Smoke is the documented anonymous checks (`/web-server/alive`, `/api/identity/entra/offered`), tolerating the Neon cold start; it never polls `/api/health` on a timer
- [ ] An opt-out per step for operators (e.g. `--no-migrate`), only if a real case needs it; prefer no flag
- [ ] `dev deploy migrate` alone still works; when the bundle is missing it publishes it instead of refusing
- [ ] Secrets never on argv and never printed (same rule as 317)
- [ ] `tw-deploy` skill (ships to generated apps): the first-deploy path for the user's AI is the single command, per target; the step verbs move to an operator section
- [ ] `skills/tw-deploy/references/azure-container-apps-neon.md` and the other target references updated to match
- [ ] dev-cli tests cover the step order and the stop-on-failure message
- [ ] Live proof on `aca-neon`: one command from empty resource group to 200 smoke, then `dev deprovision --target aca-neon`

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-21)

## Notes

- Origin: Steve 2026-10-21, "create a all in one endpoint, remember it is a template and the faster we can get someone going the better. The pit of success, and make sure we have a skill for the users AI".
- Code: `tools/dev-cli/endpoints/deploy-command.cs`, `tools/dev-cli/services/aspire-deploy.cs`, `aspire-deploy-preflight.cs`, `aspire-publish.cs`.
- Wrap tools, don't reconstruct internals: call `aspire publish` / `aspire deploy` and handle failure.
