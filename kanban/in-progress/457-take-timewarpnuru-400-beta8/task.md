# Take TimeWarp.Nuru 4.0.0-beta.8

## Description

TimeWarp.Nuru 4.0.0-beta.8 was released on 2026-10-30 (release run 41041359868, success). Bump
`TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` from `4.0.0-beta.7` to `4.0.0-beta.8` in
`Directory.Packages.props`. Fix anything that breaks in `tools/dev-cli/` or other consumers.

The `TimeWarp.Nuru` beta.8 nupkg is on the nuget.org flat container (HTTP 200), but the search
index still listed beta.7 when this task was filed. That is why the `nuru` audit check reported
"up to date". Restore uses the flat container, so it works.

## Requirements

- Bump both pins to `4.0.0-beta.8`. No `VersionOverride`, no backward pins.
- Fix any API breaks in the dev CLI and other consumers. Do not suppress warnings.
- After the bump, rebuild the dev CLI (`dev self-install`, or run it from source) so the gates
  run the new Nuru, not the stale `bin/dev`.
- Run `dev check-version`. `2.0.0-beta.30` is not published yet (task 456), so no further
  `<Version>` bump should be needed; take one only if check-version asks for it.
- Gates: `ganda repo audit`, `dev build` (0 warnings, 0 errors), `dev test`,
  `dev template-smoke`, `dev check-version`.

## Checklist

- [ ] Pins bumped to 4.0.0-beta.8
- [ ] Breaks fixed; dev CLI rebuilt
- [ ] Gates green

## Session

- Created: 8d5a7906-5253-4dc5-b6d4-729819f6f6b0 (2026-10-30)

## Notes

- Nuru releases itself; this task only takes the pin.

## Results
