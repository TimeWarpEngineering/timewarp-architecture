# Triage — web-server-integration-tests (task 145-004)

| Path | Host? | Disposition |
|------|-------|-------------|
| hello/hello-endpoint-tests.cs | yes | **Co-located** → `source/.../hello/hello/hello-tests.cs` |
| hello/hello-handler-tests.cs | yes | Suite Jaribu (handler via mediator) |
| hello/hello-validator-tests.cs | no | Suite Jaribu static |
| admin/roles/create-role/* | mixed | Suite Jaribu (contracts already co-located; host tests stay suite for this pass) |
| admin/roles/roles-* | yes | Suite Jaribu host-level |
| analytics/track-event/* | mixed | Suite Jaribu |
| identity/* | yes | Suite Jaribu (ceremony/helpers host-level) |
| identity/*-validator*, relying-party | no | Suite Jaribu static |
| test/convention-tests/* | yes | Suite Jaribu; RunForever [Skip] |

**Hybrid outcome:** suite remains (does not dissolve) with identity + BFF host tests; one slice
endpoint (Hello) co-located as hybrid proof. Further co-location opportunistic on slice touch.
