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

- [ ] M30: `verify-samples-command.cs` implemented, made an honest stub, or deleted
- [ ] M32: `<NoWarn>` lists in `container-apps/Directory.Build.props`,
      `foundation/Directory.Build.props`, `grpc-server.csproj`, `web-server.csproj` audited
      and commented per id/group
- [ ] M33: three `<Pending>` justifications in `web-spa/global-suppressions.cs` resolved
      (real justification or fix + delete)
- [ ] M34: `CorsPolicy` overload with exposed headers added; `grpc-server` consumes it in
      place of the hand-rolled CORS setup
- [ ] M36: kanban task opened and linked from the Design region, or the transition/
      `NotifyLossOfInterest` behavior fixed
- [ ] `dev build` 0/0
- [ ] `dev test`

## Notes

- Parent: 210 (round-1 ledger:
  `kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`).
  On completion, update the M-ids' Status in that ledger to fixed/wontfix on the same PR.

## Session

- Created: 227444 (2026-09-12)
