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

Steve hit this on 2026-10-21 testing `aca-neon` (task 317). On the same run, passkey sign-in failed with
"Host not allowed" until the ingress host was added by hand:
`az containerapp update -n web-server --set-env-vars WebAuthnOptions__AllowedRpIds__0=<ingress fqdn>`
(config appends to the `localhost` default; a redeploy drops the setting). Requests were also slow:
the app was in Azure `eastus` while the Neon project was in AWS `ap-southeast-1`, so every query
crossed the Pacific (~0.8 s for a no-op `/web-server/alive` from UTC+7). Steve redeploys to
`southeastasia`.

Make `dev deploy --target <t>` the all-in-one path: publish (so the migrations bundle exists),
deploy, migrate (targets with a database), smoke the ingress, and print the URL. The separate
verbs stay for operators who want one step.

## Goal (implementer restatement)

`dev deploy --target <t>` is the path from a generated app to a live site. In order it publishes (so the migration files exist), deploys, migrates when that app has a database, smokes the ingress, and prints the URL. It stops at the first failure and names the verb that retries that step. A target with no database skips migrate and says so. Smoke requests only `/web-server/alive` and `/api/identity/entra/offered`, waits through a Neon or scale-to-zero cold start, and never calls `/api/health`. There is no per-step opt-out flag; the existing step verbs are how an operator runs one step. `dev deploy migrate` still works alone, and a missing bundle is published instead of refused. Secrets stay off the command line and out of the output. Passkeys work on the first deploy: Azure Container Apps (aca and aca-neon) put the public ingress host on `WebAuthnOptions:AllowedRpIds` from the environment's public default-domain output, and any other public host is the `passkey-rp-host` parameter that deploy copies in. Compose and Kubernetes already allow `localhost`, which is the public host of those default paths. aca-neon preflight warns, and does not refuse or print the connection string, when the pooled host's AWS region is far from `Azure:Location`, and it names the preferred nearby Azure region. A location in that AWS region's nearby set stays quiet. The tw-deploy skill and the target references lead with the one command, move the step verbs to an operator section, document the passkey host (a custom domain is its own entry), and say to put the app and the Neon project in the same region, near the users. The handler runs that order through one step runner. dev-cli tests drive the runner with fake delegates, so a skipped or reordered step fails, and they lock the stop message. A live aca-neon run goes from an empty resource group to HTTP 200 on those two paths, then `dev deprovision --target aca-neon`.

## Checklist

- [x] `dev deploy --target <t>` runs publish, deploy, migrate, smoke, and prints the ingress URL, in that order, stopping at the first failure with the verb to re-run
- [x] Migrate runs only for targets that have a database; a target without one skips it and says so
- [x] Smoke is the documented anonymous checks (`/web-server/alive`, `/api/identity/entra/offered`), tolerating the Neon cold start; it never polls `/api/health` on a timer
- [x] An opt-out per step for operators (e.g. `--no-migrate`), only if a real case needs it; prefer no flag
- [x] `dev deploy migrate` alone still works; when the bundle is missing it publishes it instead of refusing
- [x] Secrets never on argv and never printed (same rule as 317)
- [x] Passkeys work on the first deploy: web-server's `WebAuthnOptions:AllowedRpIds` includes the public ingress host on every deployed target (aca, aca-neon, compose, kubernetes). Prefer the AppHost wiring the ingress FQDN to web-server; fall back to an AppHost parameter `dev deploy` fills. Today the deployed list is only `localhost`, so sign-in fails with "Host not allowed: Passkeys are not enabled for this host"
- [x] `tw-deploy` skill documents the passkey host (and a custom domain adds its own entry)
- [x] aca-neon preflight warns when the Neon host's region (from the pooled string's host, e.g. `…aws.neon.tech` region segment; never print the string) is far from `Azure:Location`, naming the nearby Azure region to use. Warn, not refuse
- [x] `tw-deploy` guide: put the app and the Neon project in the same region, near your users
- [x] `tw-deploy` skill (ships to generated apps): the first-deploy path for the user's AI is the single command, per target; the step verbs move to an operator section
- [x] `skills/tw-deploy/references/azure-container-apps-neon.md` and the other target references updated to match
- [x] dev-cli tests cover the step order and the stop-on-failure message
- [x] Live proof on `aca-neon`: one command from empty resource group to 200 smoke, then `dev deprovision --target aca-neon`

## Results

`dev deploy --target <t>` is the first-deploy command. It publishes, deploys, migrates when the app has a database, smokes the two anonymous paths, and prints the URL. The handler runs that order through `DeployPipeline.RunAsync`, which stops at the first non-zero exit. A missing migrations bundle is published by `dev deploy migrate` instead of refused. Azure Container Apps puts `ingress.<environment default domain>` on `WebAuthnOptions:AllowedRpIds` through the public `GetOutput` of `AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN`. A custom domain is the `passkey-rp-host` parameter. aca-neon warns, and does not refuse, when the Neon AWS region is outside the nearby Azure set, and it names the preferred region. A location in that set stays quiet.

### How to validate

