#region Purpose
// Idempotent application-part registration so composable server modules can each add their
// controller assembly.
#endregion

#region Design
// Modules don't know what the host already registered; AddApplicationPart would add duplicate
// AssemblyParts (duplicate routes), so this checks first — the Try* naming mirrors TryAdd* in DI.
// Lives in Microsoft.Extensions.DependencyInjection so it surfaces next to the framework's own
// IMvcBuilder extensions without an extra using.
#endregion

// https://github.com/aspnet/Mvc/issues/6749
namespace Microsoft.Extensions.DependencyInjection;

public static class MvcBuilderExtensions
{

  public static IMvcBuilder TryAddApplicationPart(this IMvcBuilder mvcBuilder, Assembly assembly)
  {
    mvcBuilder.ConfigureApplicationPartManager
    (
      applicationPartManager =>
      {
        if
        (
          !applicationPartManager.ApplicationParts.OfType<AssemblyPart>()
            .Any(assemblyPart => assemblyPart.Assembly == assembly)
        )
        {
          applicationPartManager.ApplicationParts.Add(new AssemblyPart(assembly));
        }
      }
    );

    return mvcBuilder;
  }
}
