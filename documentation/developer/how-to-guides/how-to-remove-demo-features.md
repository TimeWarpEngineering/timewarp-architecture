# How to remove the demo slices (Counter, EventStream)

Every generated solution includes two small demo **slices** in `web-spa`. They are teaching
material — each demonstrates a pattern you will reuse — and they are **not** template flags:
you remove them by deleting code once you no longer need the reference.

A **slice** is an independently removable vertical unit. Identity is the namespace under
`{RootNamespace}.Features` (e.g. `…Features.Counters`, `…Features.EventStreams`), not the
folder path — folders usually mirror slices for humans but TWA0009 enforces namespace
boundaries. Pages that implement a slice live **in that slice's namespace** (not a grab-bag
`…Pages` namespace).

| Slice | Path (under `source/container-apps/web/projects/web-spa/`) | Namespace | Demonstrates |
|-------|--------------------------------------------------|-----------|--------------|
| Counter | `features/counter/` | `…Features.Counters` | TimeWarp.State action sets, JS-interop dispatch, `[StateAccess]` |
| EventStream | `features/event-stream/` | `…Features.EventStreams` | mediator pipeline middleware (`EventStreamBehavior`) |

## The fast way

Ask your coding agent:

> Remove the counter slice: delete `web-spa/features/counter/`, then fix every compile error
> that removal causes (nav links, global usings, `_Imports`, Style Guide opt-outs, tests)
> until `dev build` is 0/0.

The compiler is the checklist — deleting the folder surfaces every referencing site as an error,
and the repo's analyzers (TWA rules, especially TWA0009 slice isolation) catch the conventions
on the way back to green.

## The manual checklist

Deleting a demo touches, at minimum:

1. The slice folder itself (state, pages, components, notification handlers).
2. `components/NavMenu.razor` — its nav link.
3. `global-usings.cs` / `_Imports.razor` — its namespace usings.
4. `tests/container-apps/web/web-spa-integration-tests/` — its test folder, plus any shared
   pipeline tests that exercise its state (e.g. `CloneStateBehavior_Tests` uses `CounterState`).
5. Cross-slice opt-outs that targeted it — e.g. Style Guide's
   `[CrossSliceReference(typeof(CounterState), …)]` (the throw-exception demo backs the
   "exception → toast" button).

Build after each step; stop when `dev build` reports 0 warnings / 0 errors.

## Mapped demo entities and EF migrations

If you remove a **mapped** demo entity (one that participates in `PostgresDbContext` /
`IEntityTypeConfiguration` — e.g. the Profile teaching aggregate under
`web/features/profile/`), schema is migration-owned (task 147-007):

1. Delete the domain type, configuration, `DbSet`, and related product code.
2. Add a new EF migration that drops the unused tables/schemas (see
   [how-to-add-your-aggregate.md](how-to-add-your-aggregate.md) §8 for the exact
   `dotnet ef migrations add` command), **or** edit the pending migration if it has not shipped
   to any shared volume yet.
3. Do **not** hand-edit only the live database — AppHost `AddEFMigrations` applies committed
   migrations; an out-of-band drop will desync `__EFMigrationsHistory`.

SPA-only demos (Counter, EventStream) have no EF mapping; the checklist above is sufficient.
