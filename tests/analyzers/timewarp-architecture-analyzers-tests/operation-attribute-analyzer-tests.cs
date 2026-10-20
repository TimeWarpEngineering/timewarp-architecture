#region Purpose
// Tests for TWA0035: a hosted contract's request without [Operation] gets an info suggestion.
#endregion

// ReSharper disable InconsistentNaming
namespace OperationAttributeAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;

public class Should_Suggest_Operation
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Suggest_Operation>();

  // ApiRouteAttribute and OperationAttribute are matched by FQN; ApiEndpointAttribute and
  // ClientOnlyContractAttribute by simple name (HostedRouteDiscovery).
  private const string Stubs =
    """
    #region Purpose
    // Test stubs.
    #endregion
    namespace TimeWarp.Foundation.Features
    {
      public enum HttpVerb { Get, Post, Delete, Put, Patch, Head, Options }
      public sealed class ApiRouteAttribute : System.Attribute
      {
        public ApiRouteAttribute(string routeTemplate, HttpVerb httpVerb) { }
      }
      public sealed class ClientOnlyContractAttribute : System.Attribute
      {
        public ClientOnlyContractAttribute(string reason) { }
      }
    }
    namespace TimeWarp.Architecture.Attributes
    {
      public sealed class ApiEndpointAttribute : System.Attribute { }
    }
    namespace TimeWarp.Contracts
    {
      public sealed class OperationAttribute : System.Attribute
      {
        public OperationAttribute(string name) { }
        public string? Description { get; set; }
      }
    }
    namespace Other
    {
      public sealed class OperationAttribute : System.Attribute
      {
        public OperationAttribute(string name) { }
      }
    }
    """;

  private static CSharpAnalyzerTest<OperationAttributeAnalyzer, RoslynTestVerifier> Test(string source) =>
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

  public static async Task Given_Hosted_Query_Without_Operation_Suggests_TWA0035()
  {
    const string source =
      """
      #region Purpose
      // Test feature.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Foundation.Features;

        [ApiEndpoint]
        public static class GetWidget
        {
          [ApiRoute("api/Widgets", HttpVerb.Get)]
          public sealed class {|TWA0035:Query|} { }
          public sealed class Response { }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Hosted_Command_With_Operation_IsClean()
  {
    const string source =
      """
      #region Purpose
      // Test feature.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Contracts;
        using TimeWarp.Foundation.Features;

        [ApiEndpoint]
        public static class CreateWidget
        {
          [ApiRoute("api/Widgets", HttpVerb.Post)]
          [Operation("widgets.create", Description = "Create a widget.")]
          public sealed class Command { }
          public sealed class Response { }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Whitespace_Operation_Name_Suggests_TWA0035()
  {
    const string source =
      """
      #region Purpose
      // Test feature.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Contracts;
        using TimeWarp.Foundation.Features;

        [ApiEndpoint]
        public static class GetWidget
        {
          [ApiRoute("api/Widgets", HttpVerb.Get)]
          [Operation("  ")]
          public sealed class {|TWA0035:Query|} { }
          public sealed class Response { }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Foreign_OperationAttribute_Suggests_TWA0035()
  {
    const string source =
      """
      #region Purpose
      // Test feature.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Foundation.Features;

        [ApiEndpoint]
        public static class GetWidget
        {
          [ApiRoute("api/Widgets", HttpVerb.Get)]
          [Other.Operation("widgets.get")]
          public sealed class {|TWA0035:Query|} { }
          public sealed class Response { }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_ClientOnly_Contract_IsClean()
  {
    const string source =
      """
      #region Purpose
      // Test feature.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Foundation.Features;

        public static class GetWidget
        {
          [ApiRoute("api/Widgets", HttpVerb.Get)]
          [ClientOnlyContract("Served by another host.")]
          public sealed class Query { }
          public sealed class Response { }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_ApiEndpoint_With_ClientOnly_Request_IsClean()
  {
    const string source =
      """
      #region Purpose
      // Test feature.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Foundation.Features;

        [ApiEndpoint]
        public static class GetWidget
        {
          [ApiRoute("api/Widgets", HttpVerb.Get)]
          [ClientOnlyContract("Served by another host.")]
          public sealed class Query { }
          public sealed class Response { }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Unrouted_Request_IsClean()
  {
    const string source =
      """
      #region Purpose
      // Test feature.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture.Attributes;

        [ApiEndpoint]
        public static class SendMessage
        {
          public sealed class Command { }
        }
      }
      """;

    await Test(source).RunAsync();
  }
}
