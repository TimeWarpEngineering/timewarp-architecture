# Migrate FluentUI components to v5 API (per-component) + shadow-DOM audit

Parent: 059. Pairs with 059-003.

## RUNTIME VERIFIED (2026-06-22) — page renders, zero console errors

App loads in the browser on v5 with no console errors. Gotcha hit + fixed: the v4→v5 bump
left a **stale `_framework/*.wasm`** (v4 FluentUI, no `FluentProviders`) → runtime
`TypeLoadException` → blank page, despite a green build. Fix = `rm -rf obj
bin/.../wwwroot/_framework` + clean rebuild + Aspire `rebuild` of `web` + browser hard-refresh.
See memory `blazor-wasm-stale-framework-asset-gotcha`. Remaining: look/polish is 059-005;
`fluent.css` still imports v4 bundle paths (fix in 059-003/006); FluentLabel→FluentText
cleanup; shadow-DOM audit.

## Progress (2026-06-22) — BUILDS CLEAN on v5 RC

web-spa AND web-server build with **0 errors/0 warnings** on `5.0.0-rc.3-26138.1`.
Fixes applied (all build-breaking v5 changes):
- Nav: `FluentNavMenu`→`FluentNav`, `FluentNavGroup`→`FluentNavCategory`,
  `FluentNavLink`→`FluentNavItem`, `Icon`→`IconRest` (LeftPane_Main, TimeWarpNavLink,
  TimeWarpNavUrl, link-helper.cs).
- `AuthorizedFluentNavLink`: rewritten as **composition** (was `@inherits FluentNavLink`) —
  v5 `FluentComponentBase` needs a `LibraryConfiguration` ctor arg, so Fluent components can
  no longer be subclassed with a parameterless ctor.
- Providers: 6 individual providers → single `<FluentProviders />` (FluentUIRequiredFeatures).
- Layout: `FluentHeader`/`FluentFooter` → native `<header>`/`<footer>` + scope handle.
- `FluentTextField` → `FluentTextInput` (slot=end span → `<EndTemplate>`); selector
  `fluent-text-field`→`fluent-text-input`.
- `FluentPersona` (removed) → `FluentAvatar` + label as menu trigger.
- `FluentMenu`: `Anchor`/`@bind-Open`/`VerticalThreshold` → `Trigger=<id>` (popover); removed
  Open state.
- `FluentProgressRing` → `FluentSpinner`.
- Enums: `Appearance.*` → `ButtonAppearance.*` (Accent→Primary, Stealth→Subtle,
  Lightweight→Transparent, Neutral/Filled/Hypertext→Default, Outline→Outline);
  `Color.Neutral` → `Color.Default`.
- `FluentMultiSplitter.RemovePane` (gone) → conditional `@if (ShowLeftPane)` pane render.
- Toast: `IToastService` only has `ShowToastAsync(Action<ToastOptions>)` in v5 — rewrote the 3
  handlers (`ShowToast`/`ShowError` → `ShowToastAsync(o => { o.Intent=…; o.Title=…; o.Timeout=…; })`,
  made async).
- Icons: kept `.Icons`/`.Emoji` at 4.14.2 (no v5 exists); compile-compatible with v5. The
  `Icons.Regular.SizeNN.X()` syntax is unchanged in v5 (per MCP).

### STILL TODO (not build-breaking)
- [ ] **Runtime verification** — app not yet run on v5. Verify: providers/toasts render,
      menu opens via `Trigger`, avatar shows, dialog (FluentDialog rearchitected), splitter
      resize, and that v5 base CSS actually loads (see `fluent.css` note below).
- [ ] **`fluent.css`** still `@import`s v4 paths (`reboot.css` + `*.bundle.scp.css`). Verify
      the v5 asset/bundle path and update `web-server/components/App.razor` (coordinate w/ 059-006).
- [ ] **FluentLabel → FluentText** cleanup (~22 usages still on `FluentLabel`; compiles but
      deprecated — migrate for no-tech-debt; note `Typo`→`As`/`Weight`/`Size`).
- [ ] **Shadow-DOM audit** of the Fluent primitives actually used (Exception-A surface).
- [ ] Icons 4.x + v5 runtime check (and whether timewarp-heroicons/simple-icons still resolve).

## Goal

Update every Fluent component usage in web-spa + web-server to the v5 API. v5 wraps Fluent
Web Components **v3** (v4 wrapped v2): property/attribute names and enum values changed
broadly, and some components were renamed/removed. This is **component-by-component** work
using the FluentUI MCP migration docs.

