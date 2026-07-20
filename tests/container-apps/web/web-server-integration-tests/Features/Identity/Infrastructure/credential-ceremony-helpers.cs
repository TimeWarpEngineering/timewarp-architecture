#region Purpose
// Shared ceremony-composition helpers for task 104-005's credential-management integration tests
// (Credential_List_Tests, Credential_Add_Tests, Credential_Revoke_Tests) — the WebAuthn/agent-key
// "start a ceremony, build a cryptographic proof" boilerplate that all three files need identically.
#endregion

#region Design
// Deliberate DEPARTURE from this folder's usual per-file duplication convention (see
// Roles_Authorization_Tests.cs's Design region for that convention's own rationale: "~15 lines isn't
// worth the coupling"). This helper is a different scale: three new test files each need BOTH "mint a
// passkey-backed session" and "mint an agent bearer token with a given scope set" as setup, plus a
// second, independent "build a fresh WebAuthn/agent-key proof against a live challenge" primitive for
// the add-credential flows — duplicating that across three files would be ~150+ near-identical lines,
// not ~15, and the cost of a byte-layout mistake diverging between copies (a subtly wrong
// attestationObject that still happens to pass one test's assertions by accident) is exactly the kind
// of risk IntegrationSoftwareAuthenticator/IntegrationSoftwareAgentKey themselves exist to avoid.
// Extracting HERE, not duplicating three times, keeps that same principle intact.
// These helpers return ready-to-use CONTRACT FIELDS (base64url strings), not HTTP responses — each
// test file still owns which endpoint it posts the result to (CompletePasskeyRegistration for a first
// credential vs. AddPasskey for an additional one) and which auth header (if any) it attaches, so the
// actual behavior under test stays local to each file, not hidden in this helper.
// Uses WebTestServerApplication.HttpClient (the shared per-class ambient client) for the ceremony
// calls themselves — every ceremony endpoint involved here (Start*/Complete*) is anonymous, so
// nothing about auth headers or the ambient cookie jar affects them; callers extract the returned
// cookie/token and attach it to their OWN isolated HttpClient for the actual authenticated call under
// test (same isolation posture as Roles_Authorization_Tests.cs's MintIdentitySessionCookie).
#endregion

namespace TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;

using System.Buffers.Text;
using System.Text.Json;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Identity;

internal static class CredentialCeremonyHelpers
{
  /// <summary>
  /// Starts a passkey registration ceremony and builds a fresh, valid attestation for it — the raw
  /// material for either CompletePasskeyRegistration.Command (first credential) or AddPasskey.Command
  /// (additional credential); both share the same three base64url fields.
  /// </summary>
  public static async Task<(string CredentialId, string ClientDataJson, string AttestationObject)> BuildPasskeyAttestationAsync
  (
    WebTestServerApplication app,
    IntegrationSoftwareAuthenticator? authenticator = null
  )
  {
    // Optional caller-supplied authenticator: passing the SAME instance across two calls produces
    // two independently-valid attestations (fresh challenge each time) that nonetheless share the
    // same underlying credential HANDLE — the only way to exercise a genuine duplicate-handle 409
    // without literally replaying an already-consumed challenge (which would 400 first).
    authenticator ??= new IntegrationSoftwareAuthenticator();

    OneOf<StartPasskeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await app.GetResponse<StartPasskeyRegistration.Response>(new StartPasskeyRegistration.Command(), CancellationToken.None);
    byte[] challenge = ReadChallenge(start.AsT0.OptionsJson);
    string origin = app.HttpClient.BaseAddress!.GetLeftPart(UriPartial.Authority);

    byte[] authenticatorData = authenticator.BuildAuthenticatorData("localhost", includeAttestedCredentialData: true);
    byte[] attestationObject = IntegrationSoftwareAuthenticator.BuildAttestationObject(authenticatorData);
    byte[] clientDataJson = IntegrationSoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, origin);

