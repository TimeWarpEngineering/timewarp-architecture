namespace TimeWarp.Architecture.Testing;

#region Purpose
// Resolves a hosted server assembly's own project directory for ContentRootPath, instead of
// wherever a transitive consumer's build copied the assembly to.
#endregion

#region Design
// Task 145-002 R2-1 root cause: Web.Server, Api.Server, and Yarp.Server all ship an
// appsettings.json content item with the SAME relative TargetPath. When a single test consumer
// transitively references more than one of them (HostGraphFactory's Web+Api graph; api-jaribu-tests
// -> timewarp-testing -> {Api,Web}.Server; a full-graph template-smoke matrix), MSBuild's
// content-copy pipeline flattens every referenced project's content into that ONE consumer output
// directory — the later-evaluated project's appsettings.json silently overwrites the earlier one
// (observed: Api.Server's appsettings.json, which has no SampleOptions section, shadowed
// Web.Server's, producing OptionsValidationException at host startup). That collision only stayed
// invisible for the pre-existing web-server-integration-tests suite because it references
// Web.Server ALONE — nothing else to collide with.
//
// Fix: each hosted server project bakes its own build-time $(MSBuildProjectDirectory) into an
// AssemblyMetadataAttribute("ProjectDirectory") — see msbuild/project-directory-metadata.props,
// imported from web-server.csproj / api-server.csproj / yarp.csproj. Resolving ContentRootPath
// from that metadata instead of Assembly.Location means each host reads ITS OWN real
// appsettings.json regardless of how many sibling hosts get flattened into the same consumer's
// output folder — the collision never has a chance to matter. Falls back to Assembly.Location's
// directory (pre-fix behavior) if the metadata is absent, so callers outside the three csproj
// files above degrade safely instead of throwing here.
#endregion

public static class ProjectContentRoot
{
  public static string Resolve(Assembly assembly)
  {
    string? projectDirectory = assembly
      .GetCustomAttributes<AssemblyMetadataAttribute>()
      .FirstOrDefault(attribute => attribute.Key == "ProjectDirectory")
      ?.Value;

    if (!string.IsNullOrEmpty(projectDirectory))
      return Path.GetFullPath(projectDirectory);

    return Path.GetDirectoryName(assembly.Location)
      ?? throw new InvalidOperationException(
        $"Could not resolve a content root directory for {assembly.FullName}.");
  }
}
