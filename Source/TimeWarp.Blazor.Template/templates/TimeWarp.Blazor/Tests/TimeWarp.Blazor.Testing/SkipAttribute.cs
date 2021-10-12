namespace TimeWarp.Blazor.Testing
{
  using System;

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
