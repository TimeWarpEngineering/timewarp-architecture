# Review framework — task 216

**Date:** 2026-09-14
**Host task:** kanban/in-progress/216-emit-trackevent-analytics-as-opentelemetry-events-and-a-counter-metric-on-the-server/
**Diff scope:** branch `task/216-emit-trackevent-analytics-as-opentelemetry-events` vs `origin/master` (product commit `662e56ad`; kitchen results commit `b6f4649b` excluded from product review)
**Plan / brief:** Replace the TrackEvent handler no-op with an OpenTelemetry sink: `[LoggerMessage]` log event (`event.name`, `correlation.id`) plus `Counter<long> analytics.events` tagged by `event.name`. Register the meter name in Aspire ServiceDefaults without referencing a web feature. Shared constants in foundation-contracts. Handler always returns success. Tests via FakeLogger + MetricCollector.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok review oracle 01a09e04-767d-7e63-a523-d78eac731913 (2026-09-14)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
