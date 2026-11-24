# Trial pinned impeccable detector on the template UI

## Description

Research only, small and time-boxed. Run a pinned exact npm version of `npx impeccable detect` ([pbakaus/impeccable](https://github.com/pbakaus/impeccable), Apache-2.0) against the Architecture template UI in two ways, record hits, misses and false positives, then recommend whether to add a CLI-only CI detect step (B), fold the rules that actually fired into `tw-blazor-css-strategy` (D), or neither.

Source: [Impeccable eval](https://github.com/TimeWarpEngineering/timewarp-intel/blob/master/evaluations/2026-11-11-impeccable.md) ([timewarp-intel PR #121](https://github.com/TimeWarpEngineering/timewarp-intel/pull/121)). Busara verdict: REWORK (med). C is GREEN and folds in B, K and L. The assumption most likely to fail is that FluentUI shadow DOM or tokens flood the scan with false positives.

Filed 2026-11-11 at Steven's request ("Yes, create the Impeccable detector trial task in timewarp-architecture.").

## Requirements

1. **Pin an exact npm version (K, folded).** Use a pinned `npx impeccable@<exact-version> detect`. Record the version in Results. Do not float to latest.
2. **Rendered URL scan.** Start the template and run detect against the running page URL. The rules that need rendering (low contrast, tiny text) only run on a URL scan. Blazor renders to plain HTML and CSS, so this scan avoids the detector's lack of a built-in `.razor` type.
3. **Source scan.** Also run detect against `.razor` / `.razor.css` sources via `detector.extensions`.
4. **FluentUI and brand ignores (L, folded).** Add ignore config for FluentUI web components and design tokens, plus the brand typography, so brand fonts and FluentUI internals are not reported as slop.
5. **Triage (time-boxed).** Record hits, misses and false positives, sorting them into actionable signal vs noise. Note anything hidden by shadow DOM.
6. **Recommend, do not implement, B and D.** From the triage, recommend whether to add (B) a CLI-only CI detect step and (D) the rule ideas that actually fired into the `tw-blazor-css-strategy` skill, or neither. Do not add the CI step or edit the skill in this task.

## Out of scope

Busara gave all of these a KILL:

- The Impeccable skill pack (A)
- Agent hooks (E)
- Live mode (F)
- A Chrome extension (I)
- Adopting PRODUCT.md / DESIGN.md (G)
- Cross-linking paperwork to the C# anti-slop eval (H)
- Lifting the command vocabulary into `tw-blazor*` skills (J), unless this trial later shows uncovered categories

Also out of scope: adding a CI detect step or editing `tw-blazor-css-strategy` in this task. Those are follow-ups only if the trial shows a useful signal-to-noise ratio.

## Goal (implementer restatement)

Find out whether the Impeccable detector tells us anything useful about the template UI, at a
fixed version, without changing the product. Run it two ways: against the running app (rendered
pages, where the contrast and text-size rules work) and against the `.razor` / `.razor.css`
sources. Give it ignore rules for FluentUI, the design tokens and the brand font. Sort what it
reports into real problems, noise, and things it cannot see (shadow DOM). End with a
recommendation on B (a CI detect step) and D (borrow rules into `tw-blazor-css-strategy`).

Acceptance:

- The exact npm version is recorded and every run used it.
- A URL scan of the running template ran and its findings are recorded.
- A source scan with `detector.extensions` covering `.razor` ran and its findings are recorded.
- An ignore config for FluentUI, tokens and brand typography exists in this task folder.
- Hits, misses and false positives are listed, with what shadow DOM hides.
- B and D each get a recommendation. No CI step is added and the skill is not edited.

## Checklist

- [x] Pin an exact npm version and record it
- [x] Scan the running template URL
- [x] Scan `.razor` / `.razor.css` via `detector.extensions`
- [x] Add FluentUI, token and brand-typography ignores
- [x] Record hits, misses and false positives (time-boxed triage)
- [x] Recommend CI detect (B), rule borrow into `tw-blazor-css-strategy` (D), or neither — do not implement those follow-ups here

## Session

- Created: 3075946 (2026-11-11)
- 2026-11-25 implementer (Claude, ganda task work): ran both scans, wrote the ignore config
  (`impeccable/.impeccable/config.json`), triaged, recommended. No product code changed.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 672a0492-4acb-4a0a-b6e1-05a34fe55e91, max-turns 500, Done — 2026-11-24T19:58:13Z

## Results

### Setup

- **Version:** `impeccable@4.11.0` (npm `latest` on 2026-11-25), installed into a scratch
  directory and run from there. Its native engine is the optional dependency
  `@impeccable/cli-linux-x64@0.1.30`. Every run set `IMPECCABLE_NO_TELEMETRY=1` and
  `DO_NOT_TRACK=1`: telemetry is **on by default** in this CLI. The committed config also sets
  `"telemetry": false`.
- **Config:** `impeccable/.impeccable/config.json` in this task folder, written with
  `impeccable ignores add-file` / `add-value`. The CLI reads `.impeccable/config.json` from the
  current directory, so every configured run used this folder as the working directory. It is not
  at the repo root, because adopting it there is follow-up B. `kanban/` task folders are not packed
  into the template.
- **App:** `Web.Server` alone (Release build, `ASPNETCORE_ENVIRONMENT=Development`,
  `http://localhost:54880`), started with `dotnet run --no-build`. The AppHost was not started, so
  no Aspire user secrets were touched. With no Postgres connection string the server keeps
  principals in memory.
- **Signed-in pages:** 20 of the 21 pages need a session. A scratch Playwright script made a
  passkey account with Chrome's virtual authenticator (the same steps as
  `SignInWithNewPasskeyAsync` in the Playwright tests). The first account is Administrator. The
  script added Developer through `PUT /api/admin/principals/{id}/roles`, then opened each page and
  ran `impeccable detect --json --cdp 9333 --target <url>` against that tab. The anonymous home
  page was also scanned as a plain URL (`impeccable detect http://localhost:54880/`).
- **Browser:** on this machine the `google-chrome` wrapper on `PATH` points at a Chromium build
  that is gone, and the plain URL scan failed until `IMPECCABLE_BROWSER` pointed at an installed
  Playwright Chromium. `CHROME_PATH` and `PUPPETEER_EXECUTABLE_PATH` worked as well.

### Source scan (`.razor` / `.razor.css`)

Command (from `impeccable/`): `impeccable detect source/container-apps/web` (absolute path).

- Without `detector.extensions` the CLI skips `.razor` and scans 142 `.css` / `.js` / `.ts` files
  with regex only. It says so: element rules (tiny-text, low-contrast) do not run.
- `{ "ext": ".razor", "engine": "html" }` makes it parse `.razor` as HTML. On a planted file,
  `engine: "regex"` gave the same findings. On the template it adds nothing: markup has almost no
  inline styles, and the HTML engine does not link a component to its scoped `.razor.css`. The
  stylesheet links in `App.razor` (`css/app.css`, `Web.Spa.styles.css`, the FluentUI bundle) do not
  resolve from source paths, and the CLI warns about each one.
- **Result, with or without the ignore config: 1 finding.** `side-tab` at
  `CommandPalette.razor.css:20` (`.twe-palette__row--active`, `box-shadow: inset 3px 0 0`).
- **Misses against the URL scan:** the same `side-tab` pattern in `DevNote.razor.css`
  (`border-left: 3px` with `border-radius: var(--twe-radius)`) and in `DesignSystemCatalog.razor.css`
  (`.ds-catalog__link[aria-current="page"]`). The regex engine does not resolve `var()` and did not
  match the selector form there. The source scan found 1 of 3 instances.
- **Planted check** (a scratch `.razor` + `.razor.css` with obvious slop): it caught gradient text,
  low contrast and 9px text in inline markup, and `side-tab`, `overused-font` and gradient text in
  the CSS. It missed `font-family: Inter` inline in markup, marketing buzzwords in the heading,
  the purple-to-cyan gradient (`ai-color-palette`) and a chromatic glow shadow (`dark-glow`).

### URL scan (rendered pages)

22 scans: 19 pages in light mode, plus `/Settings`, `/Admin/Roles` and `/DesignSystem` in dark
mode (stored Fluent mode `dark`). Viewport 1280×800. `/clients` was not scanned (it needs
`clients.read`, which no built-in role in this run had). `/Login` redirects to `/` once signed in.

Totals were **identical with and without the ignore config**: 89 findings (37 low-contrast
advisories, 3 low-contrast warnings, 16 line-length, 7 straggler advisories, 4 side-tab,
4 icon-tile-stack, 16 nested-cards, 2 em-dash advisories). Of the 62 rules, 7 fired.

| Rule | Where | Verdict |
|------|-------|---------|
| `low-contrast` 4.3:1 | `--twe-muted` `#767a86` on `#ffffff` (`span.search-end` "Ctrl-K" in the app bar on every page; helper text; 82 elements on `/DesignSystem`) | **Real.** 4.29:1 is below AA 4.5:1 for 12px text. |
| `low-contrast` 3.9:1 | `--twe-muted` on `--twe-paper-2` `#f5f5f7` (segmented control, `/DesignSystem`) | **Real.** 3.94:1. |
| `low-contrast` 4.2:1 | `--twe-link` = `--twe-secondary` `#0085b2` on white (`a.twe-breadcrumb__link`, every signed-in page) | **Real.** 4.20:1. `tw-brand` checks dark variants and `OnPrimary`, not light-mode secondary as link text. |
| `low-contrast` 3.3:1 (dark) | links `#8773b9` on `#303030` (table links on `/Admin/Roles`, definition-list links on `/DesignSystem`) | **Real, and not known.** `tokens.css` and `/brand.css` say dark links are `--twe-secondary-text` `#00ccff`, but every content link checked in dark mode (not the Fluent nav items) computes to `rgb(135, 115, 185)`. Something overrides `a { color: var(--twe-link) }` in dark mode. |
| `line-length` ~88–136 chars | `p.ds-subtitle` page subtitles and card body paragraphs (`/AgentLinks`, admin pages, `/DesignSystem`) | **Real but minor.** Prose in wide cards has no `max-width` in `ch`. |
| `straggler` (advisory) | one-word last lines in subtitles | **Minor.** `text-wrap: pretty` on body copy fixes it in one rule. |
| `side-tab` | `.twe-palette__row--active`, `.ds-catalog__link[aria-current]` | **False positive.** These mark the selected row or link. Fluent's own nav marks selection with a side bar. |
| `side-tab` | `div.twe-devnote` (`border-left: 3px` on a 12px-radius card) | **Arguable.** This is the pattern the rule names. It is a developer-only callout. |
| `nested-cards` (8 per scan) | `/DesignSystem` only: each sample sits in the catalog's `section.twe-card` frame (KPI, chart card, empty state, integration card, grouped table, `fluent-message-bar`) | **Noise.** An effect of the catalog frame. No product page reported nested cards. |
| `icon-tile-stack` | `div.twe-empty-state` (48px icon tile above the `h3`) | **Noise.** The standard empty-state layout. |
| `em-dash-overuse` (advisory) | `/DesignSystem` body copy (25 em-dashes) | **Copy, not CSS.** Out of scope for D. |

**Signal vs noise:** the contrast findings (4 distinct real problems, one of them a dark-mode bug
nobody had found) and line length are actionable. The `slop` category produced only noise or
arguable hits on this UI: 26 of the 89 findings, all of them on `/DesignSystem` (the catalog link,
the DevNote callout, the catalog frames, the empty state, and the em-dash count).

### FluentUI, tokens, brand typography, and shadow DOM

- **The feared false-positive flood did not happen.** Every ignore in the config is a no-op on
  this template today. Segoe UI is not on the `overused-font` list. The purple and blue brand
  colors are flat fills, not gradients, so `ai-color-palette` never fired. The
  `design-system-*` rules stay quiet without a `DESIGN.md` / `.impeccable/design.json` (G, out of
  scope). The ignores stay in the config so a future rebrand to a listed font, or a later
  `DESIGN.md`, does not flood the scan.
- **Shadow DOM hides FluentUI completely.** On `/` all 17 `fluent-*` elements have open shadow
  roots (`fluent-button`, `fluent-text-input`, `fluent-menu`, `fluent-avatar`, `fluent-tooltip`,
  and others). A probe injected the same 9px `#c8c8c8`-on-white paragraph into the light DOM, an
  open shadow root and a closed one. The scan reported the light-DOM copy (`low-contrast` 1.7:1 and
  `tiny-text`) and **neither shadow copy**. Text we slot into a Fluent component is light DOM and is
  checked (`span.search-end` inside `fluent-text-input`'s end slot). Anything Fluent renders inside
  its own shadow root, such as placeholders, internal labels and focus rings, is never checked.
- There is no selector-level ignore. Ignores are by rule, by file glob, or by rule + value.

### Other misses

- `tiny-text` / `undersized-ui-text` did not fire on the 11px eyebrow or the 12px helper text. Its
  threshold is below those sizes.
- `--viewport` is refused with `--cdp`, so signed-in pages were scanned at 1280×800 only. The
  anonymous home page at 390×844 added one `straggler` advisory.
- A URL or `--cdp` scan has no source lines (`line: 0`). Findings carry a CSS selector, which
  you map back to the `.razor.css` by hand.

### Recommendation

**B (CLI-only CI detect step): no.** The source scan, the only form that runs without a host, found
one finding and missed two of three instances of its own rule. The useful findings need the
rendered, signed-in app. In CI that means a passkey sign-in and a role grant, which the
`web-spa-playwright-tests` lane already does with `HostGraphFactory`. A detect step would add an
npm CLI with a native binary and default-on telemetry for the contrast findings alone, and it
cannot see inside Fluent's shadow roots. Instead, check the real problems in our own tooling:

1. Follow-up: fix the four contrast problems. Darken `--twe-muted` (light) until it reaches
   4.5:1 on `--twe-paper-2`. Give light-mode link text its own step that reaches 4.5:1 on white,
   because `--twe-secondary` is 4.20:1. Find what turns dark-mode `<a>` into `#8773b9` instead of
   `--twe-secondary-text`.
2. Follow-up: a build check or Jaribu test that computes the contrast of each text-role token pair
   (light and dark, from `tokens.css` plus the `BrandOptions` defaults) and fails below 4.5:1.
   This follows the repo rule "when two things must agree, add a build check". It also catches
   computed-color overrides like the dark link if it runs in the Playwright dark-theme lane.

**D (borrow rules into `tw-blazor-css-strategy`): yes for two rules, no for the rest.**

- Borrow **measure**: body copy and page subtitles get a `max-width` around 65–75ch. The detector
  found 16 lines over 80 characters.
- Borrow **`text-wrap: pretty` on body copy** to remove one-word last lines.
- Route **contrast** to `tw-brand` and the check in B.2, not to the CSS-strategy skill. Prose alone
  did not stop these four.
- Do not borrow `side-tab`, `nested-cards`, `icon-tile-stack` or the other slop rules. On this UI
  they were false positives or catalog artifacts.

J stays out: the trial found no uncovered category that needs Impeccable's command vocabulary.

### How to validate

Smoke (source scan with the committed config; needs Node 22+ and network for npx):

```bash
cd kanban/in-progress/488-trial-pinned-impeccable-detector-on-the-template-ui/impeccable
IMPECCABLE_NO_TELEMETRY=1 npx --yes impeccable@4.11.0 ignores list
IMPECCABLE_NO_TELEMETRY=1 npx --yes impeccable@4.11.0 detect "$(git rev-parse --show-toplevel)/source/container-apps/web"
```

Expect: `ignores list` shows the three `ignoreFiles` globs and six `ignoreValues` from
`.impeccable/config.json`. `detect` warns that the `App.razor` stylesheet links cannot be read,
then reports exactly one finding, `[side-tab] .twe-palette__row--active` at
`CommandPalette.razor.css` line 20, and exits 2.

Smoke (URL scan, anonymous home page): after `./bin/dev build`, start Web.Server alone with
`ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:54880 dotnet run -c Release --no-build --no-launch-profile --project source/container-apps/web/projects/web-server`,
then from the same task folder run
`IMPECCABLE_NO_TELEMETRY=1 npx --yes impeccable@4.11.0 detect http://localhost:54880/`
(set `IMPECCABLE_BROWSER` to a Chromium binary if the default browser fails to launch).

Expect: 0 primary findings and 1 advisory, `[low-contrast] 4.3:1 (need 4.5:1) — text #767a86 on #ffffff`, exit 0.

### Gates

- `./bin/dev build`: passed, 0 warnings, 0 errors (30.7s).
- Tests: none. This task changed no production code and no test file, only this task folder
  (`task.md` and `impeccable/.impeccable/config.json`).
- `ganda repo audit`: passed with 1 advisory warning, `vscode-window-icon` (`.vscode/settings.json`
  missing in this worktree). Boy-scout fix: `ganda repo audit --fix` wrote `window.title` into the
  gitignored local `.vscode/settings.json` (nothing to commit). The audit that `--fix` runs then
  passed all checks: 43 passed, 0 failed, 3 skipped (`dev-audit` skipped because only non-build
  paths changed).

## Notes

- Detector: https://github.com/pbakaus/impeccable (Apache-2.0)
- Eval: https://github.com/TimeWarpEngineering/timewarp-intel/blob/master/evaluations/2026-11-11-impeccable.md
- Intel PR: https://github.com/TimeWarpEngineering/timewarp-intel/pull/121
