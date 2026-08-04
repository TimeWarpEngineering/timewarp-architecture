# Fix SimpleAlert: missing title/content space, dead @apply CSS, text not selectable

## Description

Bug report from the Login page (`/account/login`): when a danger `SimpleAlert` fires (e.g. a
failed passkey registration), it renders two UX defects, both visible in the rendered DOM:

```html
<div class="fluent-card simple-alert__card" role="alert">
  <fluent-label class="simple-alert__label" typo="Typography.Body">
    <span class="simple-alert__title">Error</span>
    <span class="simple-alert__content">Passkey registration verification failed: Verification failed: OriginMismatch.</span>
  </fluent-label>
</div>
```

1. **Missing space between title and content** — renders as
   `ErrorPasskey registration verification failed: …` instead of `Error: Passkey registration…`.
2. **Alert text cannot be selected; hover shows a pointer cursor** — users cannot copy the error
   message out of the alert.

## Repro

- `dev run`, open the Login page, trigger a passkey registration failure (e.g. origin mismatch)
  → the danger alert shows the concatenated text; hovering shows a pointer cursor and
  drag-to-select does not work.
- Also visible anywhere `SimpleAlert` renders with `Title` + content:
  - `web-spa/features/account/pages/login-page/LoginPage.razor` (lines 48, 53)
  - `web-spa/features/identity/pages/passkeys-page/PasskeysPage.razor` (lines 37, 42)
  - `web-spa/features/event-stream/pages/EventStreamPage.razor` (line 14)
  - `web-spa/features/developer/pages/AlertExamplePage.razor` (all variants)

## Goal — remove ALL Tailwind, plain CSS only

**One-line goal:** Stop inventing a broken custom alert: delete SimpleAlert and other dead
Tailwind leftovers; use FluentMessageBar for inline status; keep Tier-1 Card/StatusBadge;
plain-CSS the small assembly-info helpers.

**The repo standard is: no Tailwind anywhere, hand-written plain CSS on global design tokens.**
The standard is the **`tw-blazor-css-strategy`** skill (isolation-first hybrid):

- Component CSS lives in `*.razor.css` (Blazor isolation) with a native HTML root.
- Colors/type/radius/status palette come from `var(--twe-*)` tokens in
  `web-spa/wwwroot/css/tokens.css` — never hard-coded, never Tailwind scales.
- Prefer Fluent chrome for interactive/status UI (`FluentMessageBar`, `FluentButton`).
- Keep Tier-1 native wrappers only where already established (`Card`, `StatusBadge`).

## Root-cause leads

- **Spacing / selection / dead CSS:** `SimpleAlert` is a pre-FluentUI leftover — dead Tailwind
  `@apply` CSS, wrong Fluent primitives (`FluentCard` + form `FluentLabel`), not on StyleGuide.
- **Direction (resolved in planning):** delete SimpleAlert; replace call sites with
  `FluentMessageBar`; delete orphan Tailwind components (`Button`, `HyperLink`); rewrite
  LinkDisplay/PropertyDisplay; put samples on StyleGuide; remove AlertExamplePage.

## Checklist

- [x] Replace SimpleAlert on Login, Passkeys, EventStream with FluentMessageBar (correct Intent, AllowDismiss=false)
- [x] Add Message bars section to StyleGuide
- [x] Delete SimpleAlert + AlertExamplePage + Button + HyperLink
- [x] Rewrite LinkDisplay / PropertyDisplay off Tailwind class strings
- [x] Grep-clean `@apply` / SimpleAlert / deleted components; update docs that name them
- [x] Visual path covered in StyleGuide samples (human browser smoke optional)
- [x] `dev build` 0/0

## Notes

- Example error message originates from
  `source/container-apps/web/features/identity/identity-problems-application.cs` (line 57).
- Follow `tw-blazor-css-strategy` (isolation default; no `::deep` dumps for this work).

### Implementation Plan

# Task 146 — Implementation Plan

## Summary

`SimpleAlert` is a **pre-FluentUI leftover**: not on StyleGuide, dead Tailwind `@apply` CSS, wrong Fluent primitives (`FluentCard` + form `FluentLabel`). StyleGuide already shows the real stack: **FluentButton**, custom **Card**/`StatusBadge`, Fluent **toasts**.

**Direction:**
1. **Delete `SimpleAlert`** — replace call sites with **`FluentMessageBar`**.
2. **Delete orphan Tailwind components** (`Button`, `HyperLink`) that confuse the design system.
3. Fix remaining dead Tailwind class strings where components stay (`LinkDisplay` / `PropertyDisplay`).
4. Put message-bar samples on **StyleGuide**; remove the developer-only AlertExample page.

That fixes title/content spacing, selection/cursor, and the Tailwind CSS debt in one cut.

---

## Current state (facts)

