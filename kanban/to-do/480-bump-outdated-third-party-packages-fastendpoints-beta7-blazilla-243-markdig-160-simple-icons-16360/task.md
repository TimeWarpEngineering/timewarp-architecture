# Bump outdated third-party packages: FastEndpoints beta.7, Blazilla 2.4.3, Markdig 1.6.0, simple-icons 16.36.0

## Description

`ganda nuget outdated` on 2026-11-05 lists five outdated pins, all third party. Bump them in
`Directory.Packages.props` and prove the build, tests, and template smoke still pass.

| Package | Current | Target |
|---|---|---|
| FastEndpoints, FastEndpoints.OpenApi | 9.0.0-beta.6 | 9.0.0-beta.7 |
| Blazilla | 2.4.2 | 2.4.3 |
| Markdig | 1.5.1 | 1.6.0 |
| timewarp-simple-icons | 16.35.0 | 16.36.0 |

## Requirements

- Bump only these pins. Leave TimeWarp.* and Microsoft rc pins alone.
- FastEndpoints: read the beta.7 release notes. The generated endpoints come from
  `fast-endpoint-emitter.cs` and the documentation resolver (task 472); if beta.7 changes any API the
  emitter targets, fix the emitter and its tests, not the generated output.
- Blazilla: the four forms (`RoleForm`, `TodoItemForm`, `ProfilePage`, admin `AuthenticationPage`)
  bind `EditForm` to a details interface with an explicit validator instance. Confirm 2.4.3 keeps
  that; run the web-spa integration and Playwright suites.
- No behavior changes, no refactors riding along. `ganda repo audit --fix` output is policy.
- Version: run `dev check-version`. Package pins in the template ship to generated apps; bump
  `<Version>` and the platform pins only if check-version says the current version is published.

## Checklist

- [ ] Pins bumped in Directory.Packages.props
- [ ] FastEndpoints beta.7 notes read; emitter unaffected or fixed with tests
- [ ] dev build 0/0, dev test fast + integration, Playwright, dev template-smoke
- [ ] check-version handled

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-05)

## Notes

Steve 2026-11-05: "create small task to update all and launch it". Blazilla is kept on purpose:
it accepts an explicit validator instance, which the shared I*Details validator pattern needs
(`skills/tw-web-api-contracts/references/mutability.md`).
