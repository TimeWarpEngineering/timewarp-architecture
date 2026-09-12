# Suppression hygiene, dev-cli verify-samples honesty, shared gRPC CORS policy

## Description

Code-quality findings from the 210 round-1 code review of the architecture template
(`kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`,
findings M30, M32, M33, M34, M36).

## Requirements

### M30

- File: `tools/dev-cli/endpoints/verify-samples-command.cs:20-26`
- Prints "Samples verified successfully!" around a bare TODO; nothing is verified, exit
  code 0.
- Fix: implement real verification, or make it an honest stub (error line + non-zero exit)
  — or delete the endpoint if `samples/` stays empty.

### M32

- Files: `source/container-apps/Directory.Build.props:8`; `source/foundation/Directory.Build.props:4-7`;
  `source/container-apps/grpc/projects/grpc-server/grpc-server.csproj:5`;
  `source/container-apps/web/projects/web-server/web-server.csproj:23`
- A 30-id uncommented `<NoWarn>` on container-apps, a 20-id foundation `<NoWarn>` justified
  only as "existed in original code" (ships in published `TimeWarp.Foundation.*`), and two
  uncommented per-project NoWarns. `tests/Directory.Build.props` is the exemplar (every id
  justified).
- Fix: audit each id against current code; keep only what is needed, one-line reason per id
  or group, in the `tests/Directory.Build.props` style.

### M33

- File: `source/container-apps/web/projects/web-spa/global-suppressions.cs:11-13`
- Three `SuppressMessage` entries with `Justification = "<Pending>"` (CA1052 on `Program`,
  CA2000 on `Program.Main`, CA1720 on `EventStreamBehavior.Guid`).
- Fix: write real justifications or fix the underlying warnings and delete the suppressions.

### M34

- File: `source/container-apps/grpc/projects/grpc-server/program.cs:46-56`
- Hand-rolled CORS duplicating `CorsPolicy.AnyPolicy` solely to add gRPC exposed headers.
- Fix: extend the foundation policy with an overload accepting exposed headers, and consume
  it from `grpc-server` like web/api already consume `CorsPolicy.AnyPolicy`.

### M36

- File: `source/container-apps/web/projects/web-spa/features/profile-menu/profile-menu-state/profile-menu-state.toggle.cs:29`
- "Transitions and NotifyLossOfInterest not working" — a known missing UX behavior tracked
  only by an inline TODO.
- Fix: open a kanban task and point the file's Design region at it, or fix the behavior
  directly in this task.

## Checklist

- [x] M30: `verify-samples-command.cs` implemented, made an honest stub, or deleted
- [x] M32: `<NoWarn>` lists in `container-apps/Directory.Build.props`,
      `foundation/Directory.Build.props`, `grpc-server.csproj`, `web-server.csproj` audited
      and commented per id/group
- [x] M33: three `<Pending>` justifications in `web-spa/global-suppressions.cs` resolved
      (real justification or fix + delete)
- [x] M34: `CorsPolicy` overload with exposed headers added; `grpc-server` consumes it in
      place of the hand-rolled CORS setup
- [x] M36: kanban task opened and linked from the Design region, or the transition/
      `NotifyLossOfInterest` behavior fixed
- [x] `dev build` 0/0
- [x] `dev test`
- [x] Implementation review (effort 1, general) — disposition clean

## Notes

- Parent: 210 (round-1 ledger:
  `kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`).
  On completion, update the M-ids' Status in that ledger to fixed/wontfix on the same PR.
- M36 follow-up: task 211
  (`kanban/to-do/211-profile-menu-transitions-and-notifylossofinterest/task.md`).

## Session

- Created: 227444 (2026-09-12)
- Implementer: grok session 01a095f8 (2026-09-12)
- Review oracle: grok session 01a09610-ef49-77e1-9acc-13c3f759733a (2026-09-12)
- Review general round 1: grok subagent 01a09614-7cfd-7f42-ab51-fb7f092b163b (2026-09-12)