| Piece | On StyleGuide? | Consumers | Problem |
|--------|----------------|-----------|---------|
| `SimpleAlert` | **No** | Login, Passkeys, EventStream, AlertExamplePage | Dead `@apply` CSS; `FluentLabel` wrong for body text; no title/content gap; pointer / hard to select |
| Custom `Button` | **No** (StyleGuide uses `FluentButton`) | **None** | Dead Tailwind utility class strings |
| `HyperLink` | **No** | **None** | Same |
| `Card` / `StatusBadge` | Yes | Many pages | Already plain CSS + tokens — keep |
| `LinkDisplay` / `PropertyDisplay` | No | `AssemblyInfoModal` only | Still used; default classes are dead Tailwind (`text-sm text-gray-500`, `text-blue-500`, …) |
| `AlertExamplePage` (`/developer/alert-example`) | Separate gallery | Only itself | Exists only to demo SimpleAlert |

`@apply` in web-spa CSS today: **only** `SimpleAlert.razor.css` (12 hits).  
`AlertExamplePage.razor.css`: plain CSS with hard-coded `lightblue`/`darkgreen` + `::deep` — dies with SimpleAlert.

---

## Design decisions

### 1. Delete SimpleAlert → use FluentMessageBar

Fluent v5 `FluentMessageBar` already has:
- `Title`, `ChildContent`
- `Intent`: Success | Warning | Error | Info | Custom  
- Built-in layout (title ≠ body — **no manual colon/space hack**)
- Normal text selection (not a form label)

**Intent map from old `AlertType`:**

| Old `SimpleAlert.AlertType` | `MessageBarIntent` |
|----------------------------|--------------------|
| Success | Success |
| Danger | **Error** |
| Warning | Warning |
| Info | Info |
| Custom / default | Info or Custom |

**Sticky status/errors (Login / Passkeys):** set `AllowDismiss="false"` so the bar stays while `StatusMessage` / `ErrorMessage` is set (default AllowDismiss is true).

**EventStream:** `Intent="Info"`, `Title="Event Stream"`, body = existing copy; `AllowDismiss="false"` (static explainer).

### 2. Title separator

With MessageBar, **Fluent owns title/body separation** — do **not** inject `": "` into Title. Visual check: title and body must not run together (`ErrorPasskey…`).

### 3. Orphan components

| Action | Files |
|--------|--------|
| **Delete** | `Button.razor`, `Button.razor.cs` |
| **Delete** | `HyperLink.razor`, `HyperLink.razor.cs` |
| **Delete** | `SimpleAlert.razor`, `SimpleAlert.razor.css` |
| **Delete** | `AlertExamplePage.razor`, `.razor.cs`, `.razor.css` |
| **Rewrite (keep)** | `LinkDisplay.razor`, `PropertyDisplay.razor` — plain CSS / tokens or small co-located isolation CSS |
| **Update docs** | `components/overview.md`, `documentation/developer/conceptual/component-naming-and-organization.md` if they list deleted names |

### 4. StyleGuide

Add a **“Message bars (FluentUI)”** section with Success / Warning / Error / Info samples (mirror former AlertExample). Optional: one Custom with icon. Reuse existing `sg-row` / `FluentStack` patterns.

### 5. CSS strategy (`tw-blazor-css-strategy`)

- Prefer Fluent chrome for interactive/status UI (`FluentMessageBar`, `FluentButton`).
- Keep Tier-1 native wrappers only where already established (`Card`, `StatusBadge`).
- No new `@apply`, no new Tailwind utility class strings.
- No new `::deep` dumps for this work.
- Tokens stay in `tokens.css`; no new hard-coded brand colors for assembly-info helpers.

### 6. LinkDisplay / PropertyDisplay rewrite

Keep components (used by AssemblyInfoModal). Replace dead Tailwind defaults with isolation CSS, e.g.:

```css
/* PropertyDisplay.razor.css */
.twe-prop { font-size: var(--twe-text-helper); color: var(--twe-muted); }
.twe-prop__name { font-weight: 600; color: var(--twe-ink-2); }
.twe-prop__value { font-weight: 400; }

/* LinkDisplay.razor.css */
.twe-ext-link { font-size: var(--twe-text-helper); }
.twe-ext-link a { color: var(--twe-blue); word-break: break-word; }
```

Markup: native roots with those classes (drop `text-sm text-gray-500` defaults). Prefer global `a` rules in `app.css` where enough.

---

## Files to change

### Delete
- `…/components/elements/SimpleAlert.razor`
- `…/components/elements/SimpleAlert.razor.css`
- `…/components/elements/Button.razor`
- `…/components/elements/Button.razor.cs`
- `…/components/elements/HyperLink.razor`
- `…/components/elements/HyperLink.razor.cs`
- `…/features/developer/pages/AlertExamplePage.razor`
- `…/features/developer/pages/AlertExamplePage.razor.cs`
- `…/features/developer/pages/AlertExamplePage.razor.css`

