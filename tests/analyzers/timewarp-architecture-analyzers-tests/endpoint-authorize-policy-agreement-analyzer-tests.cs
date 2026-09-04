#region Purpose
// Tests for TWA0024: hosted [EndpointAuthorize] Policy must be a policy this server registers.
#endregion

// ReSharper disable InconsistentNaming
namespace EndpointAuthorizePolicyAgreementAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;

public class Should_Enforce_Policy_Agreement
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Enforce_Policy_Agreement>();

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
    namespace Microsoft.AspNetCore.Authorization
    {
      public class AuthorizationOptions
      {
        public void AddPolicy(string name, System.Action<object> configure) { }
      }

      public class AuthorizationBuilder
      {
        public AuthorizationBuilder AddPolicy(string name, System.Action<object> configure) => this;
      }
    }
    namespace Microsoft.AspNetCore.Cors.Infrastructure
    {
      public class CorsOptions
      {
        public void AddPolicy(string name, System.Action<object> configure) { }
      }
    }
    namespace TimeWarp.Architecture.Configuration
    {
      public static class IdentitySessionDefaults
      {
        public const string AuthenticatedPolicy = "identity-session-authenticated";
      }

      public static class AgentTokenDefaults
      {
        public const string IdentityReadPolicy = "agent-scope:identity:read";
      }

      public static class CredentialManagementDefaults
      {
        public const string Policy = "credential.manage.self";
      }
    }
    namespace TimeWarp.Architecture.Features
    {
      public static class PermissionIds
      {
        public const string ClaimType = "permission";
        public const string IdentityRead = "identity.read";
        public const string CredentialManageSelf = "credential.manage.self";
      }

      public static class PermissionPolicyRegistration
      {
        public static void AddPermissionPolicies(Microsoft.AspNetCore.Authorization.AuthorizationOptions options) { }
      }
    }
    """;

  private static CSharpAnalyzerTest<EndpointAuthorizePolicyAgreementAnalyzer, RoslynTestVerifier> Test(string source) =>
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

  public static async Task Given_Literal_Matching_AddPolicy_IsClean()
  {
    const string source =
      """
      #region Purpose
      // identity-session-authenticated vs IdentitySessionDefaults.AuthenticatedPolicy.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Foundation.Features;

        [ApiEndpoint]
        [EndpointAuthorize(Policy = "identity-session-authenticated")]
        public static class GetRole
        {
          [ApiRoute("api/Roles/{RoleId:guid}", HttpVerb.Get)]
          public sealed class Query { }
          public sealed class Response { }
        }
      }
      namespace App.Server
      {
        using TimeWarp.Architecture.Configuration;
        using TimeWarp.Foundation.Features;
        using Microsoft.AspNetCore.Authorization;

        public class GetRoleEndpoint : BaseFastEndpoint<App.Contracts.GetRole.Query, App.Contracts.GetRole.Response> { }

        public static class Program
        {
          public static void Configure(AuthorizationBuilder builder)
          {
            builder.AddPolicy(IdentitySessionDefaults.AuthenticatedPolicy, _ => { });
          }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Agent_Scope_Literal_Matching_AddPolicy_IsClean()
  {
    const string source =
      """
      #region Purpose
      // agent-scope:identity:read vs AgentTokenDefaults.IdentityReadPolicy.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Foundation.Features;

        [ApiEndpoint]
        [EndpointAuthorize(Policy = "agent-scope:identity:read")]
        public static class GetAgentIdentity
        {
          [ApiRoute("api/identity/agent/me", HttpVerb.Get)]
          public sealed class Query { }
          public sealed class Response { }
        }
      }
      namespace App.Server
      {
        using TimeWarp.Architecture.Configuration;
        using TimeWarp.Foundation.Features;
        using Microsoft.AspNetCore.Authorization;

        public class GetAgentIdentityEndpoint : BaseFastEndpoint<App.Contracts.GetAgentIdentity.Query, App.Contracts.GetAgentIdentity.Response> { }

        public static class Program
        {
          public static void Configure(AuthorizationOptions options)
          {
            options.AddPolicy(AgentTokenDefaults.IdentityReadPolicy, _ => { });
          }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_PermissionIds_With_AddPermissionPolicies_IsClean()
  {
    const string source =
      """
      #region Purpose
      // credential-management surface: PermissionIds.CredentialManageSelf via AddPermissionPolicies.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Architecture.Features;
        using TimeWarp.Foundation.Features;

        [ApiEndpoint]
        [EndpointAuthorize(Policy = PermissionIds.CredentialManageSelf)]
        public static class GetCredentials
        {
          [ApiRoute("api/identity/credentials", HttpVerb.Get)]
          public sealed class Query { }
          public sealed class Response { }
        }
      }
      namespace App.Server
      {
        using TimeWarp.Architecture.Features;
        using TimeWarp.Foundation.Features;
        using Microsoft.AspNetCore.Authorization;

        public class GetCredentialsEndpoint : BaseFastEndpoint<App.Contracts.GetCredentials.Query, App.Contracts.GetCredentials.Response> { }

        public static class Program
        {
          public static void Configure(AuthorizationOptions options)
          {
            PermissionPolicyRegistration.AddPermissionPolicies(options);
          }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Drifted_Literal_Flags_TWA0024()
  {
    const string source =
      """
      #region Purpose
      // Drifted identity-session policy name.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Foundation.Features;

        [ApiEndpoint]
        [EndpointAuthorize(Policy = {|TWA0024:"identity-session-authed"|})]
        public static class GetRole
        {
          [ApiRoute("api/Roles/{RoleId:guid}", HttpVerb.Get)]
          public sealed class Query { }
          public sealed class Response { }
        }
      }
      namespace App.Server
      {
        using TimeWarp.Architecture.Configuration;
        using TimeWarp.Foundation.Features;
        using Microsoft.AspNetCore.Authorization;

        public class GetRoleEndpoint : BaseFastEndpoint<App.Contracts.GetRole.Query, App.Contracts.GetRole.Response> { }

        public static class Program
        {
          public static void Configure(AuthorizationBuilder builder)
          {
            builder.AddPolicy(IdentitySessionDefaults.AuthenticatedPolicy, _ => { });
          }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Drifted_Agent_Scope_Literal_Flags_TWA0024()
  {
    const string source =
      """
      #region Purpose
      // Drifted agent-scope policy vs AgentTokenDefaults.IdentityReadPolicy.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Foundation.Features;

        [ApiEndpoint]
        [EndpointAuthorize(Policy = {|TWA0024:"identity.read"|})]
        public static class GetAgentIdentity
        {
          [ApiRoute("api/identity/agent/me", HttpVerb.Get)]
          public sealed class Query { }
          public sealed class Response { }
        }
      }
      namespace App.Server
      {
        using TimeWarp.Architecture.Configuration;
        using TimeWarp.Foundation.Features;
        using Microsoft.AspNetCore.Authorization;

        public class GetAgentIdentityEndpoint : BaseFastEndpoint<App.Contracts.GetAgentIdentity.Query, App.Contracts.GetAgentIdentity.Response> { }

        public static class Program
        {
          public static void Configure(AuthorizationOptions options)
          {
            options.AddPolicy(AgentTokenDefaults.IdentityReadPolicy, _ => { });
          }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Stale_Credential_Management_Literal_Flags_TWA0024()
  {
    const string source =
      """
      #region Purpose
      // Historical "credential-management" literal vs CredentialManagementDefaults / PermissionIds.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Foundation.Features;

        [ApiEndpoint]
        [EndpointAuthorize(Policy = {|TWA0024:"credential-management"|})]
        public static class GetCredentials
        {
          [ApiRoute("api/identity/credentials", HttpVerb.Get)]
          public sealed class Query { }
          public sealed class Response { }
        }
      }
      namespace App.Server
      {
        using TimeWarp.Architecture.Configuration;
        using TimeWarp.Architecture.Features;
        using TimeWarp.Foundation.Features;
        using Microsoft.AspNetCore.Authorization;

        public class GetCredentialsEndpoint : BaseFastEndpoint<App.Contracts.GetCredentials.Query, App.Contracts.GetCredentials.Response> { }

        public static class Program
        {
          public static void Configure(AuthorizationOptions options)
          {
            options.AddPolicy(CredentialManagementDefaults.Policy, _ => { });
            PermissionPolicyRegistration.AddPermissionPolicies(options);
          }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_PermissionIds_Without_AddPermissionPolicies_Flags_TWA0024()
  {
    const string source =
      """
      #region Purpose
      // PermissionIds const is not registered unless AddPermissionPolicies runs.
      #endregion
      namespace App.Contracts
      {
        using TimeWarp.Architecture;
        using TimeWarp.Architecture.Attributes;
        using TimeWarp.Architecture.Features;
        using TimeWarp.Foundation.Features;

        [ApiEndpoint]
        [EndpointAuthorize(Policy = {|TWA0024:PermissionIds.IdentityRead|})]
        public static class GetAgentIdentity
        {
          [ApiRoute("api/identity/agent/me", HttpVerb.Get)]
          public sealed class Query { }
          public sealed class Response { }
        }
      }
      namespace App.Server
      {
        using TimeWarp.Architecture.Configuration;
        using TimeWarp.Foundation.Features;
        using Microsoft.AspNetCore.Authorization;

        public class GetAgentIdentityEndpoint : BaseFastEndpoint<App.Contracts.GetAgentIdentity.Query, App.Contracts.GetAgentIdentity.Response> { }

        public static class Program
        {
          public static void Configure(AuthorizationOptions options)
          {
            options.AddPolicy(AgentTokenDefaults.IdentityReadPolicy, _ => { });
          }
        }
      }
      """;

    await Test(source).RunAsync();
  }

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
