# Passkey hosts on a deployed target

Web-server's `WebAuthnOptions:AllowedRpIds` includes the public
ingress host. The C# default is `localhost`, which covers compose and a kind port-forward. Azure
Container Apps adds `ingress.<environment domain>` (the Container Apps environment's domain
output, not the ingress endpoint, so web-server and ingress do not cycle). A custom domain is the
optional AppHost parameter `passkey-rp-host` (a bare DNS name, no scheme or port). Set it in
AppHost user secrets or `Parameters__passkey-rp-host`. `dev deploy` copies a user-secret value
into the aspire environment. It is emitted only when it is set and is not `localhost`. A custom
domain adds its own entry beside `localhost` and, on ACA, beside the ingress host. A value that
is not a bare DNS name refuses the deploy.

Passkeys work over the plain-http localhost URLs `dev open` prints: with `WebAuthnOptions:AllowedOrigins` empty,
web-server accepts `http` only when the origin host is loopback (`localhost`, `127.0.0.1`,
`[::1]`) and equals the selected RP ID — the same rule browsers use for a secure context. Any
other host needs https, or an explicit `AllowedOrigins` list (an exact match, no fallback).
