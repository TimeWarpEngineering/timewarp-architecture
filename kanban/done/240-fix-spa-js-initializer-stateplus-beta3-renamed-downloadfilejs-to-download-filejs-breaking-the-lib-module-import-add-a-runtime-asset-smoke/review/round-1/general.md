# Round 1 — general
**Date:** 2026-09-19
**Scope reviewed:** branch task/240 vs origin/master; SPA initializer kebab-case `_content` imports + runtime/template-smoke asset graph

## Summary

Product commit `9ce03f87` correctly retargets SPA initializer and Counter static `_content` imports (plus ambient `.d.ts` and the interop path comment) to TimeWarp.State / State.Plus 12.0.0-beta.3 kebab-case asset names; regenerated emit matches. The new HostGraph import-graph smoke (modules.json / Blazor-Web-Initializers → GET → parse static imports → 200 + JS content type, one-level root-relative recurse) and host-free `dev template-smoke` NuGet `staticwebassets/` check close the gap task 200 left open. Risk is low: fix is mechanical, gates use ordinal/case-sensitive emit asserts where Shouldly’s default would hide PascalCase, and live un-recycled `arch.timewarp.work` is out of scope per the brief.

## Issues
