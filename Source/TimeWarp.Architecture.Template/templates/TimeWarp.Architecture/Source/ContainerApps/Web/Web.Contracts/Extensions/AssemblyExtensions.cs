namespace TimeWarp.Architecture.Extensions;

public static class AssemblyExtensions
{
  public static IEnumerable<Type> GetTypesWithAttribute(this Assembly aAssembly, Type aAttributeType)
  {
    foreach (Type type in aAssembly.GetTypes())
    {
      if (type.GetCustomAttributes(aAttributeType, false).Any())
      {
        yield return type;
      }
    }
  }
}
