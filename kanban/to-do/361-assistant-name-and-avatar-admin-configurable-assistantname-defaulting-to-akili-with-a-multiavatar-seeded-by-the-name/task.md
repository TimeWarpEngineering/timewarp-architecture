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

- [x] `AssistantName` in SiteSettings (+ validator, migration default `Akili`, admin field)
- [x] Public, cache-friendly name/avatar delivery to the SPA (no other settings leaked)
- [x] All labels from one source (button, Ctrl-K row via 301's override, header, greeting, feedback titles)
- [x] Multiavatar seeded by the name; header, turns, empty state
- [x] System prompt uses the name
- [x] Tests (validator, authz, avatar determinism, labels, Playwright rename)
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Session

- Created: 2026-10-14 (cockpit, per Steve; default name Akili chosen per "use Swahili")
- 2026-10-14: amended to build on 301 (Ctrl-K via 301's runtime override; footer button gone). Stacked 301 → 361 → 352–354.
- 2026-10-14: implemented (ganda task work, implement oracle). Gates green: `dev build` 0/0,
  `dev test`, `dev template-smoke`, `ganda repo audit`.

## Results

### What shipped

- **Domain:** `SiteSettings.AssistantName` (stored trimmed) with `RenameAssistant`, separate from
  `ReplacePolicy` so the seeder's Development reseed never resets an admin-set name.
  `AssistantNameRules` (TimeWarp.Identity) owns the rules: default **Akili**, 1-32 characters
  after trimming, no control characters, no `<` or `>`. The aggregate and
  `SiteSettingsDetailsValidator` both use it, so they can't disagree. `AssistantName` is
  non-nullable `= null!` on the command (TWA0002/0003 clean).
- **Persistence:** `identity.site_settings.AssistantName varchar(32) not null`. Migration
  `20261014110100_AddSiteSettingsAssistantName` fills existing rows with `Akili`.
  New rows get it from `SiteSettings.Create`.
- **Update flow:** `ISiteSettingsDetails` / `UpdateSiteSettings` / `GetSiteSettings` carry
  `AssistantName`. Version concurrency and `[EndpointAuthorize] settings.write` are unchanged. The
  Admin Authentication page has a new **Assistant** section with an "Assistant name" field. After a
  successful save it re-reads the public identity, so the labels change without a reload.
- **Delivery (recorded decision):** a new anonymous query, `GetAssistantIdentity`
  (`GET api/settings/assistant`). Its payload is **only** `{ name, avatar }`. `GetSiteSettings`
  needs `settings.read` and carries Entra policy, so it was not reused. The SPA reads the identity
  once per app load (`AuthenticationStateListener` → `AgentSurfaceState.LoadAssistantIdentity`) and
  again after an admin save. It's small and the same for every caller, so nothing per-user is
  cached. If the store is unmigrated, the name falls back to Akili instead of a 503.
- **Avatar:** generated server-side with `MultiavatarGenerator.Generate(name)` and returned as an
  SVG data URI, the same way `GetProfile` makes profile avatars. It is deterministic per name and
  changes on rename. It appears in three places:
  - the panel header, `<img alt="{name}">` at 24px, replacing 349's gradient Sparkle mark (the mark
    stays as the fallback until the identity loads);
  - the empty state, 56px in front of 349's orbs;
  - beside every assistant turn, through the `--twe-assistant-avatar` CSS property. Inline styles
    are banned, so `ask-ai.ts` `SetAssistantAvatar` sets that property on the document root, and it
    accepts only base64 SVG data URIs.

  `GetAssistantIdentity.MockAvatar` is the precomputed Akili URI (web-contracts does not reference
  Multiavatar). A test checks it against the generator.
- **One label source:** every label below reads `AgentSurfaceState.AssistantName` / `AskLabel`:
  - top-bar button: **"Ask {name}"**;
  - Ctrl-K: `AskAssistantDisplayOverride` feeds 301's `IActionDisplayOverride`, so the derived
    `AgentSurface.OpenAskPanel` row reads **"Ask {name}"** with description "Open the {name} panel…".
    301's `ActionDisplay` is text-only, so the row has no avatar icon;
  - panel header: **"{name}"** with the avatar. The conversation label is "{name} conversation";
  - greeting: **"Hi, I'm {name} — what can I help with?"** in 349's styling;
  - signed-out, error and starting states: "Sign in to use {name}", "{name} could not load…",
    "Starting {name}…";
  - answer-bar feedback titles: **"{name} answer"**, **"{name} thumbs up"**, **"{name} thumbs down"**.

  The "Ask" and "Ask AI" literals are gone. The credential fallback label is "Assistant conversation".
- **Prompt:** `AgentAskInstructions.For(assistantName, pageContext)` starts with "You are {name},
  the assistant built into this app. When you introduce yourself, use that name."
- **For 343 (not built here):** the WebMCP / agent card identity should read the same source.
  Server-side, read `ISiteSettingsStore` → `SiteSettings.AssistantName` and
  `Settings.Application.GetAssistantIdentity.AvatarFor(name)`. Client-side, read
  `AgentSurfaceState.AssistantName` / `AssistantAvatar`.
- **Docs:** the `tw-blazor` skill now says assistant labels come from `AssistantName` and must
  never be hard-coded.
- **Not in scope (follow-up if wanted):** an admin-uploaded avatar override.

### Tests

- `update-site-settings-tests.cs` (co-located Jaribu):
  - validator rejects empty, whitespace, 33 characters, `<b>…</b>`, `<`, a control character, a
    newline, and null;
  - validator accepts Akili, Zawadi, a padded name, `Q&A Helper`, and 32 characters;
  - the handler stores the rename trimmed.
- `get-assistant-identity-tests.cs`:
  - JSON round-trip, with no policy fields in the payload;
  - the mock is Akili, and `MockAvatar` equals the generator output;
  - the avatar is deterministic and changes on rename;
  - an empty store falls back to Akili.
- `site-settings-endpoint-tests.cs` (HTTP):
  - anonymous `GET api/settings/assistant` returns 200 with name and avatar only;
  - an Administrator renaming to "  Zawadi  " gets 200 and the public identity shows Zawadi with a
    new avatar (the test restores Akili afterwards);
  - a markup name gets 400;
  - a Member PUT gets 403.
- `timewarp-identity-tests/site-settings-assistant-name-tests.cs`: default, trim, invalid names
  throw, and a rename survives the store.
- `assistant-identity-tests.cs` (SPA in-proc):
  - state defaults to Akili / "Ask Akili";
  - a server answer applies the name and avatar;
  - an invalid answer or a failure keeps the current name;
  - the Ctrl-K override gives "Ask Zawadi";
  - the panel header, avatar alt text and sign-in text say Zawadi.
- Playwright `AdminRename_Should_Relabel_Every_Ask_Entry_Point`: renaming to Zawadi on
  /Admin/Authentication makes the top bar read "Ask Zawadi", the derived Ctrl-K row read
  "Ask Zawadi", the header read Zawadi with its avatar, and the greeting read "Hi, I'm Zawadi". No
  "Akili" is left in the panel. Screenshots: `assistant-rename-{admin,ctrl-k,panel}.png`.

### How to validate

**Smoke:**

```bash
dev build
dev test
dev template-smoke
# focused:
dotnet test --project tests/container-apps/web/web-jaribu-tests
dotnet test --project tests/container-apps/web/web-spa-playwright-tests   # includes AdminRename_Should_Relabel_Every_Ask_Entry_Point
```

To check by hand with `dev run`, sign in with the first passkey (Administrator). Open
/Admin/Authentication, set "Assistant name" to `Zawadi`, and choose Save. Then press Ctrl-K, type
`zawadi`, and press Enter. With an existing database, `dev db update` applies the migration.

**Expect:**

- `dev build`: 0 warnings, 0 errors.
- `dev test` and `dev template-smoke`: succeed.
- Before the rename, the top bar reads "Ask Akili".
- After Save, without a reload:
  - the top bar reads "Ask Zawadi";
  - the Ctrl-K row reads "Ask Zawadi";
  - the panel header reads "Zawadi" with a Multiavatar face that differs from Akili's;
  - the empty state reads "Hi, I'm Zawadi — what can I help with?";
  - assistant answers show the same face beside them.
- `GET /api/settings/assistant` (anonymous) returns `{"name":"Zawadi","avatar":"data:image/svg+xml;base64,…"}`
  and nothing else.
- Saving `<b>x</b>` or a 33-character name fails validation.

