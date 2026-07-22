# Disposition — 104-031

**Outcome: accepted-exceptions.**

Round 1, effort 2 (general + security reviewers). **0 critical / 0 major** from both.
Security reviewer verified the Host-trust model sound (forged Host selects only an
already-approved RP ID, never expands the allowlist; browser origin + rpIdHash binding blocks
completion; canonical-casing return is a real safety property). General reviewer verified
fail-closed completeness, validator edge coverage, hermetic-strip safety, and that the
full-ceremony-under-second-host test is genuine.

Findings (9): 2 fixed, 3 documented-accepted, 4 accepted/resolved. Zero open.

- **G1 (fixed)** — standalone-yarp identity-routing gap is PRE-EXISTING (not a 104-031
  regression). Fixed minimally: documented honestly in the yarp config + accessor region and
  attributed to task 107 (yarp-route generation); NO hand-maintained routes added (would have
  been scope creep + added to the very list 107 eliminates). Verified path is the AppHost YARP.
- **G2 (fixed)** — test cert-override COMMENT corrected. Correction of record (2026-07-22, post-close): the override is REQUIRED, not redundant. The build agent empirically disproved the reviewer's (and my) claim — removing `DangerousAcceptAnyServerCertificateValidator` fails 3 non-localhost cases with `AuthenticationException`, because `Headers.Host` moves .NET SocketsHttpHandler's certificate-NAME validation target (Host="webauthn-second.test" name-mismatches the localhost dev cert, aborting the handshake; TCP still targets localhost:7000). Override kept; comment now states this precisely. Commit `7acfdb4c`'s message calling it 'redundant' / 'Headers.Host doesn't change TLS SNI' is INCORRECT — this note is the authoritative record.
- **S1/S2/S3 (documented, accepted)** — append-only localhost persistence (loopback-confined,
  Steve-accepted), 400 membership-oracle (claim scoped to "no host reflected"), flat
  AllowedOrigins caveat (theoretical, blocked one layer down). All in the options Design region.
- **G2i/S4/S5 (accepted)** — ingress host-preservation lacks automated coverage (rests on the
  manual live-chain check the plan flagged; a yarp/SpaTest would close it — future);
  AllowedHosts:"*" leaves per-request allowlist as sole host gate (acceptable); hermetic strip
  exact-matches "secrets.json" (pinned by binding test, fails loud on framework change).
- **G4 (accepted)** — host-free unit tests grouped in the integration project per existing
  precedent.
- **G3 (resolved)** — the "97/1/0" is passed/skipped/failed: the 1 is the pre-existing
  `WebTestServerApplication_.Should.RunForever` manual skip, not a failure.

Follow-up spawned: none (standalone-yarp identity routing folds into existing task 107).
