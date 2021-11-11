namespace TimeWarp.Blazor.Testing;

using System;

[NotTest]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class TestTagAttribute : Attribute
{
  public TestTagAttribute(string aTag)
  {
    Tag = aTag;
  }

  public string Tag { get; set; }
}
