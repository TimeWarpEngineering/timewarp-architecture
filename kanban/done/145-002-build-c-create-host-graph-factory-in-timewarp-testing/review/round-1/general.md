# Round 1 — general
**Date:** 2026-07-31
**Scope reviewed:** HostGraphFactory C-create implementation

## Summary

Implements C-create factory with explicit Api → Web → Yarp ordering, reverse dispose, port
preflight, per-host configure hooks, ContentRoot fix for Jaribu/runfile hosts, and exemplar
conversion. Requirements met; verified green.

## Verification

| Requirement | Result |
|-------------|--------|
| Factory Api / Web+Api / Web+Api+Yarp | Implemented |
| Owner IAsyncDisposable reverse order | HostGraph.DisposeAsync |
| No process statics / refcount | Confirmed |
| Per-host override + MockAccessTokenProvider | host-graph-factory-tests 2/2 |
| Weather exemplar CreateApiAsync | 2/2 standalone |
| api-jaribu-tests MTP | 4/4 |
| dev build 0/0 | Yes |
| Fixie web regression | 97 passed |

## Issues

_None._
