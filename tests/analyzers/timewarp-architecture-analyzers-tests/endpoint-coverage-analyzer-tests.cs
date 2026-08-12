#region Purpose
// Tests for TWA0006 (routed contract must have an endpoint or an explicit [ClientOnlyContract]
// opt-out). TWA0005 (MVC verb mismatch) was retired with BaseEndpoint (task 131 F-002).
#endregion

// ReSharper disable InconsistentNaming
namespace EndpointCoverageAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;

public class Should_Enforce_Endpoint_Coverage
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Enforce_Endpoint_Coverage>();

  // Minimal foundation surface so the test compilation resolves the shapes the analyzer
  // matches by metadata name (BaseFastEndpoint`2) and simple name (ApiRouteAttribute,
  // ClientOnlyContractAttribute).
  private const string Stubs =
    """
    #region Purpose
    // Test stubs.
    #endregion
    namespace TimeWarp.Foundation.Features
    {
      public enum HttpVerb { Get, Post, Delete, Put, Patch, Head, Options }
      public class BaseFastEndpoint<TRequest, TResponse> { }
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
    """;

  private static CSharpAnalyzerTest<EndpointCoverageAnalyzer, RoslynTestVerifier> Test(string source) =>
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

  public static async Task Given_Covered_Contract_IsClean()
  {
    const string source =
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
        using TimeWarp.Foundation.Features;

        public class GetWidgetEndpoint : BaseFastEndpoint<App.Contracts.GetWidget.Query, App.Contracts.GetWidget.Response>
        {
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Uncovered_Contract_Flags_TWA0006()
  {
    const string source =
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
        using TimeWarp.Foundation.Features;

        public class GetWidgetEndpoint : BaseFastEndpoint<App.Contracts.GetWidget.Query, App.Contracts.GetWidget.Response>
        {
        }
      }
      """;

    CSharpAnalyzerTest<EndpointCoverageAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(
      new DiagnosticResult(EndpointCoverageAnalyzer.MissingEndpointId, DiagnosticSeverity.Warning)
        .WithArguments("App.Contracts.OrphanedContract.Command", "api/Orphans", "Delete"));

    await test.RunAsync();
  }

  public static async Task Given_ClientOnlyContract_OptOut_IsClean()
  {
    const string source =
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

        // F-004: ClientOnly on outer operation also opts nested routed Query out of TWA0006.
        [ClientOnlyContract("Outer ClientOnly opt-out.")]
        public static class OuterClientOnly
        {
          [ApiRoute("api/OuterMock", HttpVerb.Get)]
          public sealed class Query { }
          public sealed class Response { }
        }
      }
      namespace App.Server
      {
        using TimeWarp.Foundation.Features;

        public class GetWidgetEndpoint : BaseFastEndpoint<App.Contracts.GetWidget.Query, App.Contracts.GetWidget.Response>
        {
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Compilation_Without_Endpoints_IsClean()
  {
    // A contracts-only (or SPA) compilation declares routed contracts but no endpoints — the
    // server-project gate must keep the diagnostic silent.
    const string source =
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

    await Test(source).RunAsync();
  }
}
