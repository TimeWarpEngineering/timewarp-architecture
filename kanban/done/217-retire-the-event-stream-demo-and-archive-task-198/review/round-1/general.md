# Round 1 — general
**Date:** 2026-09-14
**Scope reviewed:** branch task/217-retire-the-event-stream-demo-and-archive-task-198 vs origin/master (21ef1e90)

## Summary

Deletes the redundant event-stream SPA demo (slice, clone test, DI/nav/usings) now that `TrackEventBehavior` is the client `IPipelineBehavior` exemplar, and moves `[TrackEvent]` to Features substrate so Counter opts in without a product→product `[CrossSliceReference]`. Risk is low: inventory removal is complete against the required grep, `TrackEventBehavior` stays registered, smoke count correctly remains 148 (deleted clone test is under `tests/`, not a web-jaribu `source/` aggregator runfile), and task 198 is archived with the required reason. No open issues.

## Issues
