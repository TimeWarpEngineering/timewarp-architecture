# Fix api-off template smoke (static-render-host missing Options using) and bump to 2.0.0-beta.25

## Description

The nightly matrix template smoke (run 40092118522, 2026-10-24) fails the `SmokeNoApi` variant:

```
tests/container-apps/web/web-spa-integration-tests/infrastructure/static-render-host.cs(41,36):
error CS0246: The type or namespace name 'IOptions<>' could not be found
error CS0103: The name 'Options' does not exist in the current context
```

`static-render-host.cs` (added by 379-017) uses `IOptions<>` / `Options.Create`, but
`Microsoft.Extensions.Options` is a global using only under `#if(api)` in that project's
`global-usings.cs`. With `--api false` the test project does not compile. The other variants pass.

This blocks the beta.25 release, so the version bump rides in the same PR.

## Required outcome

- Add `using Microsoft.Extensions.Options;` to `static-render-host.cs`, matching
  `features/application/ask-starter-prompts-tests.cs`. Do not change the `#if(api)` block in
  `global-usings.cs`.
- Bump `<Version>` in `source/Directory.Build.props` from `2.0.0-beta.24` to `2.0.0-beta.25`, with
  any platform package pins `dev check-version` requires, in the same commit (`tw-pr`).
- Gates: `dev build` 0/0; `dev check-version`; `dev template-smoke --matrix --app-tests` passes all
  variants, or at least `SmokeNoApi` plus the default variant if the full matrix cannot run.
  Report which ran.

## Checklist

- [ ] Using added to static-render-host.cs
- [ ] Version bumped to 2.0.0-beta.25 (plus pins)
- [ ] dev build 0/0, dev check-version clean
- [ ] SmokeNoApi and default template smoke pass

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-24)

## Notes

Steve asked for the beta.25 cut on 2026-10-24. The release is held on this.
