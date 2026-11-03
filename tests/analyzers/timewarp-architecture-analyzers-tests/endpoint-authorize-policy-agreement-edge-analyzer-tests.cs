#region Purpose
// Tests for TWA0024 edge cases: CORS policies, missing policies, client-only, and the diagnostic id.
#endregion

// ReSharper disable InconsistentNaming
namespace EndpointAuthorizePolicyAgreementAnalyzer_;

using TimeWarp.Architecture.Analyzers;
using static EndpointAuthorizePolicyAgreementAnalyzer_.EndpointAuthorizePolicyAgreementTestFixture;

public class Should_Enforce_Policy_Agreement_Without_Policy_Registration
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Enforce_Policy_Agreement_Without_Policy_Registration>();

  public static async Task Given_Cors_AddPolicy_Does_Not_Count()
  {
    const string source =
      """
      #region Purpose
      // CORS AddPolicy must not satisfy an authorization Policy name.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Foundation.Features;

        [ApiEndpoint]
        [EndpointAuthorize(Policy = {|TWA0024:"AllowAll"|})]
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
        using Microsoft.AspNetCore.Cors.Infrastructure;

        public class GetWidgetEndpoint : BaseFastEndpoint<App.Contracts.GetWidget.Query, App.Contracts.GetWidget.Response> { }

        public static class Program
        {
          public static void Configure(CorsOptions options)
          {
            options.AddPolicy("AllowAll", _ => { });
          }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_No_Policy_IsClean()
  {
    const string source =
      """
      #region Purpose
      // [EndpointAuthorize] with no named Policy has nothing to agree on.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Foundation.Features;

        [ApiEndpoint]
        [EndpointAuthorize]
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

        public class GetWidgetEndpoint : BaseFastEndpoint<App.Contracts.GetWidget.Query, App.Contracts.GetWidget.Response> { }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_ClientOnly_IsClean()
  {
    const string source =
      """
      #region Purpose
      // ClientOnly hosted-opt-out is not checked (not generated).
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Foundation.Features;

        [ApiEndpoint]
        [EndpointAuthorize(Policy = "not-registered")]
        [ClientOnlyContract("SPA mock only.")]
        public static class MockOnly
        {
          [ApiRoute("api/MockOnly", HttpVerb.Get)]
          public sealed class Query { }
          public sealed class Response { }
        }
      }
      namespace App.Server
      {
        using TimeWarp.Foundation.Features;
        using Microsoft.AspNetCore.Authorization;

        public class CoveredEndpoint : BaseFastEndpoint<App.Server.Covered.Query, App.Server.Covered.Response> { }

        public static class Covered
        {
          public sealed class Query { }
          public sealed class Response { }
        }

        public static class Program
        {
          public static void Configure(AuthorizationBuilder builder)
          {
            builder.AddPolicy("other", _ => { });
          }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Compilation_Without_Endpoints_IsClean()
  {
    const string source =
      """
      #region Purpose
      // Contracts-only compilation must stay silent (TWA0006 pairing gate).
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Foundation.Features;

        [ApiEndpoint]
        [EndpointAuthorize(Policy = "not-registered")]
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

  public static Task DiagnosticId_Is_TWA0024()
  {
    EndpointAuthorizePolicyAgreementAnalyzer analyzer = new();
    analyzer.SupportedDiagnostics.Length.ShouldBe(1);
    analyzer.SupportedDiagnostics[0].Id.ShouldBe("TWA0024");
    return Task.CompletedTask;
  }
}
