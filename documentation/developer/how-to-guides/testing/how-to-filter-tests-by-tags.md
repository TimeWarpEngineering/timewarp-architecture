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

**Suite-shaped MTP projects** (`dotnet test`) do NOT honor `JARIBU_FILTER_TAG` today, and the
MTP host has no tag option — this asymmetry is an open upstream request:
<https://github.com/TimeWarpEngineering/timewarp-jaribu/issues/23>. Until it ships, use
`--list-tests` + `-- --filter-uid <uid>` (see how-to-filter-tests-by-name.md).