## Results

M30–M36 from the 210 round-1 ledger, implemented on this branch.

- **M30:** `verify-samples` compiles every `*.csproj` and loose `*.cs` under `samples/`. Empty tree (template ships only `.gitkeep`) prints `No compilable samples under samples/; nothing to verify.` and exits 0. Missing `samples/` or a failed sample build is non-zero.
- **M32:** Extra `<NoWarn>` lists audited by stripping them and rebuilding. Remaining ids each have a one-line reason (`tests/Directory.Build.props` style). Unused ids dropped. `web-server.csproj` lost redundant `1591`. `grpc-server.csproj` kept CA1051/CA1848/CA1849 only.
- **M33:** `Program` is `static`; `Main` owns the host with `await using WebAssemblyHost`; unused `EventStreamBehavior.Guid` deleted. `global-suppressions.cs` removed.
- **M34:** `CorsPolicy.Apply(IServiceCollection, params string[] exposedHeaders)`; `AnyPolicy` adds `WithExposedHeaders` when the list is non-empty. `grpc-server` calls `CorsPolicy.Any.Apply(…, "Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding")` and `UseCors`/`RequireCors(CorsPolicy.Any.Name)`.
- **M36:** Task 211 opened and published to origin-home to-do. Toggle Design region points at it; inline TODO removed. Live header menu is FluentMenu in `Profile.razor`.

Parent ledger M30, M32, M33, M34, M36 set to **fixed** with disposition notes; counts table updated.

**Test outcomes:** `dotnet run tools/dev-cli/dev.cs -- build` → 0/0. `dotnet run tools/dev-cli/dev.cs -- test` → pass. `dotnet run tools/dev-cli/dev.cs -- verify-samples` → empty-set message, exit 0.

### How to validate

**Smoke**

```bash
dotnet run tools/dev-cli/dev.cs -- verify-samples
# expect: "No compilable samples under samples/; nothing to verify." and exit 0
```

```bash
rg -n "CorsPolicy.Any.Apply" source/container-apps/grpc/projects/grpc-server/program.cs
# expect: Apply(serviceCollection, "Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding")
# expect: no hand-rolled AddCors AllowAnyOrigin block
```

```bash
rg -n "Justification = \"<Pending>\"" source/container-apps/web/projects/web-spa || true
# expect: no matches; global-suppressions.cs gone
```

```bash
rg -n "task 211" source/container-apps/web/projects/web-spa/features/profile-menu/profile-menu-state/profile-menu-state.toggle.cs
# expect: Design region links kanban/to-do/211-profile-menu-transitions-and-notifylossofinterest/task.md
```

**Expect**

- Each remaining `<NoWarn>` in `source/container-apps/Directory.Build.props` and `source/foundation/Directory.Build.props` has a trailing `<!-- reason -->`.
- `web-server.csproj` has no extra `<NoWarn>`.
- `grpc-server.csproj` extra ids are only CA1051, CA1848, CA1849, each commented.

**Automated gate**

```bash
dotnet run tools/dev-cli/dev.cs -- build
# expect: Build succeeded. 0 Warning(s) 0 Error(s)

dotnet run tools/dev-cli/dev.cs -- test
# expect: Tests completed successfully!
```

**Not in scope:** wiring profile-menu Opening/Closing + NotifyLossOfInterest (task 211). Live header menu is FluentMenu.

### Review disposition

- **Rounds:** 1
- **Effort / roster:** 1 (general only)
- **Counts (final):** bug 0/0/0, suggestion 0/0/0, nit 0/0/0 (open / fixed / wontfix)
- **Disposition:** **clean** — no issues raised; 0 open
- **Paths:**
  - `review/review-framework.md`
  - `review/round-1/general.md`
  - `review/round-1/merged.md`
  - `review/disposition.md`
