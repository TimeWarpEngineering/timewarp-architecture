# How to filter tests by tags

Tag tests with Jaribu's `[TestTag]` attribute (class or method level):

```csharp
[TestTag("Smoke")]
public class CreateRole_Given_ { ... }
```

**Standalone runfiles** (the local dev loop) honor the `JARIBU_FILTER_TAG` environment
variable:

```console
JARIBU_FILTER_TAG=Smoke dotnet run source/.../create-role-tests.cs
```

**Suite-shaped MTP projects** (`dotnet test`) honor the same tag filter as of
`TimeWarp.Jaribu.TestingPlatform` **1.0.0-beta.15** (timewarp-jaribu#23), either via the CLI
option or the environment variable (CLI wins when both are set):

```console
cd tests/container-apps/web/web-spa-integration-tests
dotnet test -c Release -- --filter-tag Integration
# or
JARIBU_FILTER_TAG=Integration dotnet test -c Release
```

Matching is exact (case-insensitive), not substring, and only applies to classes/methods that
carry `[TestTag]`: a tagged class whose tags don't include the filter is skipped in full (its
`SetupOnce` never runs, so an expensive host fixture never boots); an **untagged** class or
method is unaffected by a tag filter and always runs. Verified example: in
`web-spa-integration-tests` (7 `[TestTag("Integration")]` classes + a few untagged serialization
tests), `--filter-tag NoSuchTag` completes in ~6s running only the untagged tests, while
`--filter-tag Integration` runs the full closed-box suite.

For substring selection by class/method name instead of tag, see how-to-filter-tests-by-name.md.
