#region Purpose
// Program 104 exit criterion: three CI-safe sunny paths — human passkey onboard, agent
// register→pay→call, voluntary tip — on a real in-proc web host with mock facilitator.
#endregion

#region Design
// Harness (not Playwright): HostGraphFactory C-create Web(+Api) host; IntegrationSoftwareAuthenticator
// stands in for the browser/platform authenticator; IntegrationSoftwareAgentKey stands in for a
// headless agent SDK; MockFacilitatorClient replaces IFacilitatorClient so settle never hits a live
// chain (human asleep). Tip + MeteredCapability surfaces are PostConfigured Enabled with a public
// dead PayTo (test host config layering re-applies Enabled:false from base appsettings otherwise).
//
// Why not Playwright for the Login CTA: full browser WebAuthn needs a virtual authenticator (CDP)
// or a real hardware/platform key — heavy and flaky in CI. The software authenticator exercises
// the same Start/Complete HTTP ceremonies and cookie middleware the SPA Login path uses, so the
// product story is proven without a browser. Manual Proton Pass smoke remains the human dogfood
// path from 104-016.
//
// Pipeline: this suite lives in web-server-integration-tests (already under `dev test` globs).
// Filter: `-- --filter-class Program104Sunny` or `-- --filter-tag Program104Sunny`.
// Related deeper coverage (edge/negative): passkey-registration-tests, invoke-metered-capability-tests
// (co-located), submit-tip-tests (co-located). This file is the named program-exit green bar only.
//
// 104-014 agent money path: path 2 is the automated register→402→pay→call sequence; leave 014
// free to add CLI/script documentation if still open concurrently.
#endregion

namespace Program104SunnyPaths_;

using System.Buffers.Text;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Features.MeteredCapability.Application;
using TimeWarp.Architecture.Features.Tip.Application;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using TimeWarp.Identity;
using TimeWarp.X402;
// Contract outer types share names with Application handlers — alias the contracts only.
using Metered = TimeWarp.Architecture.Features.MeteredCapability.InvokeMeteredCapability;
using TipContract = TimeWarp.Architecture.Features.Tip.SubmitTip;

/// <summary>Program 104 exit criterion — three sunny paths, mock payment chain.</summary>
[TestTag("Program104Sunny")]
[TestTag("Integration")]
public class SunnyPaths_
{
  private const string RpId = "localhost";
  private const string DeadPayTo = "0x000000000000000000000000000000000000dEaD";

  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;
  private static MockFacilitatorClient Facilitator = null!;
  private static TestApiService TestApiService => new(Web.HttpClient, ContractSerializationDefaults.Options);

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SunnyPaths_>();

  public static async Task SetupOnce()
  {
    Facilitator = new MockFacilitatorClient();
#if(api)
    Graph = await HostGraphFactory.CreateWebWithApiAsync(
      configureWeb: services => ConfigurePaidSurfaces(services, Facilitator));
#else
    Graph = await HostGraphFactory.CreateWebAsync(
      configureWeb: services => ConfigurePaidSurfaces(services, Facilitator));
#endif
  }

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  /// <summary>
  /// (1) Human passkey onboard: register ceremony → Principal + identity-session cookie.
  /// </summary>
  public static async Task Human_Passkey_Onboard_Creates_Principal_And_Session()
  {
    IntegrationSoftwareAuthenticator authenticator = new();
    CompletePasskeyRegistration.Command completeCommand = await BuildValidPasskeyRegistration(authenticator);

    HttpResponseMessage httpResponse =
      await TestApiService.GetHttpResponseMessage(completeCommand, CancellationToken.None);

    httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    httpResponse.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieValues).ShouldBeTrue();
    setCookieValues.ShouldNotBeNull();
    setCookieValues.ShouldContain(
      value => value.Contains(IdentitySessionDefaults.CookieName, StringComparison.Ordinal));

    string json = await httpResponse.Content.ReadAsStringAsync();
    CompletePasskeyRegistration.Response? completeResponse =
      JsonSerializer.Deserialize<CompletePasskeyRegistration.Response>(
        json,
        ContractSerializationDefaults.Options);
    completeResponse.ShouldNotBeNull();
    completeResponse.PrincipalId.IsEmpty.ShouldBeFalse();

