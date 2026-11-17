#region Purpose
// The project resources program.cs declared, handed to the concerns that wire across them.
#endregion

#region Design
// Each member exists only under its template flag, the same guard program.cs declares the project
// under, so an excluded service leaves no member behind and a concern reads a project only inside
// that guard. IngressResource (routes and references) and AcaNeonCompute (container-app scale) read
// it. Concerns that need one project take it as a parameter instead (PostgresResources).
#endregion

namespace TimeWarp.Architecture.Aspire;

/// <summary>The declared project resources, by template flag.</summary>
internal sealed class AppHostProjects
{
#if api
  /// <summary>The api-server project resource.</summary>
  internal required IResourceBuilder<ProjectResource> ApiServer { get; init; }
#endif
#if grpc
  /// <summary>The grpc-server project resource.</summary>
  internal required IResourceBuilder<ProjectResource> GrpcServer { get; init; }
#endif
#if web
  /// <summary>The web-server project resource.</summary>
  internal required IResourceBuilder<ProjectResource> WebServer { get; init; }

  /// <summary>The State DevTools inspector in run mode; null in publish mode.</summary>
  internal IResourceBuilder<ExecutableResource>? StateDevTools { get; init; }
#endif
}
