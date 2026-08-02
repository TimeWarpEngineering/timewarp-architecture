# How to filter tests by name

Suite-shaped projects run on Jaribu via Microsoft.Testing.Platform (MTP). Today the MTP test
host supports selection **only by test-node UID**:

```console
cd tests/container-apps/web/web-domain-tests
dotnet test -c Release -- --list-tests           # discover UIDs
dotnet test -c Release -- --filter-uid <uid>     # run one
```

Notes:

- The csproj-path form (`dotnet test <path-to-csproj>`) is unsupported for MTP projects on
  .NET 10 — always run from the project directory.
- Human-usable selection (by class/method name pattern or tag) is an open upstream request:
  <https://github.com/TimeWarpEngineering/timewarp-jaribu/issues/23>. This guide should be
  updated when it ships.
- For **co-located runfiles** the practical filter is the file itself — one file per
  SUT/action means `dotnet run <file>.cs` is already a narrow selection; tags work there too
  (see how-to-filter-tests-by-tags.md).
