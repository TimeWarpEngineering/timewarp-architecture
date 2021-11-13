namespace TimeWarp.Blazor.Testing
{
  using System;

  /// <summary>
  /// Use this attribute to indicate this test should be skipped with the reason it should be skipped
  /// </summary>
  [NotTest]
  [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
  public class SkipAttribute : Attribute
  {
    public string Reason { get; }

    public SkipAttribute(string aReason)
    {
      Reason = aReason;
    }
  }
}
