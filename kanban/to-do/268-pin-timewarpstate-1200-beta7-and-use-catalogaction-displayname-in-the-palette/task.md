# Pin TimeWarp.State 12.0.0-beta.7 and use CatalogAction DisplayName in the palette

## Description

TimeWarp.State 12.0.0-beta.7 is live on nuget.org (timewarp-state task 094, PR #614, released
2026-10-01). It adds `[CatalogAction(DisplayName = …)]`, surfaced as
`ActionCatalogEntry.DisplayName` (`string?`, null when unset). The new TWS0008 rule rejects an
empty value.

Today the Ctrl-K palette derives labels from the catalog `Name`, so `Credentials.LinkMicrosoft365`
reads "Credentials: Link microsoft 365". Steve decided on 2026-10-01 that palette labels are
written deliberately, through `DisplayName`.

## Requirements

1. **Pins.** In `Directory.Packages.props`, move `TimeWarp.State` and `TimeWarp.State.Plus` from
   12.0.0-beta.6 to 12.0.0-beta.7, together with every other `TimeWarp.State.*` pin present.
   Pins move forward only.
2. **Palette.**
   - `CommandPaletteRoster` uses `action.DisplayName` when it is set, and falls back to today's
     generated `DisplayName(action.Name)` only when it is null.
   - Record the precedence in the roster's Design region.
   - Ranking keeps matching on whatever label is shown, and on the description.
3. **Labels.**
   - Set `DisplayName = "Link Microsoft 365"` on `CredentialsState.LinkMicrosoft365`.
   - Review every other `[CatalogAction]` the palette shows (Human / Both): Sign out,
     Increment counter, Add passkey, Add existing passkey, and any added since. Set `DisplayName`
     wherever the generated label reads poorly (brand casing, awkward splits).
   - Leave it unset where the generated label is already right.
   - The palette's "Owner: Action" prefix is a presentation choice. Keep it consistent across
     authored and generated labels, and record which way you went.
4. **Tests.**
   - Palette and catalog tests: "Link Microsoft 365" appears exactly so, and typing
     `microsoft` / `link` ranks it.
   - An action without `DisplayName` still gets the generated label.
5. **Stale assets.** A package bump can leave stale WASM `_framework` files behind. Note in
   Results that the maintainer should run `dev clean` and clear site data after merging.

## Checklist

- [ ] All `TimeWarp.State.*` pins on 12.0.0-beta.7
- [ ] Palette prefers `DisplayName`, falls back to the generated label; Design region updated
- [ ] "Link Microsoft 365" set; other palette-visible actions reviewed (list in Results)
- [ ] Tests (authored label shown and ranked; fallback still works)
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`
- [ ] Do **not** start an AppHost; record the browser check as not performed
- [ ] Implementation review; host `open-pr`

## Session

- Created: 104771 (2026-10-01)

## Notes

- Task 265 (TWA0026) also edits web-spa: the command palette and `CredentialsState`. If 265 has
  merged, rebase onto it. If not, keep the edits narrow to avoid conflicts.
- Memory discipline: run builds serially; `dotnet build-server shutdown` before finishing.

## Results

*(fill when done)*

### How to validate

*(required before done)*

Maintainer, after merge:
1. Run `dev clean`, then `dev run`, and clear site data.
2. Press Ctrl-K and type `link`. "Credentials: Link Microsoft 365" (or the chosen format) shows
   with correct casing.