### Replace SimpleAlert → FluentMessageBar
- `…/features/account/pages/login-page/LoginPage.razor`
- `…/features/identity/pages/passkeys-page/PasskeysPage.razor`
- `…/features/event-stream/pages/EventStreamPage.razor`

### StyleGuide
- `…/features/style-guide/pages/StyleGuidePage.razor` (+ `.cs` only if needed)

### Assembly-info Tailwind cleanup
- `…/features/application/modals/assembly-info/LinkDisplay.razor` (+ new `.razor.css` if needed)
- `…/features/application/modals/assembly-info/PropertyDisplay.razor` (+ new `.razor.css` if needed)

### Docs (if they name deleted components)
- `source/container-apps/web/projects/web-spa/components/overview.md`
- `documentation/developer/conceptual/component-naming-and-organization.md`

### No change expected
- `tokens.css` (sufficient)
- `Card` / `StatusBadge` (already on strategy)
- Toast pipeline (already Fluent)

---

## Step-by-step implementation

1. **Call-site replacements** (keep behavior first)  
   Login / Passkeys pattern:
   ```razor
   @if (!string.IsNullOrEmpty(ErrorMessage))
   {
     <FluentMessageBar Intent="MessageBarIntent.Error"
                       Title="Error"
                       AllowDismiss="false">
       @ErrorMessage
     </FluentMessageBar>
   }
   ```
   Same for Success → `MessageBarIntent.Success`.  
   EventStream → Info + existing title/body.

2. **StyleGuide** — Message bars section with four intents (and optional Custom).

3. **Delete** SimpleAlert trio, AlertExamplePage trio, Button pair, HyperLink pair.

4. **Rewrite** LinkDisplay / PropertyDisplay (plain CSS + tokens).

5. **Docs** — remove SimpleAlert / HyperLink / custom Button from inventories; note FluentMessageBar for inline status.

6. **Sweep verify**
   ```bash
   rg '@apply|@tailwind|theme\(' source/container-apps/web/projects/web-spa --glob '*.css'
   rg 'SimpleAlert|AlertExample|components/elements/Button|HyperLink' source/ tests/ documentation/
   rg 'bg-indigo-|text-gray-|text-blue-500|bg-primary-|hover:bg-' source/container-apps/web/projects/web-spa
   ```
   Expect zero SimpleAlert / `@apply`; assembly-info free of Tailwind utility strings.

7. **Build:** `dev build` → 0/0.

8. **Visual:** StyleGuide message bars; Login error path (or Passkeys) — title separated, text selectable, no pointer-as-button feel; AssemblyInfoModal still readable.

9. **Kanban:** checklist + Results; commit per `tw-git` when implementing.

---

## Call-site snippets (concrete)

**LoginPage / PasskeysPage** (success + error):

```razor
@if (!string.IsNullOrEmpty(StatusMessage))
{
  <FluentMessageBar Intent="MessageBarIntent.Success" Title="Success" AllowDismiss="false">
    @StatusMessage
  </FluentMessageBar>
}
@if (!string.IsNullOrEmpty(ErrorMessage))
{
  <FluentMessageBar Intent="MessageBarIntent.Error" Title="Error" AllowDismiss="false">
    @ErrorMessage
  </FluentMessageBar>
}
```

**EventStreamPage:**

```razor
<FluentMessageBar Intent="MessageBarIntent.Info" Title="Event Stream" AllowDismiss="false">
  EventStream is an example of middleware. It adds each action to a list.
</FluentMessageBar>
```

**StyleGuide** (add after Status Badges or near Notifications):

```razor
<Card Title="Message bars (FluentUI)">
  <FluentStack Orientation="Orientation.Vertical" VerticalGap="12">
    <FluentMessageBar Intent="MessageBarIntent.Success" Title="Success" AllowDismiss="false">…</FluentMessageBar>
    <FluentMessageBar Intent="MessageBarIntent.Error" Title="Error" AllowDismiss="false">…</FluentMessageBar>
    <FluentMessageBar Intent="MessageBarIntent.Warning" Title="Warning" AllowDismiss="false">…</FluentMessageBar>
    <FluentMessageBar Intent="MessageBarIntent.Info" Title="Information" AllowDismiss="false">…</FluentMessageBar>
  </FluentStack>
</Card>
```

---

## Tailwind sweep findings & rewrites

