#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(TestsDirectory)common/timewarp-testing/timewarp-testing.csproj
#:project $(SourceDirectory)libraries/timewarp-402/timewarp-402.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058
#:property DefineConstants=$(DefineConstants);api

// Co-located Jaribu integration tests for the voluntary x402 tip jar (task 104-009).
// Run standalone: dotnet run source/container-apps/web/features/tip/submit-tip/submit-tip-tests.cs

#region Purpose
// Real-host proof: disabled → 503 tips_disabled; enabled unpaid → 402; mock settle → 200 thank-you.
#endregion

#region Design
// Host: web-server. configureWeb replaces IFacilitatorClient with an in-test mock so settle never
// hits the network. TipOptions are PostConfigured per test scenario (enabled vs disabled).
// Free routes are not exercised here — only /api/tip. Anonymous: no agent bearer. C-create
// HostGraphFactory per class; isolated HttpClient per call.
// Distinct from metered (104-011): no ledger debit, no agent scope.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Tip
{

  using System.Net;
  using System.Text.Json;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.DependencyInjection.Extensions;
  using Shouldly;
  using TimeWarp.Architecture.Configuration;
  using TimeWarp.Architecture.Features.Tip.Application;
  using TimeWarp.Architecture.Testing;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Jaribu;
  using TimeWarp.X402;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.Tip.SubmitTip;

  [TestTag("Integration")]
  public class SubmitTipEndpoint_Given_
  {
    private static HostGraph? Graph;
    private static WebTestServerApplication Web => Graph!.Web!;
    private static MockFacilitatorClient Facilitator = null!;

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<SubmitTipEndpoint_Given_>();

    public static async Task SetupOnce()
    {
      Facilitator = new MockFacilitatorClient();
#if(api)
      Graph = await HostGraphFactory.CreateWebWithApiAsync(
        configureWeb: services => ReplaceFacilitator(services, Facilitator, enableTip: true));
#else
      Graph = await HostGraphFactory.CreateWebAsync(
        configureWeb: services => ReplaceFacilitator(services, Facilitator, enableTip: true));
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

    public static async Task PaymentRequired_402_Given_Enabled_And_No_Signature_Get()
    {
      Facilitator.Reset();
      ForceTipEnabled(true);

      using HttpResponseMessage response = await GetTip();

      response.StatusCode.ShouldBe(HttpStatusCode.PaymentRequired);
      response.Headers.Contains(PaymentHeaders.PaymentRequired).ShouldBeTrue();
      string challenge = response.Headers.GetValues(PaymentHeaders.PaymentRequired).Single();
      challenge.ShouldNotBeNullOrWhiteSpace();
      string? json = PaymentChallengeBuilder.TryDecodeHeaderPayload(challenge);
      json.ShouldNotBeNull();
      json.ShouldContain("api/tip");
      Facilitator.VerifyCalls.ShouldBe(0);
    }

    public static async Task PaymentRequired_402_Given_Enabled_And_No_Signature_Post()
    {
      Facilitator.Reset();
      ForceTipEnabled(true);

      using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
      using HttpResponseMessage response = await client.PostAsync(
        SubmitTipPost.Command.RouteTemplate,
        content: null);

      response.StatusCode.ShouldBe(HttpStatusCode.PaymentRequired);
      response.Headers.Contains(PaymentHeaders.PaymentRequired).ShouldBeTrue();
    }

    public static async Task ServiceUnavailable_503_Given_Tips_Disabled()
    {
      Facilitator.Reset();
      ForceTipEnabled(false);

      using HttpResponseMessage response = await GetTip();

      response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
      response.Headers.Contains(PaymentHeaders.PaymentRequired).ShouldBeFalse();
      string body = await response.Content.ReadAsStringAsync();
      body.ShouldContain("tips_disabled");
    }

    public static async Task Ok_ThankYou_Given_Valid_Payment_Signature_With_Mock_Facilitator()
    {
      Facilitator.Reset();
      ForceTipEnabled(true);
      Facilitator.VerifyResult = new FacilitatorVerifyResult { IsValid = true };
      Facilitator.SettleResult = new FacilitatorSettleResult
      {
        Success = true,
        Transaction = $"0xtip-{Guid.NewGuid():N}",
        Network = "eip155:84532",
        Payer = "0x000000000000000000000000000000000000dEaD",
      };

      string signature = PaymentChallengeBuilder.EncodeHeaderPayload(new { x402Version = 2, scheme = "exact" });
      using HttpResponseMessage response = await GetTip(signature);

      response.StatusCode.ShouldBe(HttpStatusCode.OK);
      response.Headers.Contains(PaymentHeaders.PaymentResponse).ShouldBeTrue();
      string body = await response.Content.ReadAsStringAsync();
      Response? parsed = JsonSerializer.Deserialize<Response>(body, ContractSerializationDefaults.Options);
      parsed.ShouldNotBeNull();
      parsed.Tip.ShouldBeTrue();
      parsed.Message.ShouldContain("Thank you");
      parsed.Amount.ShouldBe("$0.10");
      Facilitator.VerifyCalls.ShouldBe(1);
      Facilitator.SettleCalls.ShouldBe(1);
    }

    private static void ReplaceFacilitator(
      IServiceCollection services,
      MockFacilitatorClient mock,
      bool enableTip)
    {
      services.RemoveAll<IFacilitatorClient>();
      services.AddSingleton<IFacilitatorClient>(mock);
      // See Design region: force tip surface for this class (test host config layering).
      services.PostConfigure<TipOptions>(options =>
      {
        options.Enabled = enableTip;
        options.PayTo = "0x000000000000000000000000000000000000dEaD";
        options.Price = "$0.10";
        options.Resource = "/api/tip";
        options.Network = "eip155:84532";
        options.FacilitatorBase = FacilitatorUrls.X402Org;
        options.RequiresFacilitatorAuth = false;
        options.HasFacilitatorAuth = false;
      });
    }

    private static void ForceTipEnabled(bool enabled)
    {
      Microsoft.Extensions.Options.IOptions<TipOptions> options =
        Web.WebApplicationHost.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TipOptions>>();
      // IOptions is a snapshot — mutate the underlying OptionsMonitor value via PostConfigure is
      // already applied at host start. For per-test enable/disable we re-register is not possible
      // after host start; instead mutate the Options.Value if it is a mutable class instance.
      TipOptions value = options.Value;
      value.Enabled = enabled;
      if (enabled)
      {
        value.PayTo = "0x000000000000000000000000000000000000dEaD";
      }
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

      return await client.GetAsync(Query.RouteTemplate);
    }
  }

  /// <summary>Unit tests for TIP_* env overlay (mocked env dictionary — no process env pollution).</summary>
  [TestTag("Unit")]
  public class TipEnvironment_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<TipEnvironment_Given_>();

    public static Task Enabled_Only_When_Env_Is_String_True()
    {
      var options = new TipOptions { Enabled = false };
      TipEnvironment.ApplyFromEnvironment(options, name => name == TipEnvironment.Enabled ? "true" : null);
      options.Enabled.ShouldBeTrue();

      options = new TipOptions { Enabled = true };
      TipEnvironment.ApplyFromEnvironment(options, name => name == TipEnvironment.Enabled ? "1" : null);
      options.Enabled.ShouldBeFalse();

      options = new TipOptions { Enabled = true };
      TipEnvironment.ApplyFromEnvironment(options, name => name == TipEnvironment.Enabled ? "false" : null);
      options.Enabled.ShouldBeFalse();

      return Task.CompletedTask;
    }

    public static Task PayTo_Network_Price_From_Env()
    {
      var options = new TipOptions();
      TipEnvironment.ApplyFromEnvironment(
        options,
        name => name switch
        {
          TipEnvironment.PayTo => "0x000000000000000000000000000000000000dEaD",
          TipEnvironment.Network => "eip155:84532",
          TipEnvironment.Price => "$0.25",
          _ => null,
        });

      options.PayTo.ShouldBe("0x000000000000000000000000000000000000dEaD");
      options.Network.ShouldBe("eip155:84532");
      options.Price.ShouldBe("$0.25");
      return Task.CompletedTask;
    }

    public static Task Mainnet_Sets_RequiresFacilitatorAuth_And_Cdp_Auth()
    {
      var options = new TipOptions { Network = "eip155:84532" };
      TipEnvironment.ApplyFromEnvironment(
        options,
        name => name switch
        {
          TipEnvironment.Network => "eip155:8453",
          TipEnvironment.CdpApiKeyId => "key-id",
          TipEnvironment.CdpApiKeySecret => "key-secret",
          _ => null,
        });

      options.Network.ShouldBe("eip155:8453");
      options.RequiresFacilitatorAuth.ShouldBeTrue();
      options.HasFacilitatorAuth.ShouldBeTrue();
      options.FacilitatorBase.ShouldBe(FacilitatorUrls.CdpPlatform);
      return Task.CompletedTask;
    }
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
