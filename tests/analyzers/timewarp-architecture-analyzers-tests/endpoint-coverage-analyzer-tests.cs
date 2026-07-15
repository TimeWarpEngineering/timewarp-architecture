#region Purpose
// Tests for TWA0005 (endpoint verb must match contract verb) and TWA0006 (routed contract
// must have an endpoint or an explicit [ClientOnlyContract] opt-out).
#endregion

// ReSharper disable InconsistentNaming
namespace EndpointCoverageAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;

public class Should_Enforce_Endpoint_Coverage
{
  // Minimal foundation/MVC surface so the test compilation resolves the shapes the analyzer
  // matches by metadata name (BaseEndpoint`2) and simple name (ApiRouteAttribute, Http*Attribute,
  // ClientOnlyContractAttribute).
  private const string Stubs =
    """
    #region Purpose
    // Test stubs.
    #endregion
    namespace TimeWarp.Foundation.Features
    {
      public enum HttpVerb { Get, Post, Delete, Put, Patch, Head, Options }
      public class BaseEndpoint<TRequest, TResponse> { }
      public sealed class ClientOnlyContractAttribute : System.Attribute
      {
        public ClientOnlyContractAttribute(string reason) { }
      }
    }
    namespace TimeWarp.Architecture
    {
      internal sealed class ApiRouteAttribute : System.Attribute
      {
        public ApiRouteAttribute(string routeTemplate, TimeWarp.Foundation.Features.HttpVerb httpVerb) { }
      }
    }
    namespace Microsoft.AspNetCore.Mvc
    {
      public sealed class HttpGetAttribute : System.Attribute { public HttpGetAttribute(string template) { } }
      public sealed class HttpPostAttribute : System.Attribute { public HttpPostAttribute(string template) { } }
    }
    """;

  private static CSharpAnalyzerTest<EndpointCoverageAnalyzer, FixieVerifier> Test(string source) =>
    new()
    {
      TestState =
      {
        Sources =
        {
          ("Stubs.cs", Stubs),
          ("Feature.cs", source)
        }
      }
    };

  public static async Task Given_Covered_Contract_With_Matching_Verb_IsClean()
  {
    const string Source =
      """
      #region Purpose
      // Test feature.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Foundation.Features;

        public static class GetWidget
        {
          [ApiRoute("api/Widgets", HttpVerb.Get)]
          public sealed class Query { }
          public sealed class Response { }
        }
      }
      namespace App.Server
      {
        using Microsoft.AspNetCore.Mvc;
        using TimeWarp.Foundation.Features;

        public class GetWidgetEndpoint : BaseEndpoint<App.Contracts.GetWidget.Query, App.Contracts.GetWidget.Response>
        {
          [HttpGet("api/Widgets")]
          public void Process() { }
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  public static async Task Given_Verb_Mismatch_Flags_TWA0005()
  {
    const string Source =
      """
      #region Purpose
      // Test feature.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Foundation.Features;

        public static class CreateWidget
        {
          [ApiRoute("api/Widgets", HttpVerb.Post)]
          public sealed class Command { }
          public sealed class Response { }
        }
      }
      namespace App.Server
      {
        using Microsoft.AspNetCore.Mvc;
        using TimeWarp.Foundation.Features;

        public class CreateWidgetEndpoint : BaseEndpoint<App.Contracts.CreateWidget.Command, App.Contracts.CreateWidget.Response>
        {
          [{|TWA0005:HttpGet("api/Widgets")|}]
          public void Process() { }
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  public static async Task Given_Uncovered_Contract_Flags_TWA0006()
  {
    const string Source =
      """
      #region Purpose
      // Test feature.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Foundation.Features;

        public static class GetWidget
        {
          [ApiRoute("api/Widgets", HttpVerb.Get)]
          public sealed class Query { }
          public sealed class Response { }
        }

        public static class OrphanedContract
        {
          [ApiRoute("api/Orphans", HttpVerb.Delete)]
          public sealed class Command { }
          public sealed class Response { }
        }
      }
      namespace App.Server
      {
        using Microsoft.AspNetCore.Mvc;
        using TimeWarp.Foundation.Features;

        public class GetWidgetEndpoint : BaseEndpoint<App.Contracts.GetWidget.Query, App.Contracts.GetWidget.Response>
        {
          [HttpGet("api/Widgets")]
          public void Process() { }
        }
      }
      """;

    CSharpAnalyzerTest<EndpointCoverageAnalyzer, FixieVerifier> test = Test(Source);
    test.ExpectedDiagnostics.Add(
      new DiagnosticResult(EndpointCoverageAnalyzer.MissingEndpointId, DiagnosticSeverity.Warning)
        .WithArguments("App.Contracts.OrphanedContract.Command", "api/Orphans", "Delete"));

    await test.RunAsync();
  }

  public static async Task Given_ClientOnlyContract_OptOut_IsClean()
  {
    const string Source =
      """
      #region Purpose
      // Test feature.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Foundation.Features;

        public static class GetWidget
        {
          [ApiRoute("api/Widgets", HttpVerb.Get)]
          public sealed class Query { }
          public sealed class Response { }
        }

        public static class MockOnlyContract
        {
          [ApiRoute("api/MockOnly", HttpVerb.Get)]
          [ClientOnlyContract("Served by SPA mock mode only.")]
          public sealed class Query { }
          public sealed class Response { }
        }
      }
      namespace App.Server
      {
        using Microsoft.AspNetCore.Mvc;
        using TimeWarp.Foundation.Features;

        public class GetWidgetEndpoint : BaseEndpoint<App.Contracts.GetWidget.Query, App.Contracts.GetWidget.Response>
        {
          [HttpGet("api/Widgets")]
          public void Process() { }
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  public static async Task Given_Compilation_Without_Endpoints_IsClean()
  {
    // A contracts-only (or SPA) compilation declares routed contracts but no endpoints — the
    // server-project gate must keep both diagnostics silent.
    const string Source =
      """
      #region Purpose
      // Test feature.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Foundation.Features;

        public static class GetWidget
        {
          [ApiRoute("api/Widgets", HttpVerb.Get)]
          public sealed class Query { }
          public sealed class Response { }
        }
      }
      """;

    await Test(Source).RunAsync();
  }
}
