# Analyzer: csproj DefineConstants must agree with template.json feature-flag symbols

Realizes [[prefer-analyzers-sourcegen-over-inference]] for the surviving template flags
(api/grpc/web/yarp/postgres).

## Why

`#if(flag)` regions in template content are kept in the real repo build only because each csproj
hand-lists the flags in DefineConstants (web-spa, web-server, aspire-app-host, timewarp-testing,
web-spa-integration-tests...). Nothing checks that list against `.template.config/template.json`.
Miss one and the real build silently compiles WITHOUT a region the template would emit —
agreement-by-memory, the exact failure mode this repo eliminates with analyzers. Found during 071
when web-spa-integration-tests needed constants added by hand.

## Rule sketch

For every project whose sources contain `#if(<flag>)` where `<flag>` is a template.json symbol,
the project's DefineConstants must include that flag (build-time check; MSBuild target or analyzer
+ additional-files carrying template.json). Also flag the reverse: constants naming template
symbols that no source in the project uses (stale).

## Checklist

- [ ] Choose mechanism (analyzer with AdditionalFiles=template.json vs MSBuild target)
- [ ] Implement; next free TWPA id if analyzer
- [ ] Tests both directions (missing constant; stale constant)
- [ ] dev build 0/0

## Session

- Created: 2026-07-11 (spun out of 071)
