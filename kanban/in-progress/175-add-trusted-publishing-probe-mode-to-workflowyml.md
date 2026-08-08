# Add trusted-publishing probe mode to workflow.yml

## Description

org 458-009 probe (NuGet has no policy-enumeration API; probe = dispatch mode that runs only the nuget/login OIDC exchange and stops — success proves the workflow.yml policy matches; reference timewarp-nuru's workflow.yml).

## Checklist

- [ ] probe input added
- [ ] login step condition extended
- [ ] probe-result step added
- [ ] pipeline step skipped in probe mode
- [ ] YAML valid
