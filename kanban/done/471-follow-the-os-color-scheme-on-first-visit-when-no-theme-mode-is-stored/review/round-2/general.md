# Round 2 — general
**Date:** 2026-11-04
**Scope reviewed:** delta since round 1 (base `8e45abfa7`): commit `0ecd7649f`
(`skills/tw-blazor-css-strategy/SKILL.md` rules 8 and 9, `skills/tw-blazor-layout/SKILL.md` step 1,
plus kitchen files). Re-verified prior M1.

## Summary

The fix pass changes only skill prose. Rule 8 now says the server renders `data-theme="system"`
and that `BrandThemeScript` is the first child of `<body>`. It says the script sets `body[data-theme]`
from the stored `light`/`dark` mode under `fluentui-blazor:theme-settings`, else from
`prefers-color-scheme: dark`, and never stores a mode. Rule 9 names who sets the dark attribute.
`tw-blazor-layout` step 1 points at the script. I checked each claim against
`source/container-apps/web/projects/web-spa/configuration/brand-theme-script.cs` and `App.razor:68`.
They match: same storage key, same precedence, same placement, no mode written. I found no new defect.

Re-ran `dotnet run tools/dev-cli/dev.cs -- lint-skills`: 9/9 skills pass and the eval spec is valid.
The delta has no product code, so the build and Playwright suites were not re-run. The round-1
reviewer ran them and they passed.

## Issues

None. M1 (round 1) is verified fixed.
