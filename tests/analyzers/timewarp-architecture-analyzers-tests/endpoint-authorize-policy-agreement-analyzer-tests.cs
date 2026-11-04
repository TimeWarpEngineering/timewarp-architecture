#region Purpose
// Tests for TWA0024: literal and permission policies must agree with this server registrations.
#endregion

// ReSharper disable InconsistentNaming
namespace EndpointAuthorizePolicyAgreementAnalyzer_;

using static EndpointAuthorizePolicyAgreementAnalyzer_.EndpointAuthorizePolicyAgreementTestFixture;

public class Should_Enforce_Policy_Agreement
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Enforce_Policy_Agreement>();

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
}
