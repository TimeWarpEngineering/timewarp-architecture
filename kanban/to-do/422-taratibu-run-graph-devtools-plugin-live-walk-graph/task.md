# Taratibu run-graph DevTools plugin (live walk graph)

## Description

A plugin for the TimeWarp DevTools shell (task 335) that renders a Taratibu walk as a live, left-to-right graph while it runs, driven by the run events from TimeWarpEngineering/timewarp-taratibu task 059.

Origin: Steven, 2026-10-27, on Alexandre Mutel's (@xoofx) post https://x.com/xoofx/status/2114969021514965468 ("Workflows and CodeMode coming to CodeAlta", 9 s video): "this is what I am thinking for TimeWarp.DevTools and Taratibu".

Target UX, from the demo:
- The workflow as a left-to-right directed graph of rounded step cards ("split the question" → "Research each sub-question" → "Write the answer"), each card holding nested sub-task / tool-call cards.
- Pending nodes dimmed; the running node and its running sub-task get a pulsing blue border and a spinner; finished nodes get a green check.
- A header badge with the workflow name, memory use and a live "Running N s" timer that turns into a green "Completed".
- Parallel sub-tasks stacked vertically inside their step (the demo's middle step runs two at once).
- Per-sub-task token counts (e.g. "20/20 tokens") and a run statistics footer (total tokens, per-provider aggregate).

The demo shows no loops and no failures. Designing those is part of this task (see Requirements).

## Depends on

- 335

## Requirements

- **Plugin contract (335):** name, icon, tab content, optional service registration. The plugin must **not** depend on TimeWarp State, nor require the State DevTools plugin.
- **Data source:** the run events from timewarp-taratibu task 059 (its published event contract). Prefer consuming them as OpenTelemetry spans/events over OTLP (the AppHost already routes OTLP for State DevTools), so no Taratibu-specific transport is needed. Fall back to whatever contract 059 publishes if it chooses an observer slot.
- **Layout:** node order and edges from the routine's IR (graph shape is known before the walk), live state from events. Nested walks render as nested cards; parallel siblings stack vertically.
- **States to draw:** pending, running, finished, failed, skipped, parked (waiting on a gate), retrying / blocked re-invoke. Failed: red border + reason on hover/expand, downstream nodes stay dimmed. Parked: distinct "waiting" badge naming the gate.
- **Loops / revisits:** a node visited more than once (retry, blocked re-invoke, review rounds) shows an iteration count badge and lets the user step through iterations, rather than drawing duplicate cards.
- **Header + footer:** run name, live elapsed timer, status badge (Running / Parked / Completed / Failed), memory if reported; footer with tokens and cost per provider/model where reported (absent when not reported, not zero).
- **Multiple runs:** a run picker (recent runs, live first); a finished run stays inspectable.
- **Conventions:** follow tw-blazor (one `@code` at top, then markup), tw-blazor-css-strategy (FluentUI + scoped CSS rules) and tw-feature-placement. Animations respect `prefers-reduced-motion`.

## Acceptance criteria

- [ ] The plugin registers in the 335 shell through the minimal contract and builds without any `TimeWarp.State*` reference (checked by a test or analyzer).
- [ ] A sample Taratibu walk (compute, oracle, gate, nested child walk, two parallel nodes) renders left-to-right with nested and stacked parallel cards.
- [ ] Live state updates: pending dimmed → running (pulse + spinner) → finished (green check); timer runs and flips to Completed.
- [ ] A failing node shows the failed state and reason; the run badge shows Failed.
- [ ] A parked gate shows the waiting state; resume continues on the same graph.
- [ ] A retried / re-invoked node shows an iteration count and lets the user view each iteration.
- [ ] Token and cost figures per node and in the footer match the events.
- [ ] bUnit tests for state rendering; a closed-box check that the plugin tab loads in the shell.
- [ ] `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit` green.

## Checklist

- [ ] Confirm the 335 shell and plugin contract exist (see open point below); if not, file or pick up that work first
- [ ] Read the task 059 event contract once published
- [ ] Design failed / parked / loop states (sketch in Notes before building)
- [ ] Implement plugin: IR layout, live event binding, header, footer, run picker
- [ ] Tests and gates

## Session

- Created: 2204330 (2026-10-27), filed by Amina on Steven's "sure" (2026-10-27 22:25 Asia/Bangkok). Not launched.

## Notes

- **Open point / dependency: the shell does not exist yet.** Task 335 is in `kanban/done/`, but its two subtasks (335-001, 335-002) only wired State DevTools into the AppHost. As of 2026-10-27 there is no DevTools shell or plugin contract code in Architecture. This task needs that shell first: either reopen 335 step 3 or file a dedicated shell task, and add it to `## Depends on`.
- Today's DevTools code lives in timewarp-state (`source/timewarp-state-devtools*`); this plugin must not take a dependency on it.
- Upstream: timewarp-taratibu task 059 (run events). That repo is private; this task links it by id only and builds against its published event contract.
- Source video kept on the bot box only; link the X post, do not commit the video.

## Learnings

<!-- Add dated entries as the work teaches something: what happened, what to do next time, and why. -->
