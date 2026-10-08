# Disposition — task 272

**Date:** 2026-10-09
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

One general reviewer at effort 3 reviewed the branch against master (bf7bb258c, the .NET 11 RC1 upgrade). It raised no bugs. It confirmed that all global.json pins, the TFM, CPM, dotnet-ef and CI setup-dotnet are consistent, that no stray net10 references remain, and that the template pack dropped no content. It raised 2 suggestions and 2 nits. One nit was fixed: the stale NU1107 comment in web-server-integration-tests.csproj was removed. The other three are wontfix, with rationale below.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | suggestion | NoWarn is scoped to `tools/dev-cli` only. The offending files are Nuru package content outside the repo, which `.editorconfig` cannot target. | orchestrator |
| M2 | suggestion | The 180s seed budget is still bounded and fails loudly. It is required by the EF 11 `--verbose` startup and documented in the Design region. | orchestrator |
| M4 | nit | The LF normalization matches `.gitattributes` (`eol=lf`). | orchestrator |

## Escalations

- None.
