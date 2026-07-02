#region Purpose
// Reflection helper for discovering types in an assembly by attribute, used to register contracts without hand-maintained lists.
#endregion

namespace TimeWarp.Architecture.Extensions;

public static class AssemblyExtensions
{
  public static IEnumerable<Type> GetTypesWithAttribute(this Assembly assembly, Type attributeType)
  {
    foreach (Type type in assembly.GetTypes())
    {
      if (type.GetCustomAttributes(attributeType, false).Length != 0)
      {
        yield return type;
      }
    }
  }
}
