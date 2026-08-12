# Home sign-in CTA: link to Login only (no ceremony clone)

## Description

Home and `/Login` both showed "Sign in with a passkey"; home only redirected — double hop and
confusing naming. Fix: home is a soft **Sign in** entry; passkey ceremony stays only on LoginPage.

## Checklist

- [x] Home NotAuthorized card: no passkey ceremony clone; CTA label **Sign in** → `/Login`
- [x] Design region documents ceremony ownership
- [ ] Commit / PR if shipping alone

## Session

- Naming decision: user-facing "Sign in"; route stays `/Login`; ceremony only on LoginPage.
