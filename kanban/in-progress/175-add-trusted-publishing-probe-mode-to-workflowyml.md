# Add trusted-publishing probe mode to workflow.yml

## Description

org 458-009 probe (NuGet has no policy-enumeration API; probe = dispatch mode that runs only the nuget/login OIDC exchange and stops — success proves the workflow.yml policy matches; reference timewarp-nuru's workflow.yml).

## Checklist

- [x] probe input added
- [x] login step condition extended
- [x] probe-result step added
- [x] pipeline step skipped in probe mode
- [x] YAML valid
