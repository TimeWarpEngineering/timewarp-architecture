# Round 2 — general
**Date:** 2026-10-28
**Scope reviewed:** fix delta `f029de790..309942e66` (round-1 base.sha to HEAD), product paths: `agent-link-onboarding-prompt.cs`, `agent-key-proof.cs`, the new `agent-link-onboarding-prompt-tests.cs`, and the Playwright test. The round-1 findings M1 and M2 were re-checked against this delta.

## Summary

The fix pass splits the prompt into two bash blocks. The first block registers the key, mints a bearer it keeps only in memory, POSTs the link, writes `keyId` and `linkId` files into the directory it prints, and runs `unset access`. The second block `cd`s to `$DIR`, mints a fresh token with the saved `agent.pem` and key id, and GETs `human-ux`. The prompt now takes routes from the contracts' `RouteTemplate`, verbs from `GetHttpVerb()`, JSON names from `ContractSerializationDefaults`' naming policy applied to `nameof`, and ceremony prefixes from `AgentKeyProof.GetPrefix`. A new pure Jaribu test serializes the contracts and checks the prompt against them. M1 and M2 are fixed.

The fix also makes `AgentKeyProof.GetPrefix` public in `TimeWarp.Identity`, a platform package. Version 2.0.0-beta.27 is already on NuGet, and the branch still builds 2.0.0-beta.27. Generated apps always use the package (the template excludes `source/libraries/timewarp-identity/**`), and the published beta.27 still has `GetPrefix` private, so every generated app's web-spa would fail to compile. Template smoke doesn't catch this because it packs local source as `2.0.0-smoke`. Verified in this round:

- `dotnet build` of `web-spa-integration-tests` reported 0 warnings and 0 errors.
- `Build_Should.FollowTheContracts_And_MintAFreshTokenAfterApproval` passed (1/1).
- `dev check-version` failed: "Version 2.0.0-beta.27 was already released."

## Issues

### Issue 1 — Severity: bug
- File: source/libraries/timewarp-identity/ceremonies/agent-key/agent-key-proof.cs:91
- Description: `GetPrefix` went from private to public in `TimeWarp.Identity`, and `AgentLinkOnboardingPrompt` (web-spa, which ships in the template) now calls it. The repo `<Version>` and the `TimeWarp.Identity` pin are still `2.0.0-beta.27`, and that version is already published. Generated apps resolve `TimeWarp.Identity` from NuGet (package mode: `UseIdentityPackages` is true when the library source is absent, and `.template.config` excludes it). They get beta.27, where `GetPrefix` is private, so `dotnet new timewarp-architecture` output fails with CS0122. `dev template-smoke` packs local source as `2.0.0-smoke` and hides the failure. `dev check-version` fails on this branch. AGENTS.md says to bump `<Version>` and the platform package pins in the same commit when shipping packages.
- Suggestion: Bump `source/Directory.Build.props` `<Version>` to the next prerelease (2.0.0-beta.28), and move every platform pin in `Directory.Packages.props` (Analyzers/Generators/Attributes, `TimeWarp.Identity`, `TimeWarp.402`, and the Foundation pins) to it in the same commit. Re-run `dev check-version` until it passes. Do not work around the release by copying the prefix literals back into the spa.
- Status: open

## Re-verification of round-1 findings

- **M1 — fixed.** The first block saves `keyId` and `linkId` (chmod 600) in the directory it prints, and runs `unset access`. The second block reads both files, signs a new Token.v1 challenge with the `agent.pem` in that directory, and GETs `api/agent-links/${link_id}/human-ux` with the new bearer. Neither block writes the bearer to a file, and the "same bearer" wording is gone. `json_string` extracts `linkId` from `RequestAgentHumanLink.Response`, which serializes the Guid as a JSON string.
- **M2 — fixed.** Routes, verbs, JSON member names, prefixes, scope, and `SpecId` all come from the contracts and `AgentKeyProof`. Renaming a contract property now changes the prompt at compile time. The new test serializes each command and response with `ContractSerializationDefaults.Options` and asserts the prompt contains each wire name. For the `keyId`, `linkId`, and `accessToken` file-name and extraction sites, it asserts exact usage.

## Observations (not findings)

- `JsonMember` applies only the naming policy and ignores `[JsonPropertyName]`. The Design region records this as a deliberate choice (WASM trimming). For command members, the test checks a bare `ShouldContain(name)` across the whole prompt, so an override whose name shows up elsewhere in the prose could pass by accident. No agent-identity or agent-link contract uses `[JsonPropertyName]` today, so this is not raised as a finding.
- Splicing the multi-line `Helpers` constant into the raw string leaves its lines at column 0 of the bash block. That is only cosmetic and does not affect bash.
