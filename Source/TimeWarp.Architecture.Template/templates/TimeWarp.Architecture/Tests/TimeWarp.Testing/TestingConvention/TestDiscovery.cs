namespace TimeWarp.Architecture.Testing;

using Fixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// Fixie allows for the configuration of a custom test discovery process. This is our implementation.
/// </summary>
/// <remarks>This convention looks for all classes that are public and do not have the <see cref="NotTest"/> attribute
/// And all methods within those classes that are not named with the value in <see cref="SetupMethodName"/> are tests
/// </remarks>
[NotTest]
public class TestDiscovery : IDiscovery
{
  private readonly IReadOnlyList<string> CustomArguments;

  public TestDiscovery(IReadOnlyList<string> aCustomArguments)
  {
    CustomArguments = aCustomArguments;
  }

  /// <inheritdoc/>
  public IEnumerable<Type> TestClasses(IEnumerable<Type> aConcreteClasses) =>
    aConcreteClasses
      .Where(TestClassFilter())
      .Where(TagClassFilter());

  /// <inheritdoc/>
  public IEnumerable<MethodInfo> TestMethods(IEnumerable<MethodInfo> aPublicMethods) =>
    aPublicMethods
      .Where(TestMethodFilter())
      .Where(TagMethodFilter());

  internal static Func<Type, bool> TestClassFilter() =>
    aType => aType.IsPublic && !aType.Has<NotTest>();

  private Func<Type, bool> TagClassFilter() =>
    aType =>
      CustomArguments.Count == 0 ||
        aType
          .GetCustomAttributes<TestTagAttribute>()
          .Select(aTestTagAttribute => aTestTagAttribute.Tag)
          .Intersect(CustomArguments)
          .Any();

  private static Func<MethodInfo, bool> TestMethodFilter() =>
    aMethodInfo =>
      !aMethodInfo.IsSpecialName &&
      aMethodInfo.Name != TestingConvention.SetupLifecycleMethodName &&
      aMethodInfo.Name != TestingConvention.CleanupLifecycleMethodName;

  private Func<MethodInfo, bool> TagMethodFilter() =>
    aMethodInfo =>
      CustomArguments.Count == 0 ||
        aMethodInfo
          .GetCustomAttributes<TestTagAttribute>()
          .Select(aTestTagAttribute => aTestTagAttribute.Tag)
          .Intersect(CustomArguments)
          .Any();
}
