#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:package Microsoft.Extensions.Caching.Memory
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0007;IDE0008

// Co-located Jaribu: ResolveEntraTenant contract round-trip and problem mapping.
// Run standalone:  dotnet run source/container-apps/web/features/settings/resolve-entra-tenant/resolve-entra-tenant-tests.cs

#region Purpose
// ResolveEntraTenant JSON round-trip, domain validation, and distinct problem statuses.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Settings
{
  using System.Net;
  using System.Net.Http;
  using System.Text;
  using System.Text.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using Microsoft.Extensions.Caching.Memory;
  using Microsoft.Extensions.Logging.Abstractions;
  using OneOf;
  using Shouldly;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.Settings.ResolveEntraTenant;
  using LookupHandler = TimeWarp.Architecture.Features.Settings.Application.ResolveEntraTenant.Handler;

  [TestTag("Contracts")]
  public class ResolveEntraTenant_Given_
  {
    private static readonly Guid TenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<ResolveEntraTenant_Given_>();

    public static Task Response_Should_Round_Trip_Through_Json()
    {
      Response response = new(TenantId, "contoso.com");

      string json = JsonSerializer.Serialize(response, ContractSerializationDefaults.Options);
      Response? parsed = JsonSerializer.Deserialize<Response>(json, ContractSerializationDefaults.Options);

      json.ShouldContain("\"tenantId\"");
      json.ShouldContain("\"domain\":\"contoso.com\"");
      parsed.ShouldNotBeNull();
      parsed.TenantId.ShouldBe(TenantId);
      parsed.Domain.ShouldBe("contoso.com");
      return Task.CompletedTask;
    }

    public static Task Query_Should_Round_Trip_The_Domain()
    {
      Query query = new() { Domain = "contoso.com" };

      string json = JsonSerializer.Serialize(query, ContractSerializationDefaults.Options);
      Query? parsed = JsonSerializer.Deserialize<Query>(json, ContractSerializationDefaults.Options);

      parsed.ShouldNotBeNull();
      parsed.Domain.ShouldBe("contoso.com");
      query.GetRouteWithQueryString().ShouldBe("api/settings/entra-tenant?Domain=contoso.com");
      return Task.CompletedTask;
    }

    public static Task Blank_Domain_Should_Fail_Validation()
    {
      FluentValidation.Results.ValidationResult result = new Validator().Validate(new Query { Domain = "  " });
      result.IsValid.ShouldBeFalse();
      result.Errors.ShouldContain(error => error.ErrorMessage == "Enter a domain name.");
      return Task.CompletedTask;
    }

    public static async Task Not_Found_Should_Be_404()
    {
      OneOf<Response, SharedProblemDetails> result = await HandleAsync(
        _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
          Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });

      result.IsT1.ShouldBeTrue();
      result.AsT1.Status.ShouldBe(404);
      result.AsT1.Title.ShouldBe("Tenant not found");
    }

    public static async Task Unexpected_Issuer_Should_Be_502()
    {
      OneOf<Response, SharedProblemDetails> result = await HandleAsync(
        _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent("""{"issuer":"https://example.com/tenant/v2.0"}""", Encoding.UTF8, "application/json")
        });

      result.IsT1.ShouldBeTrue();
      result.AsT1.Status.ShouldBe(502);
      result.AsT1.Title.ShouldBe("Unexpected issuer");
    }

    public static async Task Found_Should_Return_The_Parsed_Tenant()
    {
      OneOf<Response, SharedProblemDetails> result = await HandleAsync(
        _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent(
            $$"""{"issuer":"https://login.microsoftonline.com/{{TenantId:D}}/v2.0"}""",
            Encoding.UTF8,
            "application/json")
        });

      result.IsT0.ShouldBeTrue();
      result.AsT0.TenantId.ShouldBe(TenantId);
      result.AsT0.Domain.ShouldBe("contoso.com");
    }

    private static async Task<OneOf<Response, SharedProblemDetails>> HandleAsync(
      Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
      var handler = new ScriptedHandler(respond);
      using var cache = new MemoryCache(new MemoryCacheOptions());
      var resolver = new EntraTenantResolver(
        new ScriptedFactory(handler),
        cache,
        NullLogger<EntraTenantResolver>.Instance,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromMinutes(1));
      var lookup = new LookupHandler(resolver);
      return await lookup.Handle(new Query { Domain = "contoso.com" }, CancellationToken.None);
    }

    private sealed class ScriptedHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(respond(request));
    }

    private sealed class ScriptedFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
      public HttpClient CreateClient(string name) =>
        new(handler, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan };
    }
  }
}
