namespace TimeWarp.Blazor.Testing
{
  using Fixie;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Reflection;

  [NotTest]
  public class TestDiscovery : IDiscovery
  {
    public IEnumerable<Type> TestClasses(IEnumerable<Type> aConcreteClasses) =>
      aConcreteClasses.Where(aType => aType.IsPublic && !aType.Has<NotTest>());

    public IEnumerable<MethodInfo> TestMethods(IEnumerable<MethodInfo> aPublicMethods) =>
      aPublicMethods.Where(aMethodInfo => aMethodInfo.Name != "Setup" && !aMethodInfo.IsSpecialName);
  }
}
