namespace TimeWarp.Blazor.Testing
{
  using System;

  /// <summary>
  /// This is an attribute used to mark classes that are not intended to be test cases
  /// </summary>
  [NotTest]
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
  public class NotTest : Attribute { }
}