Smoke: from a checkout with `az login`, AppHost user secrets for the two Neon strings, `Azure:Location`, and `Azure:ResourceGroup`, create that resource group empty and run `dev deploy --target aca-neon --yes`.

Expect: exit 0, the step line `publish → deploy → migrate → smoke`, `200 /web-server/alive`, `200 /api/identity/entra/offered`, and a printed `https://` URL whose host equals web-server's `WebAuthnOptions__AllowedRpIds__0`. The migrate plan shows `<direct Neon connection string>`, not the secret. Then `dev deprovision --target aca-neon --yes` deletes the resource group and leaves the Neon project.

Live proof on 2026-10-21. Empty resource group `rg-tw-architecture-aca-neon-sea` in `southeastasia` (0 resources). Neon pooled host region `ap-southeast-1`, so the location already matched and preflight stayed quiet. `dev deploy --target aca-neon --yes` exited 0. Smoke printed `200 /web-server/alive` and `200 /api/identity/entra/offered`. The URL was `https://ingress.orangetree-59f940d0.southeastasia.azurecontainerapps.io`. web-server's `WebAuthnOptions__AllowedRpIds__0` was that same host. Migrate ran the bundle and reported the database already up to date. `dev deprovision --target aca-neon --yes` exited 0. The group was `Deleting`, with only the Container Apps environment left, when this session stopped watching. The Neon project was not deleted.

Tests, Release, from each project directory, after the review fix pass: `dev-cli-tests` 293 passed; `AcaNeonPublish_Given_` 7 passed; `AcaPublish_Given_` 13 passed. `dotnet build` of `tools/dev-cli/dev.cs` and the AppHost succeeded. `ganda repo audit` passed (32 checks, 0 failed, 1 skipped). This pass did not repeat the live Azure deploy.

### Review

Round 1 ran on 2026-10-21 at effort 3 (by-diff budget) with one general reviewer. The reviewer re-ran `dev-cli-tests` in Release and got 289 passed. Final counts so far: 1 bug, 1 suggestion, and 1 nit, all open. The bug is M1: the step-order and stop tests lock `DeployPipeline.Steps` and the test-only `StopMessage`, not the order the handler actually runs. The suggestion is M2: the AppHost reflects into the internal `ContainerAppDomain` getter when the public `GetOutput` exists. The nit is M3: the Neon region warning calls a nearby pair "far". A fix pass was needed before round 2. Paths: `review/review-framework.md`, `review/round-1/general.md`, and `review/round-1/merged.md`.

Round 2 ran on 2026-10-21 at effort 2 (by-diff budget, 386 lines) with one general reviewer. It covered the fix delta `80af5fa25..bf734a6a1`. M1, M2, and M3 are all fixed, and the round raised no new findings. The reviewer re-ran `dev-cli-tests` in Release (293 passed) and built the AppHost in Release (0 warnings, 0 errors). The reviewer also confirmed that `AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN` is in the pinned Aspire.Hosting.Azure.AppContainers 13.6.2 assembly. Final counts: bug 0 open / 1 fixed; suggestion 0 open / 1 fixed; nit 0 open / 1 fixed; 0 wontfix. **Disposition: clean.** Paths: `review/round-2/general.md`, `review/round-2/merged.md`, and `review/disposition.md`.

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-21)
- Implement: one-command deploy, passkey ingress host, Neon region warning, and the aca-neon live proof (2026-10-21)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-21T14:33:23Z
- Review oracle round 1: general reviewer (claude, model claude-opus-5-5), effort 3, session not reported. 3 findings open (M1 bug, M2 suggestion, M3 nit); a fix pass is requested (2026-10-21)
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-21T14:36:43Z
- Fix pass (review round 1): the handler runs `DeployPipeline.RunAsync` over `Steps`, the AppHost reads the domain with public `GetOutput`, and a nearby Azure region stays quiet (2026-10-21)
- Review oracle round 2: general reviewer (claude, model claude-opus-5-5), effort 2, max-turns 120, session not reported. M1–M3 fixed, no new findings, disposition clean (2026-10-21)

## Learnings

- Restatement vs task (narrowing, not a misread of intent): the task says passkeys work on "every deployed target (aca, aca-neon, compose, kubernetes)". The restatement says compose and Kubernetes are covered by the `localhost` default. That holds for compose and for a kind port-forward. It does not cover a Kubernetes LoadBalancer host. That host is an IP, which cannot be an RP ID, or a DNS name, which works like a custom domain. In both cases it needs `passkey-rp-host`, which the task allowed as the fallback. The reviewer judged this acceptable and raised no finding.

## Review

- Round 1: Blocked — open M1, M2, M3
- Round 2: Done — M1, M2, M3 fixed; disposition clean

## Notes

- Origin: Steve 2026-10-21, "create a all in one endpoint, remember it is a template and the faster we can get someone going the better. The pit of success, and make sure we have a skill for the users AI".
- Code: `tools/dev-cli/endpoints/deploy-command.cs`, `tools/dev-cli/services/aspire-deploy.cs`, `aspire-deploy-preflight.cs`, `aspire-publish.cs`.
- Wrap tools, don't reconstruct internals: call `aspire publish` / `aspire deploy` and handle failure.
