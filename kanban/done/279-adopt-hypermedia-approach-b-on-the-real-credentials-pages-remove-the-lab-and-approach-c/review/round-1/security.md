# Round 1 — security
**Date:** 2026-10-05
**Scope reviewed:** `git diff master...HEAD` (excluding kanban/), security lens. Files read in full or by diff:
SPA `credential-offer-rows.cs`, `credential-offer.cs`, `credentials-context-source.cs`, `command-palette-runner.cs`,
`command-palette-context.cs`, `contextual-action-arguments.cs`, `command-palette-roster.cs` (diff + `IsPaletteCommand`),
`command-palette-state.open.cs` (diff), `credentials-state*.cs` (diff), Settings/Passkeys page call sites; server
`credential-offers-application.cs`, `entra-sign-in-offer-application.cs`, `offered-action-contracts.cs`,
`get-credentials-*` (diff), `get-entra-sign-in-offered-handler-application.cs` (diff),
`revoke-credential-handler-application.cs` / `-contracts.cs` (unchanged, re-verified), and lab removal via `git grep`.

## Summary

No security defects found. Verified:

- **Forged / stale offers cannot run.** `CredentialOfferRows.RunAsync` with no current offer builds a bare row
  (no ArgumentsJson / FollowUpTarget) that can never equal a contributed row, so
  `CommandPaletteRunner.RunContextualAsync` refuses it at `context.IsOffered(row)` (record equality on name,
  target, arguments, follow-up, RequiresInput). A null `context` also refuses (`?.IsOffered(row) != true`).
  Rows are recomputed from the live `CredentialsState` and the current path at run time, so a palette row from an
  older snapshot or another page is refused; the Passkeys page contributes passkey rows only.
- **Fail closed order:** offered → `actionCatalog.Find` (unknown name refused) → `RefusalAsync` (Visibility
  Human/Both, authenticated, every Permission via `IAuthorizationService`) → bind → `Execute`. Every refusal is
  a shell Warning and nothing dispatches.
- **Input cannot override bound args.** `Bind` merges input with `TryAdd` into the parsed (Ordinal) dictionary
  and refuses on collision ("input cannot replace the offered 'credentialId'"); a case-variant key
  (`CredentialId`) survives `TryAdd` but is then refused by `ContextualActionArguments.Bind` as an undeclared
  parameter (Ordinal name match). JSON null for a required parameter counts as missing.
- **Visibility Agent → Both** for `RevokeCredential` / `RenameCredential`: the only Visibility consumers are
  `CommandPaletteRoster.IsPaletteCommand` and `CommandPaletteContext.RefusalAsync`. The static roster still excludes
  both (required `credentialId`), and no agent surface filters on Visibility today, so nothing new is exposed.
  `FetchCredentials` is newly cataloged as Agent with `CredentialManageSelf`; it is run only as a client-chosen
  follow-up, which is correctly not M4-checked.
- **Server remains the boundary.** `RevokeCredential.Handler` (unchanged) resolves the caller via
  `ICurrentPrincipalAccessor`, returns 404 for another principal's credential before mutating, and enforces
  `CredentialRules.CanRevoke` (409 LastCredential). `GetCredentials` / `RevokeCredential` keep
  `[EndpointAuthorize(Policy = CredentialManageSelf, IdentitySession + AgentToken)]`.
- **No cross-principal leak.** Offers are computed only from the caller's own summaries plus the site-wide
  Entra-offered flag, which is already public via the anonymous `GetEntraSignInOffered`. `EntraSignInOffer` keeps
  the original semantics (scheme enabled AND site setting on; null settings → not offered) and keeps the
  unconditional store read.
- **Lab removal is clean.** `git grep -i "hypermedia-lab|HypermediaLab|FollowCommand|AppRelativeHref|credential-commands"`
  over source/tests/skills is empty; the removed `api/hypermedia-lab/*` routes used the shared
  `CredentialManageSelf` policy (still in use), so no orphan route, ingress prefix (generated from contracts) or policy remains.
- Server-supplied labels are rendered through Razor/palette text (encoded); no markup injection path.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/identity/credential-offer-rows.cs:73
- Description: The client allow-list for credential offers is the whole catalog narrowed by Visibility/Permissions
  (the recorded M4 decision), so a GetCredentials response could name any Human/Both catalog action, including
  unrelated parameterless ones, and it would run when clicked in Ctrl-K on Settings/Passkeys. The server is the
  trusted origin for this response, so this is not exploitable without a compromised server or response; it is a
  defense-in-depth observation only, not a defect against the decision.
- Suggestion: Optional: `ForPage` could skip offers whose `Name` is not in `OfferedActionNames.All` (already
  shared with the SPA), which narrows the credentials surface to the vocabulary the server declares, without adding
  a second list. Leaving as-is is consistent with the recorded M4 decision.
- Status: open
