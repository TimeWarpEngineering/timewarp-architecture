# Rename GetEntraSignInOffered to GetSignInMethods at api/identity/sign-in-methods

## Description

Task 410 (PR #576) changed `GetEntraSignInOffered` (`features/identity/get-entra-sign-in-offered/`,
route `api/identity/entra/offered`) to return every offered sign-in method,
`{"methods":["Passkey","Microsoft365"]}`. The old name now says it answers "is Entra offered?",
which it no longer does. Rename the endpoint so its name and route say what it returns.

Depends on 410 being merged. Start from master after the merge.

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

- [ ] Contract, folder, files and handler renamed to GetSignInMethods
- [ ] Route is `api/identity/sign-in-methods`; ingress golden regenerated if affected
- [ ] SPA, server, dev CLI deploy probe, skills and tests updated
- [ ] Old-name search is empty
- [ ] `./bin/dev build` 0/0, server and SPA integration tests pass, `./bin/dev template-smoke`,
      `./bin/dev lint-skills`, `ganda repo audit`

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-25)

## Notes

- Found while fixing PR #576's stale test. Steve 2026-10-25: "yes file the rename task".
