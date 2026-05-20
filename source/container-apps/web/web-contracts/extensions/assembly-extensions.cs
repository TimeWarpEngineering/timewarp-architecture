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
