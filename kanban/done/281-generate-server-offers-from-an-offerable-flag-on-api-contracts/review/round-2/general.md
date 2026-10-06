# Round 2 — general
**Date:** 2026-10-06
**Scope reviewed:** fix delta a9f2a03cd + prior M1–M4

## Prior findings
| ID | Status | Evidence |
|----|--------|----------|
| M1 | fixed | The predicate now matches any `TypeDeclarationSyntax` (`contracts-generator.offerable.cs:118`). `CanCarryOffer` (`:249`) rejects the global namespace, records, structs, interfaces and non-partial declarations along the whole containing chain, and reports TWE014 on the attribute (`:172-177`). The TWE014-only `OfferTarget` stays equatable: it holds strings and a `DiagnosticInfo` with only value fields. TWE014 is registered consistently in the descriptor SSOT, `AnalyzerReleases.Unshipped.md`, the AGENTS.md TWE table, both skills, the generator Design region, the csproj comment and the TWA0031 analyzer Design region. Every positive test uses a file-scoped namespace, and `Should_Emit_Offer_For_Contract_Nested_In_A_Container` covers a partial container, so neither causes a false positive. |
| M2 | fixed | TWE012 now takes a `{2}` reason: "is listed more than once", "is the auth-filled UserId …", or "names no property of X.Command" (`:206-210`). The title, description, AGENTS.md row, release-tracking row and skills all match. A duplicate is checked first, so a duplicated unknown entry reports "listed more than once" on its second occurrence and "names no property" on its first. That is accurate. |
| M3 | fixed | The name is now `OfferedActionNames.LinkMicrosoft365 = "Identity.LinkMicrosoft365"`. The `<Slice>.<Operation>` scheme for hand-written offers is documented in both skills and in the `offered-action-contracts.cs` Design region. The Design regions for the SPA action, roster and palette were updated, along with the catalog and palette tests. The only remaining `Credentials.LinkMicrosoft365` outside `kanban/` is a constant in a self-contained analyzer test fixture (`action-offer-agreement-analyzer-tests.cs:67`). It has no product meaning. |
| M4 | fixed | New generator tests cover: contracts nested in a container, inherited and hidden Command properties, `UserInput` naming a route parameter, the TWE013 span on `[Offerable]`, four TWE014 shapes, and the duplicate and UserId TWE012 reasons. `dotnet test -c Release -- --filter-class ContractsGeneratorOfferable`: 15/15 passed. |

## Summary
All four round-1 findings are fixed. The fix delta introduces no bugs: TWE014 does not fire on legitimate contracts, and the incremental pipeline stays equatable. There are two new nits. The first is a fail-open shape that TWE014 still misses: generic contracts or containers. It is unlikely for routed contracts. The second is a roster Design sentence ("rows group by state") that the rename made inaccurate.

## Issues
### Issue 1 — Severity: nit
- File: source/foundation/foundation-contracts-generators/contracts-generator.offerable.cs:249
- Description: `CanCarryOffer` does not reject generic types. `ToTarget` writes the contract and container names without type parameters (`contracts-generator.cs:246,248`). So `[Offerable] static partial class Op<T>`, or an `[Offerable]` contract nested in `partial class Outer<T>`, would compile a new, separate non-generic `partial class Op`/`Outer` that holds the Offer. No error is reported. TWA0031 then skips the real contract because it has no `OfferName`. This is the same fail-open class M1 closed. It is unlikely in practice, because routed contracts are never generic.
- Suggestion: add `type.IsGenericType` (or `Arity > 0`) to the TWE014 rejection loop, and add "generic" to the TWE014 message and docs. Or record in the Design region that generics are out of scope.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/command-palette/command-palette-roster.cs:16
- Description: The Design region says the label owner is "the catalog name's prefix, so rows group by state". After the M3 rename, the `CredentialsState` action Link Microsoft 365 shows as "Identity: Link Microsoft 365", as do the generated `Identity.RenameCredential` and `Identity.RevokeCredential` actions. Meanwhile Add passkey and the other credentials actions stay under "Credentials:". So the owner is the catalog-name prefix, and that prefix is no longer always the state. The behaviour is intended and tested. Only the "by state" wording is inaccurate.
- Suggestion: reword to "rows group by catalog-name prefix (the owning state, or the offering slice for server-offered actions)".
- Status: open
