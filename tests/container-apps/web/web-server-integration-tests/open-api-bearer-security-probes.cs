#region Purpose
// Probe FastEndpoints for the OpenAPI bearer and session security tests: one per scheme shape and seed grant.
#endregion

#region Design
// Split from open-api-bearer-security-tests.cs so that file stays under the 500-line cap. The
// test host discovers every type in this namespace whose name ends in Endpoint. ProbePaths maps
// each seeded permission to the probe route that requires it.
#endregion

namespace OpenApiBearerSecurity_;

using FastEndpoints;
using TimeWarp.Architecture.Features;

public sealed class AgentMeEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/identity/agent/me");
    AuthSchemes(AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.IdentityRead);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class DomainForbiddenEndpoint : EndpointWithoutRequest
{
  internal const string Route = "/api/agent-links/human-ux";
  internal const string DomainDetail = "This link belongs to a different agent.";

  public override void Configure()
  {
    Get(Route);
    AuthSchemes(AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.AgentLinkManageSelf);
    Summary(summary => summary.Responses[403] = DomainDetail);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

internal static class ProbePaths
{
  private static readonly Dictionary<string, string> Paths = new(StringComparer.Ordinal)
  {
    [PermissionIds.IdentityRead] = "/api/identity/agent/me",
    [PermissionIds.AgentLinkManageSelf] = "/api/agent-links",
    [PermissionIds.CredentialManageSelf] = "/api/identity/credentials",
    [PermissionIds.DemoInvoke] = "/api/demo/metered-capability",
    [PermissionIds.FeedbackRead] = "/api/feedback",
    [PermissionIds.FeedbackNote] = "/api/feedback/notes",
    [PermissionIds.FeedbackSeverity] = "/api/feedback/severity",
    [PermissionIds.FeedbackStatus] = "/api/feedback/status",
  };

  internal static bool TryGetValue(string permission, out string? path) =>
    Paths.TryGetValue(permission, out path);
}

public sealed class AgentLinksEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/agent-links");
    AuthSchemes(AuthenticationSchemeNames.IdentitySession, AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.AgentLinkManageSelf);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class CredentialsEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/identity/credentials");
    AuthSchemes(AuthenticationSchemeNames.IdentitySession, AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.CredentialManageSelf);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class MeteredEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/demo/metered-capability");
    AuthSchemes(AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.DemoInvoke);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class FeedbackReadEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/feedback");
    AuthSchemes(AuthenticationSchemeNames.IdentitySession, AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.FeedbackRead);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class FeedbackNoteEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/feedback/notes");
    AuthSchemes(AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.FeedbackNote);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class FeedbackSeverityEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/feedback/severity");
    AuthSchemes(AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.FeedbackSeverity);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class FeedbackStatusEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/feedback/status");
    AuthSchemes(AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.FeedbackStatus);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class UnmappedAgentEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Put("/api/settings/site");
    AuthSchemes(AuthenticationSchemeNames.AgentToken);
    Policies(PermissionIds.SettingsWrite);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class PublicHelloEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/hello");
    AllowAnonymous();
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class CookieProfileEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Put("/api/profile");
    AuthSchemes(AuthenticationSchemeNames.IdentitySession);
    Policies(PermissionIds.ProfileWrite);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class DefaultSchemeEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/api/default-scheme");
    Policies(PermissionIds.ProfileWrite);
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}
