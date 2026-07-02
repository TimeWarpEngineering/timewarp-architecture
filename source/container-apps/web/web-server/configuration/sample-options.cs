#region Purpose
// Example options class demonstrating the AddFluentValidatedOptions binding-plus-validation pattern.
#endregion

namespace TimeWarp.Architecture.Configuration;

public class SampleOptions
{
  public string SampleOption { get; set; } = null!;
}
