# Round 1 — general
**Date:** 2026-09-16
**Scope reviewed:** branch task/232 vs origin/master; Directory.Packages.props + aspire-app-host.csproj + surrounding Aspire call sites / CI / breaking-change grep

## Summary

Product change is a coherent one-train Aspire bump 13.5.3 → 13.5.4 (AppHost SDK, Hosting.Yarp/PostgreSQL/Testing, EF hosting preview `13.5.4-preview.1.26464.4`) plus ServiceDiscovery / ServiceDiscovery.Yarp / Http.Resilience to 10.10.0. Diff matches implementer Results; no leftover 13.5.3 in props/csproj/workflows; breaking-change greps and wait-edge / ingress / `AspireUseCliBundle=false` claims re-verified against source. Overall risk is low for a same-minor patch train with no API edits required.

## Issues

