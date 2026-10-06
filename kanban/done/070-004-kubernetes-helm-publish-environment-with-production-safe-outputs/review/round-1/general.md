# Round 1 — general
**Date:** 2026-10-06
**Scope reviewed:** branch vs master (commit 4efb065d9)

## Summary

The `Publish:Target` switch is correct. Run mode always takes the Compose branch, the
Kubernetes-only parameters and resources exist only in publish mode with
`--Publish:Target=kubernetes`, and Compose/run-mode behaviour (environment, yarp port pinning, bundle)
is unchanged. The refactor into `services/aspire-publish.cs` matches the old Compose command step for
step, apart from the extra `-- --Publish:Target=<name>` passthrough. The `ASPIRECOMPUTE003` pragma
covers only the two registry calls. The CI upload path matches the output directory, `helm lint` is
gated on PATH in the dev CLI, and the preview pin is the newest 13.6 build on nuget.org (there is no
stable 13.6 release). I checked the chart in `artifacts/aspire-output/kubernetes` from the CLI run:
every Service is ClusterIP, the only Ingress points to `ingress-service`, secrets are in `*-secrets`
Secret objects, and `efmigrations/` holds only the SQL script. The safety suite is not vacuous for
the default flags. The issues below are a stale inline comment, a test robustness gap for
flag-off combinations, and documentation gaps an operator would hit.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:333
- Description: The inline comment says the persistent volume "renders postgres as a StatefulSet
  with a volumeClaimTemplate". The generated chart has no `volumeClaimTemplates`. It has a separate
  `PersistentVolumeClaim postgres-data` (`templates/postgres-data/postgres-data.yaml`) that the
  StatefulSet mounts through `spec.template.spec.volumes[].persistentVolumeClaim.claimName`. The
  Design region (line 150–153) and the test (`claimName.ShouldBe("postgres-data")`) describe it
  correctly. Only this comment is wrong, which breaks the reconcile-on-edit rule.
- Suggestion: Change it to "binds by name to the PersistentVolumeClaim postgres-data, which renders
  postgres as a single-replica StatefulSet mounting that claim".
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/container-apps/aspire/aspire-tests/kubernetes-publish-tests.cs:184
- Description: The Design region (lines 19–23) says every rule depends on which services the flags
  emitted. `Leaves(YamlMappingNode? node, …)` handles `null`. But `Mapping(...)` (line 416–417)
  uses the `Children[...]` indexer, which throws `KeyNotFoundException` and never returns null. So
  `Leaves(Mapping(Values, "config"|"parameters"), …)` (line 184) and
  `Leaves(Mapping(Values, "secrets"), …)` (line 190) crash when values.yaml has no such section. In
  the default chart, `secrets:` exists only because of web-server (Entra secret, postgres password)
  and postgres. A web-off combination such as api+grpc+yarp has no secret parameters, so the
  section is likely missing. The test would then fail with an exception, not a rule violation.
  `dev template-smoke` does not run aspire-tests per flag combination, so CI would not catch this.
  I did not generate a flag-off chart to confirm the section is absent. The crash path itself is
  confirmed from the code.
- Suggestion: Add a `MappingOrNull` helper (`TryGetValue`) for optional sections, use it at lines
  184 and 190, and keep the throwing `Mapping` for structural keys (`spec`, `metadata`).
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:145-149
- Description: The generated values.yaml has **two** independent postgres password keys,
  `secrets.postgres.postgres_password` (used by `postgres-secrets`) and
  `secrets.web_server.postgres_password` (used by `web-server-secrets` in
  `ConnectionStrings__postgres_db`, `POSTGRES_DB_PASSWORD` and `POSTGRES_DB_URI`). Both default to
  `""`. `aspire deploy` fills both from the one AddPostgres parameter. An operator who runs plain
  `helm install`/`upgrade` with this chart has to set both to the same value. If one is missing,
  postgres refuses to start (empty `POSTGRES_PASSWORD`), or web-server cannot authenticate. The
  Design region only says secrets are "backed by values.yaml `secrets.<resource>` with empty
  defaults", which does not warn about this.
- Suggestion: Add one sentence to the Design region (and to the Results "How to validate", if
  useful) naming both keys and saying they must match when the chart is installed without
  `aspire deploy`.
- Status: open

### Issue 4 — Severity: nit
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:127, 142, 151-153
- Description: The Design region calls ingress class, chart version and storage capacity
  "parameters", but `publishValueAsDefault: true` resolves them into the chart as literals at
  publish time (`ingressClassName: "nginx"` in `templates/cluster-ingress/cluster-ingress.yaml`,
  `storage: "10Gi"` in `postgres-data.yaml`, `version: "1.0.0"` in Chart.yaml). They are not
  `.Values` entries, so `helm install --set` cannot change them; the operator has to re-publish or
  deploy with the parameter. The Design region also names only `aspire publish -- --Publish:Target=kubernetes`.
  `aspire deploy` needs the same switch too, as the Results section says, but the region does not.
- Suggestion: Say these are publish/deploy-time parameters baked into the chart, and show the
  switch on `aspire deploy` as well.
- Status: open

### Issue 5 — Severity: nit
- File: tests/container-apps/aspire/aspire-tests/kubernetes-publish-tests.cs:248-257
- Description: `Publish_Should_RejectAnUnknownTarget` only checks for some
  `InvalidOperationException`. That type is general enough that an unrelated IOE thrown while
  creating the publish-mode builder would also pass the test.
- Suggestion: Capture the exception and assert that its message contains `Publish:Target` (or
  `'swarm'`).
- Status: open
