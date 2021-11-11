namespace TimeWarp.Blazor.Testing;

using System;

/// <summary>
/// Use this attribute to parameterize inputs into tests
/// </summary>
/// <example>see SimpleNoApplicationTest_Should_.Subtract</example>
[NotTest]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class InputAttribute : Attribute
{
  public InputAttribute(params object[] parameters)
  {
    Parameters = parameters;
  }

  public object[] Parameters { get; }
}
