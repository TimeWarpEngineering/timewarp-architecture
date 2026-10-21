# Round 1 — general
**Date:** 2026-10-21
**Scope reviewed:** same as framework (branch vs master, three product files)

## Summary

The change moves the Flexible Server, Key Vault, web-server pooled-string, and passkey-host assertions behind `#if web` plus `#if postgres` or `#if yarp`. Those are the same blocks that declare the resources in the AppHost: Neon and Flexible Server sit in `#if web` → `#if postgres` (program.cs:456–498), and `WebAuthnOptions__AllowedRpIds` with the ACA domain sits in `#if yarp` → `#if web` (program.cs:754–794). Leak checks (no direct Neon parameter, no web-migrations module, no scale-to-zero, no Neon names on aca) stay unconditional. The aca-neon external-ingress fact now detects the ingress module at runtime and requires 0 external apps without it. This matches how aca-publish-tests.cs:98 already handles `hasIngress`. The AppHost change is a comment only. Its claim that this is the only Neon parameter declaration, under web, postgres, and aca-neon, holds. Risk is low.

Verified: in-repo (all flags on) `AcaNeonPublish_Given_` 7/7 and `AcaPublish_Given_` 13/13 pass. The aca-neon log shows the `MissingParameterValueException` warning followed by "3 parameter values saved", which matches the Results. The reviewer did not re-run the full `template-smoke --matrix --app-tests`. The command has no single-variant option and needs more than the 10-minute foreground limit. The flag-off variant counts in Results are implementer-reported, and the nightly run confirms them.

Observation, not a finding: the aca-neon ingress fact uses runtime detection rather than `#if yarp`. With yarp on, a regression that drops the ingress module would pass that one fact with 0 external apps. That pattern already exists in aca-publish-tests.cs, and ingress-smoke-tests guard the ingress elsewhere, so this is consistent with the existing convention and is not raised.

## Issues

None.
