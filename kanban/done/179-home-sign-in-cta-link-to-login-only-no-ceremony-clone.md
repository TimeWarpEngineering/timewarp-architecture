# Home sign-in CTA: link to Login only (no ceremony clone)

## Description

Home and `/Login` both showed "Sign in with a passkey"; home only redirected — double hop and
confusing naming. Fix: home is a soft **Sign in** entry; passkey ceremony stays only on LoginPage.

## Checklist

- [x] Home NotAuthorized card: no passkey ceremony clone; CTA label **Sign in** → `/Login`
- [x] Design region documents ceremony ownership
- [x] Commit / PR if shipping alone — product PR **301** (`b174e6e9`); kitchen moved to `kanban/done/` by task 202

## Notes

Product already merged to master 2026-08-12 (PR **301**). Kitchen id kept as **179** (the other duplicate 179 was re-id'd to 203).

## Results

Home NotAuthorized card is a soft **Sign in** link to `/Login`. Passkey ceremony stays only on LoginPage. Shipped as `b174e6e9` / PR **301**. Kitchen remains id **179** and lives in `kanban/done/`.

### How to validate

**Smoke**

```bash
test -f kanban/done/179-home-sign-in-cta-link-to-login-only-no-ceremony-clone.md && echo ok
# Expect: ok

ganda kanban path 179
# Expect: …/kanban/done/179-home-sign-in-cta-link-to-login-only-no-ceremony-clone.md

ls kanban/in-progress/ | rg '^179-' || echo 'no in-progress 179'
# Expect: no in-progress 179

git log -1 --oneline b174e6e9
# Expect: b174e6e9 fix(web-spa): home Sign in CTA links to Login only
```

**Expect**

- One architecture 179 kitchen, in `kanban/done/`, home Sign-in CTA (not githooks).
- Product change already on master via PR **301**.

## Session

- Naming decision: user-facing "Sign in"; route stays `/Login`; ceremony only on LoginPage.
- Board close: task 202 kept id 179 and `git mv`'d this kitchen to `kanban/done/` (2026-08-26).
