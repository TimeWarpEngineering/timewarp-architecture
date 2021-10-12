namespace TimeWarp.Blazor.Testing
{
  using System;

  [NotTest]
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
  public class NotTest : Attribute { }
}