## Component inventory (from web-spa + web-server, 2026-06-22)

| v4 component | count | v5 mapping (verify via MCP docs) |
|---|---|---|
| `FluentLabel` | 22 | → **`FluentText`** (`As="TextTag.*"`, `Weight="TextWeight.*"`, `Size="TextSize.*"`) — confirmed in crunchit |
| `FluentButton` | 12 | `Appearance` enum → **`ButtonAppearance.*`** (e.g. `Transparent`) — confirmed |
| `FluentStack` | 9 | light-DOM div; check `Orientation`/`*Gap`/`*Alignment` enums |
| `FluentSpacer` | 7 | verify still present |
| `FluentNavLink`/`FluentNavGroup`/`FluentNavMenu` | 6/4/1 | → **`FluentNav`/`FluentNavItem`** (`Match="NavLinkMatch.*"`) — confirmed in crunchit `NavMenu.razor` |
| `FluentIcon` | 5 | verify icon type/`Value` API under v5 Icons package |
| `FluentMultiSplitter`(+Pane) | 2/4 | **CONFIRMED present in v5 RC** (`FluentMultiSplitter`, `FluentMultiSplitterPane`, `FluentMultiSplitterResizeEventArgs` all in `5.0.0-rc.3-26138.1`). Keep it. Verify `Size/Min/Max/Collapsible/Collapsed` + `RemovePane` API unchanged. Used by `TimeWarpPage` + `RightPane_Main`. |
| `FluentMenu`/`FluentMenuItem` | 1/4 | verify API + provider model |
| `FluentTextField` | 1 | shadow-DOM primitive; verify v5 field API |
| `FluentCard` | 1 | verify |
| `FluentDialog` | 1 | v5 uses native `<dialog>` — API changed |
| `FluentPersona` | 1 | confirm exists in v5 |
| `FluentProgressRing` | 1 | confirm naming |
| `FluentDivider` | 1 | verify |
| `FluentHeader`/`FluentFooter` | 1/1 | may map to `FluentLayout`/`FluentLayoutItem Area=` (crunchit uses FluentLayout) |

No `FluentDataGrid`/`QuickGrid` in markup, no `FluentDesignTheme` — nothing to migrate there.

## Tasks

- [ ] For each component above, pull its v5 "how to migrate" doc via the FluentUI MCP and
      apply the rename/enum/attribute changes.
- [x] **Availability check** against `5.0.0-rc.3-26138.1` dll: present →
      `FluentMultiSplitter`(+Pane), `FluentLayout`/`FluentLayoutItem`/`FluentLayoutHamburger`,
      `FluentNav`/`FluentNavItem`/`FluentNavCategory`/`FluentNavSectionHeader`, `FluentCard`,
      `FluentStack`, `FluentTabs`/`FluentTab`, `FluentTreeView`, `FluentSplitButton`,
      `FluentGrid`/`FluentGridItem`, `FluentDataGrid`. Still need to confirm presence/renames
      of `FluentPersona`, `FluentProgressRing`, `FluentMenu`, `FluentTextField`,
      `FluentSpacer`, `FluentDivider`, `FluentHeader`/`FluentFooter` (check via MCP docs).
- [ ] **Shadow-DOM audit (Exception A surface):** probe the live v5 DOM (`el.shadowRoot`) for
      each interactive component we use to classify shadow-DOM (→ `::part()`/vars) vs
      light-DOM (→ class/scope-handle). Known: `fluent-button`, `fluent-text` are shadow-DOM;
      `FluentStack` is light-DOM.
- [ ] Update `_Imports.razor` / `global-usings.cs` namespaces if v5 moved any.
- [ ] Icons: FluentUI `Icons.Regular.*` (~26 uses) get a v5 Icons major — update.
      `timewarp-heroicons` / `timewarp-simple-icons` (`Icons.Outline.*`, `Icons.Solid.*`,
      `Icons.GithubIcon`) and raw `<Svg>` are **NOT** FluentUI — leave untouched, just verify
      no naming clash with the aliased `Icons = Microsoft.FluentUI...Icons`.
- [ ] Verify with Playwright screenshots against the running app.

## Notes

`AssemblyMarker`, MediatR, FastEndpoints, TimeWarp.State are unaffected — this is purely the
Fluent component surface + icons.
