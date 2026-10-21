# Admin-managed xAI API key (write-only, encrypted at rest)

## Description

Make the site's xAI API key settable from Admin, next to Assistant name (task 361) and the
accepted Entra tenants (task 306), so a deployed app can turn Ask on without the Azure portal or
a redeploy. The key is a secret, so it is handled unlike the other SiteSettings values.

Depends on task 397 (durable Data Protection key ring).

## Checklist

- [ ] Admin → AI page (or a section on the existing settings page) to set, replace, and clear the key
- [ ] Write-only: the key never returns to the browser; Admin shows "configured, ends in …xxxx" and when/who set it
- [ ] Stored encrypted with ASP.NET Data Protection (purpose string specific to this key), in Postgres
- [ ] Host configuration wins: when `XAI:ApiKey` is set (environment variable, container-app secret, user secret in Development), it is used and Admin shows "managed by the host" read-only
- [ ] Ask picks up a changed key without a restart
- [ ] Only admins can read the status or change the key (authorization tests)
- [ ] Audit entry for set/replace/clear, never the value
- [ ] Contracts per `tw-web-api-contracts` (`[Operation]`, `ICommand`/`IQuery`)
- [ ] The not-configured hint (task 397) points at Admin → AI
- [ ] Without `postgres`: decide with Steve (host configuration only, Admin shows how to set it)
- [ ] Tests: round trip set → status → Ask uses it; host config precedence; clear; non-admin refused

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-21)

## Notes

- Origin: Steve 2026-10-21, "should this be configurable from Admin like Assistant name? And Tenant Ids".
- Task 399 (BYOK, per-user keys) builds on the same encrypted-secret storage; keep the storage reusable for it.
