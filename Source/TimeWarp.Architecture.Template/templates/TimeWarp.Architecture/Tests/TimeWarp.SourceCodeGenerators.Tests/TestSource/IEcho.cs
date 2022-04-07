#nullable enable
namespace TimeWarp.SourceCodeGenerators.Tests.TestSource.Echo;
using System.Collections.Generic;

public interface IEcho
{
  public int MyProperty { get; set; }

  public int MyGetOnlyProperty { get; }

  public int MySetOnlyProperty { set; }

  //event EventHandler ShapeChanged;

  string Echo(string message, List<string> emotions);
  string Method1(int aInt = 10);
  string Method3(string aString, object? aObject = null);
}