    return
    (
      Base64Url.EncodeToString(authenticator.CredentialId),
      Base64Url.EncodeToString(clientDataJson),
      Base64Url.EncodeToString(attestationObject)
    );
  }

  /// <summary>
  /// Registers a brand-new passkey-backed principal and mints its identity-session cookie — the
  /// "cookie principal with one credential" starting point every cookie-authenticated
  /// credential-management test needs.
  /// </summary>
  public static async Task<(PrincipalId PrincipalId, string SessionCookie)> RegisterPasskeyAndMintSessionAsync(WebTestServerApplication app)
  {
    (string credentialId, string clientDataJson, string attestationObject) = await BuildPasskeyAttestationAsync(app);

    var registerCommand = new CompletePasskeyRegistration.Command
    {
      CredentialId = credentialId,
      ClientDataJson = clientDataJson,
      AttestationObject = attestationObject
    };

    var testApiService = new TestApiService(app.HttpClient, ContractSerializationDefaults.Options);
    HttpResponseMessage registerResponse = await testApiService.GetHttpResponseMessage(registerCommand, CancellationToken.None);
    registerResponse.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieValues).ShouldBeTrue();
    string? sessionCookie = setCookieValues!.FirstOrDefault(value => value.Contains(IdentitySessionDefaults.CookieName, StringComparison.Ordinal));
    sessionCookie.ShouldNotBeNull("Expected passkey registration to issue an identity-session cookie.");

    string json = await registerResponse.Content.ReadAsStringAsync();
    CompletePasskeyRegistration.Response? response =
      JsonSerializer.Deserialize<CompletePasskeyRegistration.Response>(json, ContractSerializationDefaults.Options);
    response.ShouldNotBeNull();

    return (response.PrincipalId, sessionCookie.Split(';')[0]);
  }

  /// <summary>
  /// Starts an agent-key registration ceremony and signs the resulting challenge for the given key —
  /// the raw material for either CompleteAgentKeyRegistration.Command (first credential) or
  /// AddAgentKey.Command (additional credential); both share the same three base64url fields.
  /// </summary>
  public static async Task<(string PublicKey, string Challenge, string Signature)> BuildAgentKeyRegistrationProofAsync
  (
    WebTestServerApplication app,
    IntegrationSoftwareAgentKey key
  )
  {
    OneOf<StartAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await app.GetResponse<StartAgentKeyRegistration.Response>(new StartAgentKeyRegistration.Command(), CancellationToken.None);
    byte[] challenge = Base64Url.DecodeFromChars(start.AsT0.Challenge);
    byte[] signature = key.Sign(AgentKeyCeremonyType.Registration, challenge);

    return
    (
      Base64Url.EncodeToString(key.SpkiPublicKey),
      Base64Url.EncodeToString(challenge),
      Base64Url.EncodeToString(signature)
    );
  }

  /// <summary>
  /// Registers a brand-new agent-key-backed principal and issues it a bearer token carrying the given
  /// scopes — the "bearer principal with one credential and a token" starting point every
  /// bearer-authenticated credential-management test needs.
  /// </summary>
  public static async Task<(PrincipalId PrincipalId, string KeyId, string AccessToken)> RegisterAgentKeyAndIssueTokenAsync
  (
    WebTestServerApplication app,
    IntegrationSoftwareAgentKey key,
    List<string> scopes
  )
  {
    (string publicKey, string registerChallenge, string registerSignature) = await BuildAgentKeyRegistrationProofAsync(app, key);

    var registerCommand = new CompleteAgentKeyRegistration.Command
    {
      PublicKey = publicKey,
      Challenge = registerChallenge,
      Signature = registerSignature
    };

    OneOf<CompleteAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> registerResult =
      await app.GetResponse<CompleteAgentKeyRegistration.Response>(registerCommand, CancellationToken.None);
    registerResult.IsT0.ShouldBeTrue("Agent key registration setup should succeed.");

    OneOf<StartAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> tokenStart =
      await app.GetResponse<StartAgentTokenIssuance.Response>(new StartAgentTokenIssuance.Command(), CancellationToken.None);
    byte[] tokenChallenge = Base64Url.DecodeFromChars(tokenStart.AsT0.Challenge);
    byte[] tokenSignature = key.Sign(AgentKeyCeremonyType.TokenIssuance, tokenChallenge);

    var tokenCommand = new CompleteAgentTokenIssuance.Command
    {
      KeyId = registerResult.AsT0.KeyId,
      Challenge = Base64Url.EncodeToString(tokenChallenge),
      Signature = Base64Url.EncodeToString(tokenSignature),
      Scopes = scopes
    };

    OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> tokenResult =
      await app.GetResponse<CompleteAgentTokenIssuance.Response>(tokenCommand, CancellationToken.None);
    tokenResult.IsT0.ShouldBeTrue("Token issuance setup should succeed.");

    return (registerResult.AsT0.PrincipalId, registerResult.AsT0.KeyId, tokenResult.AsT0.AccessToken);
  }

  private static byte[] ReadChallenge(string optionsJson)
  {
    using JsonDocument document = JsonDocument.Parse(optionsJson);
    string challengeBase64Url = document.RootElement.GetProperty("challenge").GetString()!;
    return Base64Url.DecodeFromChars(challengeBase64Url);
  }
}