| Hit | Action |
|-----|--------|
| `SimpleAlert.razor.css` entire `@apply` file | **Delete with component** |
| `AlertExamplePage.razor.css` hard-coded + `::deep` | **Delete with page** |
| `Button.razor.cs` Tailwind class strings | **Delete with component** |
| `HyperLink.razor` Tailwind `BaseCssClass` | **Delete with component** |
| `LinkDisplay.razor` `text-blue-500`, `break-words`, … | **Rewrite** plain CSS / tokens |
| `PropertyDisplay.razor` `text-sm text-gray-500`, `font-bold` | **Rewrite** plain CSS / tokens |
| Other `*.razor.css` / `wwwroot/**/*.css` | No `@apply` found — no further CSS rewrites required for this task |

**Out of this task:** `StatusBadge.razor.css` hard-coded hex tints — not Tailwind; leave unless drive-by.

---

## Test / verification plan

| Gate | Pass criteria |
|------|----------------|
| `dev build` | 0 warnings / 0 errors |
| Grep | No `SimpleAlert`, no `@apply` in web-spa CSS, no orphan Button/HyperLink paths |
| StyleGuide `/StyleGuide` | Message bars section renders 4 intents; buttons/toasts unchanged |
| Login or Passkeys | Force error → Error MessageBar: title ≠ body, text selectable/copyable |
| `/developer/alert-example` | 404 / gone from nav |
| Assembly info modal | Properties + links still render with muted helper styling |

No automated UI test required unless one already asserts SimpleAlert markup (none found).

---

## Out of scope / non-goals

- Restyling `StatusBadge` hard-coded hex → `color-mix`
- Replacing custom `Card` with `FluentCard` app-wide
- Toast pipeline changes
- Identity error message copy
- Adding a new SimpleAlert wrapper — **explicitly rejected**; call sites use Fluent directly
- Full SPA audit of every `class="…"` string outside the hits above

---

## Risks

| Risk | Mitigation |
|------|------------|
| FluentMessageBar theming looks “Fluent default” vs brand tokens | Accept Fluent chrome for status (same as buttons/toasts) |
| `AllowDismiss=true` clears UI but state still holds message | Use `AllowDismiss="false"` for Login/Passkeys |
| Nav still links to AlertExample | `[Page]` drives discovery — deleting the page removes the route |
| Delete Button/HyperLink breaks something | Zero razor consumers; `dev build` is the gate |
| MessageBar multi-line long errors | Prefer default; optional `Layout="MessageBarLayout.MultiLine"` on Error only |

---

## One-line goal

**Stop inventing a broken custom alert:** delete SimpleAlert and other dead Tailwind leftovers; use FluentMessageBar for inline status; keep Tier-1 Card/StatusBadge; plain-CSS the small assembly-info helpers.

## Session

- Created: opencode (2026-08-04)
- Planning: opencode orchestration (2026-08-04)
- Implementation: d98abb29 (2026-08-04)
- Review: Phase 4b round-1 general, clean (2026-08-04)

## Results

### Outcome
Shipped. Deleted broken SimpleAlert and orphan Tailwind Button/HyperLink; inline status now uses FluentMessageBar. Assembly-info helpers on plain CSS tokens. StyleGuide documents the pattern.

### Implementation summary
- **Replaced** SimpleAlert → FluentMessageBar on LoginPage, PasskeysPage, EventStreamPage (`AllowDismiss="false"`; Danger→Error intent).
- **StyleGuide** — new "Message bars (FluentUI)" card with Success/Error/Warning/Info.
- **Deleted** SimpleAlert (+css), custom Button (+cs), HyperLink (+cs), AlertExamplePage (+cs/css).
- **Rewrote** LinkDisplay / PropertyDisplay with isolation CSS on `--twe-*` tokens.
- **Docs** — components/overview.md and component-naming-and-organization.md point at FluentMessageBar/FluentButton and Card/StatusBadge.

### Intent mapping
| Old AlertType | MessageBarIntent |
|---------------|------------------|
| Success | Success |
| Danger | Error |
| Warning | Warning |
| Info | Info |

### Verification
- `dev build` → 0 Warning(s) / 0 Error(s) (implement commit)
- Grep: no `@apply`/`@tailwind`/`theme(` in web-spa CSS; no SimpleAlert/AlertExample/HyperLink in source/tests/docs; no orphan elements/Button
- Phase 4b review: effort 1 general, round 1, **0 open** → disposition **clean**

### Review
- **Rounds:** 1
- **Roster / effort:** general only (effort 1)
- **Final counts:** bug/suggestion/nit all 0 open, 0 fixed, 0 wontfix
- **Disposition:** clean
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`

### Commits
- `d98abb29` — fix(web-spa): replace SimpleAlert with FluentMessageBar
- Planning/move/folderize commits earlier on the task trail

### Follow-ups / out of scope
- Optional human browser smoke on Login error path + StyleGuide
- StatusBadge hard-coded hex → color-mix (not this task)
- Historical kanban/done docs may still mention SimpleAlert (left alone)