    GetCurrentSession.Response session = await GetCurrentSessionWithCookie(setCookieValues!);
    session.IsAuthenticated.ShouldBeTrue();
    session.PrincipalId.ShouldBe(completeResponse.PrincipalId);
  }

  /// <summary>
  /// (2) Agent register → unpaid metered call 402 → mock pay → capability succeeds + Funded tier.
  /// </summary>
  public static async Task Agent_Register_Pay_Then_Call_Metered_Capability()
  {
    Facilitator.Reset();
    Facilitator.VerifyResult = new FacilitatorVerifyResult { IsValid = true };
    Facilitator.SettleResult = new FacilitatorSettleResult
    {
      Success = true,
      Transaction = $"0xsunny-agent-{Guid.NewGuid():N}",
      Network = "eip155:84532",
      Payer = DeadPayTo,
    };

    var key = new IntegrationSoftwareAgentKey();
    (PrincipalId principalId, string accessToken) =
      await RegisterAgentAndIssueToken(key, [AgentScopes.DemoInvoke]);

    IPrincipalStore principals = Web.WebApplicationHost.ServiceProvider.GetRequiredService<IPrincipalStore>();
    (await principals.GetPrincipalAsync(principalId))!.TrustTier.ShouldBe(TrustTier.Keyed);

    // Unpaid → 402 + PAYMENT-REQUIRED (money path starts with a challenge).
    using HttpResponseMessage unpaid = await GetMeteredWithBearer(accessToken);
    unpaid.StatusCode.ShouldBe(HttpStatusCode.PaymentRequired);
    unpaid.Headers.Contains(PaymentHeaders.PaymentRequired).ShouldBeTrue();
    string challenge = unpaid.Headers.GetValues(PaymentHeaders.PaymentRequired).Single();
    challenge.ShouldNotBeNullOrWhiteSpace();
    PaymentChallengeBuilder.TryDecodeHeaderPayload(challenge).ShouldNotBeNull();

    // Mock settle → capability granted + Funded (104-013 composition).
    string signature = PaymentChallengeBuilder.EncodeHeaderPayload(new { x402Version = 2, scheme = "exact" });
    using HttpResponseMessage paid = await GetMeteredWithBearer(accessToken, signature);

    paid.StatusCode.ShouldBe(HttpStatusCode.OK);
    paid.Headers.Contains(PaymentHeaders.PaymentResponse).ShouldBeTrue();
    string body = await paid.Content.ReadAsStringAsync();
    Metered.Response? parsed = JsonSerializer.Deserialize<Metered.Response>(
      body,
      ContractSerializationDefaults.Options);
    parsed.ShouldNotBeNull();
    parsed.FundingSource.ShouldBe(MeteredCapabilityGranted.FundingPayment);
    parsed.Message.ShouldNotBeNullOrWhiteSpace();

    Principal? funded = await principals.GetPrincipalAsync(principalId);
    funded.ShouldNotBeNull();
    funded.TrustTier.ShouldBe(TrustTier.Funded);
    funded.IsFundedAndActive.ShouldBeTrue();
    Facilitator.VerifyCalls.ShouldBe(1);
    Facilitator.SettleCalls.ShouldBe(1);
  }

  /// <summary>
  /// (3) Voluntary tip: unpaid 402 → mock settle → thank-you (no principal required).
  /// </summary>
  public static async Task Voluntary_Tip_Settles_With_Mock_Facilitator()
  {
    Facilitator.Reset();
    Facilitator.VerifyResult = new FacilitatorVerifyResult { IsValid = true };
    Facilitator.SettleResult = new FacilitatorSettleResult
    {
      Success = true,
      Transaction = $"0xsunny-tip-{Guid.NewGuid():N}",
      Network = "eip155:84532",
      Payer = DeadPayTo,
    };

    using HttpResponseMessage unpaid = await GetTip();
    unpaid.StatusCode.ShouldBe(HttpStatusCode.PaymentRequired);
    unpaid.Headers.Contains(PaymentHeaders.PaymentRequired).ShouldBeTrue();

    string signature = PaymentChallengeBuilder.EncodeHeaderPayload(new { x402Version = 2, scheme = "exact" });
    using HttpResponseMessage paid = await GetTip(signature);

    paid.StatusCode.ShouldBe(HttpStatusCode.OK);
    paid.Headers.Contains(PaymentHeaders.PaymentResponse).ShouldBeTrue();
    string body = await paid.Content.ReadAsStringAsync();
    TipContract.Response? parsed =
      JsonSerializer.Deserialize<TipContract.Response>(body, ContractSerializationDefaults.Options);
    parsed.ShouldNotBeNull();
    parsed.Tip.ShouldBeTrue();
    parsed.Message.ShouldContain("Thank you");
    Facilitator.VerifyCalls.ShouldBe(1);
    Facilitator.SettleCalls.ShouldBe(1);
  }

  private static void ConfigurePaidSurfaces(IServiceCollection services, MockFacilitatorClient mock)
  {
    services.RemoveAll<IFacilitatorClient>();
    services.AddSingleton<IFacilitatorClient>(mock);

    services.PostConfigure<MeteredCapabilityOptions>(options =>
    {
      options.Enabled = true;
      options.PayTo = DeadPayTo;
      options.Price = "$0.10";
      options.Resource = "/api/demo/metered-capability";
      options.Network = "eip155:84532";
      options.FacilitatorBase = FacilitatorUrls.X402Org;
    });

    services.PostConfigure<TipOptions>(options =>
    {
      options.Enabled = true;
      options.PayTo = DeadPayTo;
      options.Price = "$0.10";
      options.Resource = "/api/tip";
      options.Network = "eip155:84532";
      options.FacilitatorBase = FacilitatorUrls.X402Org;
      options.RequiresFacilitatorAuth = false;
      options.HasFacilitatorAuth = false;
    });
  }

  private static async Task<CompletePasskeyRegistration.Command> BuildValidPasskeyRegistration(
    IntegrationSoftwareAuthenticator authenticator)
  {
    OneOf<StartPasskeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await Web.GetResponse<StartPasskeyRegistration.Response>(
        new StartPasskeyRegistration.Command(),
        CancellationToken.None);
    start.IsT0.ShouldBeTrue("StartPasskeyRegistration should succeed for sunny-path setup.");

    byte[] challenge = ReadChallenge(start.AsT0.OptionsJson);
    string origin = Web.HttpClient.BaseAddress!.GetLeftPart(UriPartial.Authority);

    byte[] authenticatorData = authenticator.BuildAuthenticatorData(RpId, includeAttestedCredentialData: true);
    byte[] attestationObject = IntegrationSoftwareAuthenticator.BuildAttestationObject(authenticatorData);
    byte[] clientDataJson =
      IntegrationSoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, origin);

    return new CompletePasskeyRegistration.Command
    {
      CredentialId = Base64Url.EncodeToString(authenticator.CredentialId),
      ClientDataJson = Base64Url.EncodeToString(clientDataJson),
      AttestationObject = Base64Url.EncodeToString(attestationObject),
    };
  }

  private static async Task<(PrincipalId PrincipalId, string AccessToken)> RegisterAgentAndIssueToken(
    IntegrationSoftwareAgentKey key,
    List<string> scopes)
  {
    OneOf<StartAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> registerStart =
      await Web.GetResponse<StartAgentKeyRegistration.Response>(
        new StartAgentKeyRegistration.Command(),
        CancellationToken.None);
    registerStart.IsT0.ShouldBeTrue();
    byte[] registerChallenge = Base64Url.DecodeFromChars(registerStart.AsT0.Challenge);
    byte[] registerSignature = key.Sign(AgentKeyCeremonyType.Registration, registerChallenge);

    var registerCommand = new CompleteAgentKeyRegistration.Command
    {
      PublicKey = Base64Url.EncodeToString(key.SpkiPublicKey),
      Challenge = Base64Url.EncodeToString(registerChallenge),
      Signature = Base64Url.EncodeToString(registerSignature),
    };

    OneOf<CompleteAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> registerResult =
      await Web.GetResponse<CompleteAgentKeyRegistration.Response>(registerCommand, CancellationToken.None);
    registerResult.IsT0.ShouldBeTrue("Agent registration should succeed.");
    PrincipalId principalId = registerResult.AsT0.PrincipalId;

    OneOf<StartAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> tokenStart =
      await Web.GetResponse<StartAgentTokenIssuance.Response>(
        new StartAgentTokenIssuance.Command(),
        CancellationToken.None);
    tokenStart.IsT0.ShouldBeTrue();
    byte[] tokenChallenge = Base64Url.DecodeFromChars(tokenStart.AsT0.Challenge);
    byte[] tokenSignature = key.Sign(AgentKeyCeremonyType.TokenIssuance, tokenChallenge);

    var tokenCommand = new CompleteAgentTokenIssuance.Command
    {
      KeyId = registerResult.AsT0.KeyId,
      Challenge = Base64Url.EncodeToString(tokenChallenge),
      Signature = Base64Url.EncodeToString(tokenSignature),
      Scopes = scopes,
    };

    OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> tokenResult =
      await Web.GetResponse<CompleteAgentTokenIssuance.Response>(tokenCommand, CancellationToken.None);
    tokenResult.IsT0.ShouldBeTrue("Token issuance should succeed.");

    return (principalId, tokenResult.AsT0.AccessToken);
  }

  private static async Task<HttpResponseMessage> GetMeteredWithBearer(
    string accessToken,
    string? paymentSignature = null)
  {
    HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    if (!string.IsNullOrWhiteSpace(paymentSignature))
    {
      client.DefaultRequestHeaders.TryAddWithoutValidation(
        PaymentHeaders.PaymentSignature,
        paymentSignature);
    }

    return await client.GetAsync(Metered.Query.RouteTemplate);
  }

  private static async Task<HttpResponseMessage> GetTip(string? paymentSignature = null)
  {
    HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    if (!string.IsNullOrWhiteSpace(paymentSignature))
    {
      client.DefaultRequestHeaders.TryAddWithoutValidation(
        PaymentHeaders.PaymentSignature,
        paymentSignature);
    }

    return await client.GetAsync(TipContract.Query.RouteTemplate);
  }

  private static byte[] ReadChallenge(string optionsJson)
  {
    using JsonDocument document = JsonDocument.Parse(optionsJson);
    string challengeBase64Url = document.RootElement.GetProperty("challenge").GetString()!;
    return Base64Url.DecodeFromChars(challengeBase64Url);
  }

  private static async Task<GetCurrentSession.Response> GetCurrentSessionWithCookie(
    IEnumerable<string> setCookieValues)
  {
    string? sessionCookie = setCookieValues.FirstOrDefault(
      value => value.Contains(IdentitySessionDefaults.CookieName, StringComparison.Ordinal));
    sessionCookie.ShouldNotBeNull("Expected a Set-Cookie header carrying the identity-session cookie.");

    using HttpClient isolatedClient = new() { BaseAddress = Web.HttpClient.BaseAddress };
    isolatedClient.DefaultRequestHeaders.Add("Cookie", sessionCookie.Split(';')[0]);

    HttpResponseMessage response = await isolatedClient.GetAsync(GetCurrentSession.Query.RouteTemplate);
    string json = await response.Content.ReadAsStringAsync();

    return JsonSerializer.Deserialize<GetCurrentSession.Response>(json, ContractSerializationDefaults.Options)
      ?? throw new InvalidOperationException("GetCurrentSession response deserialized to null.");
  }

  /// <summary>In-test facilitator mock — never hits the network (same shape as tip/metered runfiles).</summary>
  internal sealed class MockFacilitatorClient : IFacilitatorClient
  {
    public FacilitatorVerifyResult VerifyResult { get; set; } =
      new() { IsValid = false, InvalidReason = "invalid_payload" };

    public FacilitatorSettleResult SettleResult { get; set; } =
      new() { Success = false, ErrorReason = "not_implemented", Network = "eip155:84532" };

    public int VerifyCalls { get; private set; }
    public int SettleCalls { get; private set; }

    public void Reset()
    {
      VerifyCalls = 0;
      SettleCalls = 0;
      VerifyResult = new FacilitatorVerifyResult { IsValid = false, InvalidReason = "invalid_payload" };
      SettleResult = new FacilitatorSettleResult
      {
        Success = false,
        ErrorReason = "not_implemented",
        Network = "eip155:84532",
      };
    }

    public Task<FacilitatorSupported> GetSupportedAsync(CancellationToken cancellationToken = default) =>
      Task.FromResult(new FacilitatorSupported
      {
        Kinds =
        [
          new FacilitatorKind { X402Version = 2, Scheme = "exact", Network = "eip155:84532" },
        ],
      });

    public Task<FacilitatorVerifyResult> VerifyAsync(
      FacilitatorPaymentRequest request,
      CancellationToken cancellationToken = default)
    {
      VerifyCalls++;
      return Task.FromResult(VerifyResult);
    }

    public Task<FacilitatorSettleResult> SettleAsync(
      FacilitatorPaymentRequest request,
      CancellationToken cancellationToken = default)
    {
      SettleCalls++;
      return Task.FromResult(SettleResult);
    }
  }
}
