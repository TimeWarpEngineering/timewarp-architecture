#region Purpose
// Shared arrange helpers for the IngressRoutePrefixGenerator test classes: the web-contracts source
// snippet, the enabled-gate options, and the generated-source reader.
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System.Linq;

internal static class IngressRoutePrefixGeneratorTestFixture
{
  // api/Roles admin set that the hand list had dropped (must now appear), a [ClientOnlyContract]
  // GetCurrentUser (must NOT appear), a non-api Analytics route (falls to the web catch-all), and a
  // multi-segment api/Users/Current/Profile (collapses to api/Users).
  internal const string WebContracts = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Web.Features;

    [ApiEndpoint]
    public static partial class GetCurrentSession
    {
        [ApiRoute("api/identity/session", HttpVerb.Get)]
        public sealed partial class Query { }
        public sealed class Response { }
    }

    [ApiEndpoint]
    public static partial class AddPasskey
    {
        [ApiRoute("api/identity/credentials/passkey", HttpVerb.Post)]
        public sealed partial class Command { }
        public sealed class Response { }
    }

    [ApiEndpoint]
    public static partial class RevokeCredential
    {
        [ApiRoute("api/identity/credentials/{CredentialId:guid}/revoke", HttpVerb.Post)]
        public sealed partial class Command { }
        public sealed class Response { }
    }

    [ApiEndpoint]
    public static partial class StartPasskeyRegistration
    {
        [ApiRoute("api/identity/passkey/register/options", HttpVerb.Post)]
        public sealed partial class Command { }
        public sealed class Response { }
    }

    [ApiEndpoint]
    public static partial class GetAgentIdentity
    {
        [ApiRoute("api/identity/agent/me", HttpVerb.Get)]
        public sealed partial class Query { }
        public sealed class Response { }
    }

    [ApiEndpoint]
    public static partial class GetRoles
    {
        [ApiRoute("api/Roles", HttpVerb.Get)]
        public sealed partial class Query { }
        public sealed class Response { }
    }

    [ApiEndpoint]
    public static partial class GetRole
    {
        [ApiRoute("api/Roles/{RoleId:guid}", HttpVerb.Get)]
        public sealed partial class Query { }
        public sealed class Response { }
    }

    [ApiEndpoint]
    public static partial class GetProfile
    {
        [ApiRoute("api/Users/Current/Profile", HttpVerb.Get)]
        public sealed partial class Query { }
        public sealed class Response { }
    }

    [ApiEndpoint]
    public static partial class SayHello
    {
        [ApiRoute("api/Hello", HttpVerb.Get)]
        public sealed partial class Query { }
        public sealed class Response { }
    }

    // Real client-only shape (matches get-current-user-contracts.cs): NO [ApiEndpoint] on the outer
    // type, [ClientOnlyContract] on the NESTED request. Excluded by the [ApiEndpoint] gate and the
    // nested-[ClientOnlyContract] check alike.
    public static partial class GetCurrentUser
    {
        [ApiRoute("api/GetCurrentUser", HttpVerb.Get)]
        [ClientOnlyContract("Client builds the identity locally; never a server endpoint.")]
        public sealed partial class Query { }
        public sealed class Response { }
    }

    [ApiEndpoint]
    public static partial class TrackEvent
    {
        [ApiRoute("Analytics/TrackEvent", HttpVerb.Post)]
        public sealed partial class Command { }
        public sealed class Response { }
    }
    """;

  internal static Dictionary<string, string> EnabledForWebContracts() => new()
  {
    ["EnableIngressRouteGeneration"] = "true",
    ["IngressWebContractAssemblies"] = "web-contracts",
    ["IngressReservedPathPrefixes"] = "grpc",
  };

  internal static string GeneratedSource(GeneratorDriverRunResult runResult)
    => runResult.Results.SelectMany(r => r.GeneratedSources).Single().SourceText.ToString();
}
