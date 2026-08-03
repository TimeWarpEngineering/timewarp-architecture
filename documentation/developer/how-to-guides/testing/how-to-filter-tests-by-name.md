# How to filter tests by name

Suite-shaped projects run on Jaribu via Microsoft.Testing.Platform (MTP). As of
`TimeWarp.Jaribu.TestingPlatform` **1.0.0-beta.15** (timewarp-jaribu#23), the MTP test host
supports human-usable name selection directly — no UID lookup required:

```console
cd tests/container-apps/web/web-domain-tests
dotnet test -c Release -- --filter-class ProfileId    # substring match on class FullName
dotnet test -c Release -- --filter-method Should_      # substring match on method name
```

Both filters are substring, ordinal, case-insensitive. `--filter-class`/`--filter-method` **omit**
non-matching classes/methods entirely (no `Skipped` nodes reported) rather than skipping them — a
class or method that doesn't match the filter simply never runs and never boots its `SetupOnce`
host, if any.

The UID-based form still works and remains useful for scripting against a specific discovered
node:

```console
dotnet test -c Release -- --list-tests           # discover UIDs
dotnet test -c Release -- --filter-uid <uid>     # run one
```

Notes:

- The csproj-path form (`dotnet test <path-to-csproj>`) is unsupported for MTP projects on
  .NET 10 — always run from the project directory.
- For **co-located runfiles** the practical filter is the file itself — one file per
  SUT/action means `dotnet run <file>.cs` is already a narrow selection; tags work there too
  (see how-to-filter-tests-by-tags.md).
- For tag-based selection (`--filter-tag`), see how-to-filter-tests-by-tags.md.
