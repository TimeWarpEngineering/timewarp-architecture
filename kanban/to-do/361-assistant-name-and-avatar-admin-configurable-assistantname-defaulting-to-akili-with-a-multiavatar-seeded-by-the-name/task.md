# Assistant name and avatar: admin-configurable `AssistantName`, defaulting to **Akili**, with a Multiavatar seeded by the name

## Why (Steve, 2026-10-14)

The assistant is labelled inconsistently:
- "**Ask**": the Ctrl-K action and the panel header;
- "**Ask AI**": the top-bar button (`web-spa/components/TimeWarpPage.razor:108`, `data-qa=AskAiButton`);
- "**Ask AI answer**" / "**Ask AI thumbs up/down**": the feedback that `AskAnswerBar.razor` files.

Steve wants one consistent name, configurable by admins, with personality: a **Swahili** name, in
keeping with Nuru, Amuru, Jaribu, Kiini and Taratibu. He also wants an **avatar** generated from the
name with Multiavatar, the way profiles get theirs.

## Decision log (2026-10-14, cockpit; change by appending a dated entry)

1. **Default name: Akili.** Swahili for *intelligence / mind / wit*. It is short, easy to say and
   fits an AI. Alternatives considered: *Rafiki* (friend; strong Lion King association), *Hekima* /
   *Busara* (wisdom; heavier), *Msaidizi* (helper; long). Admins can rename it per app.
2. **Where it lives: `SiteSettings`.** It is the existing admin-editable, database-backed aggregate
   (Entra/passkey policy) with a `Version` concurrency token and the `ISiteSettingsDetails` form on
   the admin Authentication settings page. Add `AssistantName` there, rather than appsettings, so
   admins change it at runtime without a deploy.
3. **Avatar: Multiavatar seeded by the assistant name.** Generate it locally with
   `TimeWarp.Multiavatar` / `MultiavatarGenerator.Generate`, exactly like `GetProfile` (task 149),
   so renaming changes the face deterministically. An admin-uploaded image override is **out of
   scope** for now; file it as a follow-up if wanted.

## Requirements

- **One source of truth for the label.** Every user-facing assistant label derives from
  `AssistantName`. Remove the hard-coded "Ask" and "Ask AI" strings. Pick a consistent pattern for
  the entry points and record it, for example:
  - top-bar button **"Ask Akili"**;
  - Ctrl-K action **"Ask Akili"**;
  - panel header **"Akili"** with the avatar;
  - greeting along the lines of **"Hi, I'm Akili — what can I help with?"** (keep 349's styling).

  Answer bar feedback titles: "Akili answer", "Akili thumbs up/down".
- **SiteSettings:**
  - `AssistantName` is required, trimmed, 1–32 characters, plain text (no markup or control
    characters), with a validator in `SiteSettingsDetailsValidator`. TWA0002/0003 nullability must
    agree.
  - Defaults to `Akili` via the seed or migration. Add an EF migration that sets existing rows to
    `Akili`.
  - The admin settings page gets an "Assistant name" field, through the existing update flow
    (Version concurrency, `[EndpointAuthorize]` unchanged).
- **Delivery to the SPA:** the SPA needs the name, and the avatar or its seed, on every page,
  including anonymous pages where the Ask button shows. Choose a cheap, cache-friendly path (for
  example a small public "assistant identity" query, or the existing site-settings read if it's
  already public) and record it. It must not expose any other admin-only setting.
- **Avatar:** generated **server-side** from the name (same generator as profiles), returned as an
  SVG data URI or a cacheable endpoint. Shown in the panel header (replacing 349's gradient Sparkle
  mark), on assistant turns in the thread, and in the empty state. It needs alt text (the name) and
  a sensible size in both themes.
- **Prompt and identity:** the system prompt or agent instructions, where the assistant introduces
  itself, use the name. Note in the task where the WebMCP / agent card identity (343) should pick it
  up; don't build 343 here.
- Every interaction stays a TimeWarp.State action (TWA0026). Styling per `tw-blazor-css-strategy`.
- **Tests:**
  - validator (empty, too long, markup rejected);
  - update flow, then the name reflected (admin 200, Member 403);
  - the avatar is deterministic per name and changes on rename;
  - SPA: labels and header use the configured name;
  - Playwright: rename to "Zawadi", and the top bar, Ctrl-K, header and greeting all say Zawadi.

## Amendment (2026-10-14, cockpit per Steve): builds on task 301

Stacked **after 301** (301 → 361 → 352–354). 301 derives Ctrl-K from the action system, removes
the hand-wired palette footer "Ask" button (`data-qa="CommandPaletteAsk"`), and adds a **runtime
display-name override** for derived rows. Therefore:

- **Ctrl-K:** don't relabel a footer button, because it no longer exists. Feed "Ask {AssistantName}"
  into 301's runtime override for the `AgentSurfaceState.OpenAskPanel` row (also its icon/avatar,
  if 301's row supports an icon).
- Surfaces owned here: the top-bar button (`TimeWarpPage.razor:108`), the derived Ctrl-K row (via the
  override), the panel header, the greeting, and the answer-bar feedback titles.
- The Playwright rename check ("Zawadi") asserts on the derived Ctrl-K row, not the old footer button.

## Checklist

- [ ] `AssistantName` in SiteSettings (+ validator, migration default `Akili`, admin field)
- [ ] Public, cache-friendly name/avatar delivery to the SPA (no other settings leaked)
- [ ] All labels from one source (button, Ctrl-K row via 301's override, header, greeting, feedback titles)
- [ ] Multiavatar seeded by the name; header, turns, empty state
- [ ] System prompt uses the name
- [ ] Tests (validator, authz, avatar determinism, labels, Playwright rename)
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Session

- Created: 2026-10-14 (cockpit, per Steve; default name Akili chosen per "use Swahili")
- 2026-10-14: amended to build on 301 (Ctrl-K via 301's runtime override; footer button gone). Stacked 301 → 361 → 352–354.
