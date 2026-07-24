# Add runtime smoke coverage for standalone yarp generated routes

## Description

From 107 review finding G3 (2026-07-23): the standalone yarp project now consumes the
generated WebServerApiRoutePrefixes via a cross-provider LoadFromMemory merge (in-memory routes
→ config-defined Web.Server cluster), but that path is BUILD-VERIFIED ONLY — aspire-tests
exercise the AppHost YARP, not standalone yarp. Accepted to ship because the AppHost is the
dogfooded/verified public chain (112); this task adds an automated runtime smoke for the
standalone gateway: boot yarp + web-server (no Aspire), request /api/Hello and /api/identity
through it, assert the generated carve-outs route to Web.Server (and foreign-Host handling on
the http cluster). Also covers the Development cluster https→http change made in 107.

## Checklist

- [ ] Test host that boots standalone yarp + web-server with test config
- [ ] Facts: generated prefix routes reach Web.Server (200/401-not-404); foreign-Host ok
- [ ] Runs in dev test (respect fixed-port discipline)

## Notes

Origin: 107 review/round-1 G3 verdict (ship + follow-up). Standalone ReverseProxy config is Development-only today; test scope matches.
