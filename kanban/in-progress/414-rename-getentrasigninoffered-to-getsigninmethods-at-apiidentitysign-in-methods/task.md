# Rename GetEntraSignInOffered to GetSignInMethods at api/identity/sign-in-methods

## Description

Task 410 (PR #576) changed `GetEntraSignInOffered` (`features/identity/get-entra-sign-in-offered/`,
route `api/identity/entra/offered`) to return every offered sign-in method,
`{"methods":["Passkey","Microsoft365"]}`. The old name now says it answers "is Entra offered?",
which it no longer does. Rename the endpoint so its name and route say what it returns.

Depends on 410 being merged. Start from master after the merge.

## Goal (implementer restatement)

The anonymous read already returns every sign-in method the browser should render, but its type, folder, and route still say it only answers whether Microsoft 365 is offered. Rename that contract, its handler, its tests, and its folder to `GetSignInMethods` / `features/identity/get-sign-in-methods/`, with kebab file names. Move the route to `api/identity/sign-in-methods` and do not keep the old route. Point the SPA, server comments, deploy smoke probe, skills, and tests at the new name and route. Leave `EntraSignInOffer` as the Microsoft 365 rule. This contract has no `[Operation]` tool id. The ingress golden lists only the top-level `api/identity` prefix, so this route change does not rewrite it. Done means the old type, route, and folder are gone from `source`, `tests`, `skills`, `tools`, and `goldens`, and build, server and SPA integration tests, template-smoke, skill lint, and repo audit pass.

## Required outcome

- The contract `GetEntraSignInOffered` becomes `GetSignInMethods`, and its folder becomes
  `features/identity/get-sign-in-methods/`. That covers the contracts, handler and tests files,
  with kebab file names (`tw-csharp`, TW0001).
- The route `api/identity/entra/offered` becomes `api/identity/sign-in-methods`. No alias for the
  old route: the template ships the SPA and server together.
- Regenerate the ingress golden if the route affects it (`dev regenerate-generated`, then commit
  `goldens/ingress`).
- Callers that move to the new name and route (find them with
  `rg -n 'GetEntraSignInOffered|entra/offered|get-entra-sign-in-offered' source tests skills tools goldens`):
  - SPA `sign-in-state.fetch-offered-sign-in-methods.cs` and `SettingsPage.razor.cs`;
  - `get-site-settings-contracts.cs`, `get-credentials-handler-application.cs`,
    `sign-in-options-application.cs`, and `entra-sign-in-offer-application.cs` (Design comments);
  - the dev CLI `deploy-pipeline.cs` and `deploy-command.cs`, which probe the route after a deploy,
    and `tests/tools/dev-cli-tests/deploy-pipeline-tests.cs`;
  - `skills/tw-deploy/SKILL.md` and `skills/tw-deploy/references/azure-container-apps-neon.md`;
  - the server tests `site-settings-endpoint-tests.cs`, `site-settings-seed-on-read-tests.cs` and
    `credential-availability-tests.cs`, and the SPA tests `settings-page-microsoft-365-tests.cs`,
    `settings-page-passkey-buttons-render-tests.cs` and `sign-in-state-tests.cs`.
- `EntraSignInOffer` (`entra-sign-in-offer-application.cs`) keeps its name. It is still the
  Microsoft 365 rule that the methods read uses.
- Rename the `[Operation]` / tool id if the contract has one, and update anything that lists it.
- After the change, `rg -n 'GetEntraSignInOffered|entra/offered|get-entra-sign-in-offered' source tests skills tools goldens`
  is empty.

## Checklist

- [x] Contract, folder, files and handler renamed to GetSignInMethods
- [x] Route is `api/identity/sign-in-methods`; ingress golden regenerated if affected
- [x] SPA, server, dev CLI deploy probe, skills and tests updated
- [x] Old-name search is empty
- [x] `./bin/dev build` 0/0, server and SPA integration tests pass, `./bin/dev template-smoke`,
      `./bin/dev lint-skills`, `ganda repo audit`

## Results

`GetEntraSignInOffered` is now `GetSignInMethods` in `features/identity/get-sign-in-methods/` (`get-sign-in-methods-contracts.cs`, `get-sign-in-methods-handler-application.cs`, `get-sign-in-methods-tests.cs`). The route is `api/identity/sign-in-methods`. There is no alias. The SPA fetch, Settings comment, server design comments, `dev deploy` smoke paths, `tw-deploy`, and the listed tests use the new name and route. `EntraSignInOffer` is unchanged. This contract has no `[Operation]` tool id.

`IngressRoutePrefixGenerator` keeps only the top-level `api/<segment>` prefix. Both the old and new routes sit under `api/identity`, so `goldens/ingress/WebServerApiRoutePrefixes.g.cs` does not change and was not regenerated.

`rg -n 'GetEntraSignInOffered|entra/offered|get-entra-sign-in-offered' source tests skills tools goldens` is empty. Older mentions stay in `kanban/done/`.

`./bin/dev build` succeeded with 0 warnings and 0 errors. Co-located `get-sign-in-methods-tests.cs` passed 5/5. `dev-cli-tests` passed 342/342. `web-spa-integration-tests` passed 500/500. `web-server-integration-tests` passed 357 with 1 skipped and 0 failed. `./bin/dev template-smoke` succeeded (generated web Jaribu 405/405, api Jaribu 9/9, timewarp-testing 14/14). `./bin/dev lint-skills` passed 9/9. `ganda repo audit` exited 0. The only finding is the pre-existing `kanban-landed` warning for eight other claims that have no worktree on this machine.

### How to validate

Smoke: from a running app, `curl -fsS "https://<ingress-or-local-host>/api/identity/sign-in-methods"`. The co-located stand-in is `dotnet run --file source/container-apps/web/features/identity/get-sign-in-methods/get-sign-in-methods-tests.cs`.

Expect: HTTP 200 and a JSON object whose payload is `methods` (Passkey when it is listed; Microsoft365 when it is listed and site settings allow Entra sign-in). `GET /api/identity/entra/offered` is not a route.

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-25)
- Implementation: implementer oracle (2026-10-25). Audit advisory `kanban-landed` names other claims (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244) and was left as-is.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-25T12:01:17Z

## Notes

- Found while fixing PR #576's stale test. Steve 2026-10-25: "yes file the rename task".
