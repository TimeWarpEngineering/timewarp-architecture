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
