namespace TimeWarp.Blazor.Extensions
{
  using System;
  using System.Collections.Generic;
  using System.Reflection;
  public static class AssemblyExtensions
  {
    public static IEnumerable<Type> GetTypesWithAttribute(this Assembly assembly, Type aAttributeType)
    {
      foreach (Type type in assembly.GetTypes())
      {
        if (type.GetCustomAttributes(aAttributeType, false).Length > 0)
        {
          yield return type;
        }
      }
    }
  }
}
