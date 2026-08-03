#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(TestsDirectory)common/timewarp-testing/timewarp-testing.csproj
#:project $(SourceDirectory)libraries/timewarp-402/timewarp-402.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058
#:property DefineConstants=$(DefineConstants);api

// Co-located Jaribu integration tests for the metered pay-for-capability demo (task 104-011).
// Run standalone: dotnet run source/container-apps/web/features/metered-capability/invoke-metered-capability/invoke-metered-capability-tests.cs

#region Purpose
// Real-host proof: unpaid → 402 + PAYMENT-REQUIRED; prepaid credit → 200 + debit; mock settle → 200.
#endregion

#region Design
// Host: web-server (agent bearer already wired). configureWeb replaces IFacilitatorClient with an
// in-test mock so settle never hits the network, and PostConfigures MeteredCapabilityOptions to
// Enabled+valid PayTo. (WebApplicationHost layers AppContext.BaseDirectory/appsettings.json after
// ContentRoot appsettings.Development.json, which re-applies the base Enabled:false — hermetic
// test hosts must force the paid surface on.) Free routes are not exercised here — only the
// metered path. C-create HostGraphFactory per class; isolated HttpClient per call.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.MeteredCapability
{

  using System.Net;
  using System.Net.Http.Headers;
  using System.Buffers.Text;
  using System.Text.Json;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.DependencyInjection.Extensions;
  using Shouldly;
  using TimeWarp.Architecture.Configuration;
  using TimeWarp.Architecture.Features.Identity;
  using TimeWarp.Architecture.Features.MeteredCapability.Application;
  using TimeWarp.Architecture.Testing;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using TimeWarp.X402;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.MeteredCapability.InvokeMeteredCapability;

  [TestTag("Integration")]
  public class InvokeMeteredCapabilityEndpoint_Given_
  {
    private static HostGraph? Graph;
    private static WebTestServerApplication Web => Graph!.Web!;
    private static MockFacilitatorClient Facilitator = null!;

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<InvokeMeteredCapabilityEndpoint_Given_>();

    public static async Task SetupOnce()
    {
      Facilitator = new MockFacilitatorClient();
#if(api)
      Graph = await HostGraphFactory.CreateWebWithApiAsync(
        configureWeb: services => ReplaceFacilitator(services, Facilitator));
#else
      Graph = await HostGraphFactory.CreateWebAsync(
        configureWeb: services => ReplaceFacilitator(services, Facilitator));
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

    public static async Task PaymentRequired_402_Given_No_Credit_And_No_Signature()
    {
      Facilitator.Reset();
      var key = new IntegrationSoftwareAgentKey();
      (_, string accessToken) = await RegisterAndIssueToken(key, [AgentScopes.DemoInvoke]);

      using HttpResponseMessage response = await GetMeteredWithBearer(accessToken);

      response.StatusCode.ShouldBe(HttpStatusCode.PaymentRequired);
      response.Headers.Contains(PaymentHeaders.PaymentRequired).ShouldBeTrue();
      string challenge = response.Headers.GetValues(PaymentHeaders.PaymentRequired).Single();
      challenge.ShouldNotBeNullOrWhiteSpace();
      string? json = PaymentChallengeBuilder.TryDecodeHeaderPayload(challenge);
      json.ShouldNotBeNull();
      json.ShouldContain("metered-capability");
    }

    public static async Task Ok_Given_Prepaid_Credit_Debits_Ledger()
    {
      Facilitator.Reset();
      var key = new IntegrationSoftwareAgentKey();
      (PrincipalId principalId, string accessToken) = await RegisterAndIssueToken(key, [AgentScopes.DemoInvoke]);

      ICreditLedger ledger = Web.WebApplicationHost.ServiceProvider.GetRequiredService<ICreditLedger>();
      await ledger.CreditAsync(principalId, 1.00m, $"seed-{principalId.Value:N}");

      using HttpResponseMessage response = await GetMeteredWithBearer(accessToken);

      response.StatusCode.ShouldBe(HttpStatusCode.OK);
      string body = await response.Content.ReadAsStringAsync();
      Response? parsed = JsonSerializer.Deserialize<Response>(body, ContractSerializationDefaults.Options);
      parsed.ShouldNotBeNull();
      parsed.FundingSource.ShouldBe(MeteredCapabilityGranted.FundingCredit);
      parsed.BalanceAfter.ShouldBe(0.90m);
      (await ledger.GetBalanceAsync(principalId)).ShouldBe(0.90m);
      Facilitator.VerifyCalls.ShouldBe(0);
      Facilitator.SettleCalls.ShouldBe(0);
    }

    public static async Task Ok_Given_Valid_Payment_Signature_With_Mock_Facilitator()
    {
      Facilitator.Reset();
      Facilitator.VerifyResult = new FacilitatorVerifyResult { IsValid = true };
      Facilitator.SettleResult = new FacilitatorSettleResult
      {
        Success = true,
        Transaction = $"0xint-{Guid.NewGuid():N}",
        Network = "eip155:84532",
        Payer = "0x000000000000000000000000000000000000dEaD",
      };

      var key = new IntegrationSoftwareAgentKey();
      (PrincipalId principalId, string accessToken) = await RegisterAndIssueToken(key, [AgentScopes.DemoInvoke]);

      string signature = PaymentChallengeBuilder.EncodeHeaderPayload(new { x402Version = 2, scheme = "exact" });
      using HttpResponseMessage response = await GetMeteredWithBearer(accessToken, signature);

      response.StatusCode.ShouldBe(HttpStatusCode.OK);
      response.Headers.Contains(PaymentHeaders.PaymentResponse).ShouldBeTrue();
      string body = await response.Content.ReadAsStringAsync();
      Response? parsed = JsonSerializer.Deserialize<Response>(body, ContractSerializationDefaults.Options);
      parsed.ShouldNotBeNull();
      parsed.FundingSource.ShouldBe(MeteredCapabilityGranted.FundingPayment);
      parsed.BalanceAfter.ShouldBe(0m);
      ICreditLedger ledger = Web.WebApplicationHost.ServiceProvider.GetRequiredService<ICreditLedger>();
      (await ledger.GetBalanceAsync(principalId)).ShouldBe(0m);
      Facilitator.VerifyCalls.ShouldBe(1);
      Facilitator.SettleCalls.ShouldBe(1);
    }

    public static async Task Unauthorized_Given_No_Bearer()
    {
      using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
      HttpResponseMessage response = await client.GetAsync(Query.RouteTemplate);
      response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    public static async Task Forbidden_Given_IdentityRead_Scope_Only()
    {
      var key = new IntegrationSoftwareAgentKey();
      (_, string accessToken) = await RegisterAndIssueToken(key, [AgentScopes.IdentityRead]);

      HttpResponseMessage response = await GetMeteredWithBearer(accessToken);

      response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static void ReplaceFacilitator(IServiceCollection services, MockFacilitatorClient mock)
    {
      services.RemoveAll<IFacilitatorClient>();
      services.AddSingleton<IFacilitatorClient>(mock);
      // See Design region: force paid surface on for this class (test host config layering).
      services.PostConfigure<MeteredCapabilityOptions>(options =>
      {
        options.Enabled = true;
        options.PayTo = "0x000000000000000000000000000000000000dEaD";
        options.Price = "$0.10";
        options.Resource = "/api/demo/metered-capability";
        options.Network = "eip155:84532";
        options.FacilitatorBase = FacilitatorUrls.X402Org;
      });
    }

    private static async Task<(PrincipalId PrincipalId, string AccessToken)> RegisterAndIssueToken(
      IntegrationSoftwareAgentKey key,
      List<string> scopes)
    {
      OneOf.OneOf<StartAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> registerStart =
        await Web.GetResponse<StartAgentKeyRegistration.Response>(
          new StartAgentKeyRegistration.Command(),
          CancellationToken.None);
      byte[] registerChallenge = Base64Url.DecodeFromChars(registerStart.AsT0.Challenge);
      byte[] registerSignature = key.Sign(AgentKeyCeremonyType.Registration, registerChallenge);

      var registerCommand = new CompleteAgentKeyRegistration.Command
      {
        PublicKey = Base64Url.EncodeToString(key.SpkiPublicKey),
        Challenge = Base64Url.EncodeToString(registerChallenge),
        Signature = Base64Url.EncodeToString(registerSignature),
      };

      OneOf.OneOf<CompleteAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> registerResult =
        await Web.GetResponse<CompleteAgentKeyRegistration.Response>(registerCommand, CancellationToken.None);
      registerResult.IsT0.ShouldBeTrue("Registration setup should succeed.");
      PrincipalId principalId = registerResult.AsT0.PrincipalId;

      OneOf.OneOf<StartAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> tokenStart =
        await Web.GetResponse<StartAgentTokenIssuance.Response>(
          new StartAgentTokenIssuance.Command(),
          CancellationToken.None);
      byte[] tokenChallenge = Base64Url.DecodeFromChars(tokenStart.AsT0.Challenge);
      byte[] tokenSignature = key.Sign(AgentKeyCeremonyType.TokenIssuance, tokenChallenge);

      var tokenCommand = new CompleteAgentTokenIssuance.Command
      {
        KeyId = registerResult.AsT0.KeyId,
        Challenge = Base64Url.EncodeToString(tokenChallenge),
        Signature = Base64Url.EncodeToString(tokenSignature),
        Scopes = scopes,
      };

      OneOf.OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> tokenResult =
        await Web.GetResponse<CompleteAgentTokenIssuance.Response>(tokenCommand, CancellationToken.None);
      tokenResult.IsT0.ShouldBeTrue("Token issuance setup should succeed.");

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

      // Caller owns disposal of the response; client is short-lived per call.
      HttpResponseMessage response = await client.GetAsync(Query.RouteTemplate);
      // Detach response from client lifetime so disposing client does not dispose content early.
      // (HttpClient dispose disposes pending handlers; response already buffered for typical sizes.)
      return response;
    }
  }

  /// <summary>Deterministic software agent key for HTTP integration tests (local duplicate of suite fixture).</summary>
  internal sealed class IntegrationSoftwareAgentKey
  {
    private readonly byte[] D;
    private readonly byte[] X;
    private readonly byte[] Y;

    public IntegrationSoftwareAgentKey()
    {
      using System.Security.Cryptography.ECDsa ecdsa =
        System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP256);
      System.Security.Cryptography.ECParameters parameters = ecdsa.ExportParameters(includePrivateParameters: true);
      D = parameters.D!;
      X = parameters.Q.X!;
      Y = parameters.Q.Y!;
    }

    public byte[] SpkiPublicKey
    {
      get
      {
        using System.Security.Cryptography.ECDsa ecdsa = CreateEcdsa();
        return ecdsa.ExportSubjectPublicKeyInfo();
      }
    }

    public byte[] Sign(AgentKeyCeremonyType ceremonyType, byte[] challenge)
    {
      byte[] signedData = AgentKeyProof.BuildSignedData(ceremonyType, challenge);
      using System.Security.Cryptography.ECDsa ecdsa = CreateEcdsa();
      return ecdsa.SignData(
        signedData,
        System.Security.Cryptography.HashAlgorithmName.SHA256,
        System.Security.Cryptography.DSASignatureFormat.Rfc3279DerSequence);
    }

    private System.Security.Cryptography.ECDsa CreateEcdsa() =>
      System.Security.Cryptography.ECDsa.Create(
        new System.Security.Cryptography.ECParameters
        {
          Curve = System.Security.Cryptography.ECCurve.NamedCurves.nistP256,
          D = D,
          Q = new System.Security.Cryptography.ECPoint { X = X, Y = Y },
        });
  }

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

} // namespace
