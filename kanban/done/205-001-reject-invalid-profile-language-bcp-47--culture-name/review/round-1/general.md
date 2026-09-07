# Round 1 — general
**Date:** 2026-09-06
**Scope reviewed:** branch `task/205-001-reject-invalid-profile-language-bcp-47-culture-nam` vs `origin/master` — `profile-details-contracts.cs`, `profile-domain.cs`, `update-profile-tests.cs`, `ProfilePage.razor`, `web-domain-tests/profile-tests.cs` (plus UpdateProfile/GetProfile/ProfileState call sites)

## Summary

The change closes the `/Profile` free-text hole by binding Language, Region, and Theme to Fluent UI v5 `FluentSelect<TOption,TValue>` over a curated `ProfileCatalog` in contracts, with `ProfileDetailsValidator.Must` membership and duplicated domain `HashSet`s enforced in `Create` / setters / `Invariants`. Catalog and domain code sets match exactly (15 / 27 / 3); Alias and Email remain text inputs. Risk is low: `web-spa` builds 0/0, co-located and domain junk/default tests pass, and the UX cannot type trailing junk the way `FluentTextInput` could. Residual gap is only live Aspire `/Profile` smoke (noted in the task Results), not a code defect.

## Issues
