// https://github.com/aspnet/Mvc/issues/6749
namespace Microsoft.Extensions.DependencyInjection;

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System.Linq;
using System.Reflection;

public static class MvcBuilderExtensions
{

  public static IMvcBuilder TryAddApplicationPart(this IMvcBuilder aMvcBuilder, Assembly aAssembly)
  {
    aMvcBuilder.ConfigureApplicationPartManager
    (
      aApplicationPartManager =>
      {
        if
        (
          !aApplicationPartManager.ApplicationParts.OfType<AssemblyPart>()
            .Any(aAssemblyPart => aAssemblyPart.Assembly == aAssembly)
        )
        {
          aApplicationPartManager.ApplicationParts.Add(new AssemblyPart(aAssembly));
        }
      }
    );

    return aMvcBuilder;
  }
}
