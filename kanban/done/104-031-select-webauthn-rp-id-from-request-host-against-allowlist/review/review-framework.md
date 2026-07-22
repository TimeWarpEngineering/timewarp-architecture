# Review framework — 104-031

- **Diff scope:** commit `337527bc` (per-request WebAuthn RP ID selection) on branch dev.
- **Roster / effort:** effort 2 — general reviewer + security reviewer (WebAuthn ceremony
  surface, Host-header handling, auth-adjacent; matches 104-003/104-004 precedent).
- **General focus:** options/binder-append correctness, selection helper shape, handler
  ordering (selection before challenge burn; AddPasskey auth-first deviation), hermetic test
  host blast radius across suites, test quality (host-selection integration mechanics incl.
  the SNI/dev-cert workaround), Design-region accuracy, TWA/convention hygiene.
- **Security focus (adversarial):** Host-header trust model (original-Host preservation vs
  forwarded headers; can a forged Host reach an unintended RP ID or origin acceptance?),
  fail-closed completeness across all five handlers (any path minting a relying party without
  selection?), challenge lifecycle under rejected hosts, enumeration/oracle uniformity of the
  400 response, allowlist validator bypasses (unicode/punycode/trailing-dot/case tricks),
  origin-rule interplay with the selected RP ID, credential scoping cross-RP-ID leakage in
  list/add/revoke paths.
- **Rounds:** round-1/{general,security}.md → merged.md → evaluate.
