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

- [x] All `TimeWarp.State.*` pins on 12.0.0-beta.7
- [x] Palette prefers `DisplayName`, falls back to the generated label; Design region updated
- [x] "Link Microsoft 365" set; other palette-visible actions reviewed (list in Results)
- [x] Tests (authored label shown and ranked; fallback still works)
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`
- [x] Do **not** start an AppHost; record the browser check as not performed
- [x] Implementation review (clean, round 1)
- [ ] Host `open-pr`

## Session

- Created: 104771 (2026-10-01)
- Implement (2026-10-01, ganda task work implement oracle, Claude): pins, roster precedence, label, tests, gates.
- Review (2026-10-01, ganda task work review oracle, Claude Opus 5.5): effort 1, roster general, 1 round, disposition clean.

## Notes

- Task 265 (TWA0026) also edits web-spa: the command palette and `CredentialsState`. If 265 has
  merged, rebase onto it. If not, keep the edits narrow to avoid conflicts.
- Memory discipline: run builds serially; `dotnet build-server shutdown` before finishing.

## Results

**Pins.** `Directory.Packages.props`: `TimeWarp.State` and `TimeWarp.State.Plus` 12.0.0-beta.6 → 12.0.0-beta.7
(the only `TimeWarp.State.*` pins present). The beta.6 mention in the `program.cs` catalog comment moved too.

**Palette** (`command-palette-roster.cs`). New `CommandPaletteRoster.Label(ActionCatalogEntry)` shows
`action.DisplayName` when it is set, and falls back to the generated `DisplayName(action.Name)` only when it is null.
The ranker is unchanged: it still matches the row `Name` (the shown label) and the description. The Design region records
the precedence.

**Prefix decision.** The palette keeps "Owner: Action" for **every** command, authored or generated. The owner is the catalog
name's prefix, and `DisplayName` supplies only the action part. So `DisplayName = "Link Microsoft 365"` renders as
"Credentials: Link Microsoft 365", and rows still group by state no matter how their label was written.

**Labels reviewed** (palette-visible means Human or Both, plus no required argument):

| Action | Generated label | Decision |
|--------|-----------------|----------|
| `Credentials.LinkMicrosoft365` | Credentials: Link microsoft 365 | **Set** `DisplayName = "Link Microsoft 365"` (brand casing) |
| `Profile.SignOut` | Profile: Sign out | Leave unset (reads right) |
| `Credentials.AddPasskey` | Credentials: Add passkey | Leave unset |
| `Credentials.AddExistingPasskey` | Credentials: Add existing passkey | Leave unset |
| `Counter.IncrementCounter` (Both) | Counter: Increment counter | Leave unset. It needs `int amount`, so the palette hides it anyway |

No other Human/Both `[CatalogAction]` exists. The rest are Agent-only: RenameCredential, RevokeCredential, UpdateProfile,
CreateRole, UpdateSiteSettings.

**Tests.**
- `command-palette-tests.cs`: the roster pins "Credentials: Link Microsoft 365".
- `Rank_Link_Microsoft_365_First_For_Its_Authored_Label` covers `microsoft`, `Microsoft 365`, and `link microsoft`, and
  checks that the highlighted row is the link command.
- `Rank_Link_Microsoft_365_On_Its_Label_For_Link` checks that `link` ranks the row ahead of every row without "link" in its
  name. The "Agent Links" page can tie on word-start, and wins because its name is shorter.
- `Label_Commands_With_DisplayName_Else_The_Generated_Name` uses probe entries to show an authored label wins and a null
  `DisplayName` gets the generated label.
- `action-catalog-tests.cs`: `LinkMicrosoft365.DisplayName == "Link Microsoft 365"`, and `SignOut.DisplayName` is null.

**Gates** (run in this worktree):
- `dev build`: 0 warnings, 0 errors.
- `dev test`: exit 0. web-spa-integration-tests 129/129.
- `dev template-smoke`: SUCCEEDED. web-jaribu-tests 227/227.
- `ganda repo audit`: passes all checks. `--fix --checks bin-dev` built the local, gitignored `bin/dev`, and nothing was committed from it.

**Browser check not performed.** Per the brief, no AppHost was started.

**Stale assets.** A package bump can leave stale WASM `_framework` files behind. After merging, the maintainer should run
`dev clean` and clear the site data before testing in the browser.

### How to validate

Smoke:
1. `cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class CommandPalette`
2. `dotnet test -c Release -- --filter-class ActionCatalog`

Expect: both pass, 0 failures. The CommandPalette run includes the `Rank_Link_Microsoft_365_*` and
`Label_Commands_With_DisplayName_Else_The_Generated_Name` cases.

Maintainer, after merge:
1. Run `dev clean`, then `dev run`, and clear site data.
2. Press Ctrl-K and type `link`. "Credentials: Link Microsoft 365" shows with correct casing. Typing `microsoft`
   highlights it.

### Review disposition

- **Effort / roster:** effort 1 (by-diff, 174 lines), `general` only. 1 round.
- **Final counts:** bug 0, suggestion 0, nit 0. All are 0 open, 0 fixed, 0 wontfix.
- **Disposition:** `clean`, with no findings raised. The reviewer re-ran CommandPalette (33/33) and ActionCatalog (11/11), and `ganda repo audit` passed.
- **Artifacts:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
