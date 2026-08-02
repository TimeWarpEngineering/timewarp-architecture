#region Purpose
// Tests for TWA0013 (missing auth posture), TWA0014 (conflicting auth posture), and TWA0020
// ([ApiEndpoint] + [ClientOnlyContract]) on [ApiEndpoint] contracts.
#endregion

// ReSharper disable InconsistentNaming
namespace EndpointAuthPostureAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;

public class Should_Enforce_Auth_Posture
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Enforce_Auth_Posture>();

  // Minimal stub surface — matched by the analyzer via simple name, same convention as
  // EndpointCoverageAnalyzer's stubs. IAuthApiRequest/AuthApiRequestAttribute stand in for BOTH the
  // manual interface form and the [AuthApiRequest] mixin-generator-expanded form: the analyzer only
  // cares about the simple names "IAuthApiRequest"/"AuthApiRequestAttribute" being present, exactly
  // like the real ContractsMixinGenerator-expanded attribute/interface would be by the time this
  // analyzer runs on a real compilation.
  private const string Stubs =
    """
    #region Purpose
    // Test stubs.
    #endregion
    namespace TimeWarp.Foundation.Features
    {
      public enum HttpVerb { Get, Post, Delete, Put, Patch, Head, Options }
      public interface IApiRequest { }
      public interface IAuthApiRequest : IApiRequest { }
    }
    namespace TimeWarp.Architecture
    {
      internal sealed class ApiRouteAttribute : System.Attribute
      {
        public ApiRouteAttribute(string routeTemplate, TimeWarp.Foundation.Features.HttpVerb httpVerb) { }
      }
      internal sealed class AuthApiRequestAttribute : System.Attribute { }
      internal sealed class ClientOnlyContractAttribute : System.Attribute
      {
        public ClientOnlyContractAttribute(string reason) { }
      }
    }
    namespace TimeWarp.Architecture.Attributes
    {
      public sealed class ApiEndpointAttribute : System.Attribute { }

      public sealed class EndpointAuthorizeAttribute : System.Attribute
      {
        public string? Policy { get; set; }
        public string? AuthenticationSchemes { get; set; }
        public string? Roles { get; set; }
      }

      public sealed class EndpointAllowAnonymousAttribute : System.Attribute
      {
        public EndpointAllowAnonymousAttribute(string reason) { }
      }
    }
    """;

  private static CSharpAnalyzerTest<EndpointAuthPostureAnalyzer, RoslynTestVerifier> Test(string source) =>
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

  public static async Task Given_No_Marker_Flags_TWA0013()
  {
    const string Source =
      """
      #region Purpose
      // Test feature.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Foundation.Features;

        [{|TWA0013:ApiEndpoint|}]
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

  public static async Task Given_EndpointAuthorize_Only_IsClean()
  {
    const string Source =
      """
      #region Purpose
      // Test feature.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Foundation.Features;

        [ApiEndpoint]
        [EndpointAuthorize(Policy = "SomePolicy")]
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

  public static async Task Given_EndpointAllowAnonymous_Only_IsClean()
  {
    const string Source =
      """
      #region Purpose
      // Test feature.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Foundation.Features;

        [ApiEndpoint]
        [EndpointAllowAnonymous("Public demo endpoint.")]
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

  public static async Task Given_Both_Markers_Flags_TWA0014()
  {
    const string Source =
      """
      #region Purpose
      // Test feature.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Foundation.Features;

        [{|TWA0014:ApiEndpoint|}]
        [EndpointAuthorize(Policy = "SomePolicy")]
        [EndpointAllowAnonymous("Contradictory.")]
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

  public static async Task Given_AllowAnonymous_With_Manual_IAuthApiRequest_Flags_TWA0014()
  {
    const string Source =
      """
      #region Purpose
      // Test feature.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Foundation.Features;

        [{|TWA0014:ApiEndpoint|}]
        [EndpointAllowAnonymous("Should be flagged: Query declares IAuthApiRequest.")]
        public static class GetWidget
        {
          [ApiRoute("api/Widgets", HttpVerb.Get)]
          public sealed class Query : IAuthApiRequest
          {
            public System.Guid UserId { get; set; }
          }
          public sealed class Response { }
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  public static async Task Given_AllowAnonymous_With_AuthApiRequest_Mixin_Attribute_Flags_TWA0014()
  {
    // Simulates the [AuthApiRequest] mixin form (get-roles.cs's shape): the attribute is applied
    // directly rather than the interface being hand-declared — ContractsMixinGenerator would expand
    // this into an IAuthApiRequest implementation too, but this test exercises the attribute-only
    // detection path independently (see the analyzer's Design region on why both checks exist).
    const string Source =
      """
      #region Purpose
      // Test feature.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Foundation.Features;

        [{|TWA0014:ApiEndpoint|}]
        [EndpointAllowAnonymous("Should be flagged: Query carries [AuthApiRequest].")]
        public static class GetWidgets
        {
          [ApiRoute("api/Widgets", HttpVerb.Get)]
          [AuthApiRequest]
          public sealed class Query { }
          public sealed class Response { }
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  public static async Task Given_Non_ApiEndpoint_Contract_IsClean()
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
      """;

    await Test(Source).RunAsync();
  }

  public static async Task Given_ApiEndpoint_With_ClientOnly_On_Outer_Flags_TWA0020()
  {
    const string Source =
      """
      #region Purpose
      // Test feature.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Foundation.Features;

        [{|TWA0020:ApiEndpoint|}]
        [ClientOnlyContract("SPA mock only.")]
        [EndpointAllowAnonymous("Would be public if hosted.")]
        public static class MockOnlyWidget
        {
          [ApiRoute("api/Widgets", HttpVerb.Get)]
          public sealed class Query { }
          public sealed class Response { }
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  public static async Task Given_ApiEndpoint_With_ClientOnly_On_Nested_Flags_TWA0020()
  {
    const string Source =
      """
      #region Purpose
      // Test feature.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Foundation.Features;

        [{|TWA0020:ApiEndpoint|}]
        [EndpointAllowAnonymous("Would be public if hosted.")]
        public static class NestedMockWidget
        {
          [ApiRoute("api/Widgets", HttpVerb.Get)]
          [ClientOnlyContract("ClientOnly on nested Query.")]
          public sealed class Query { }
          public sealed class Response { }
        }
      }
      """;

    await Test(Source).RunAsync();
  }
}
