#region Purpose
// Shared harness (stubs and Test builder) for the TWA0024 policy agreement tests.
#endregion

// ReSharper disable InconsistentNaming
namespace EndpointAuthorizePolicyAgreementAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;

internal static class EndpointAuthorizePolicyAgreementTestFixture
{
  internal const string Stubs =
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
      public sealed class ApiRouteAttribute : System.Attribute
      {
        public ApiRouteAttribute(string routeTemplate, HttpVerb httpVerb) { }
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

  internal static CSharpAnalyzerTest<EndpointAuthorizePolicyAgreementAnalyzer, RoslynTestVerifier> Test(string source) =>
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
}
